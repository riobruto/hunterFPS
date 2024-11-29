using System.Collections.Generic;
using UnityEngine;

namespace Game.Train
{
    public class TrainEngine : TrainBase, ITrainEngine
    {
        [SerializeField] private TrainEngineConfig _config;
        [SerializeField] private List<TrainBase> _connectedCars = new List<TrainBase>();

        private float _forwardMultiplier = 1;
        private float _force;
        private int _currentLevel;
        private float _rpm;
        private float _avaliableEnergy;
        private float _rollingResistanceCoefficient = 0.004f;
        private float _currentFuel;
        private bool _isApplyingBrakes => _currentBrakeLevel > 0;
        private float _currentBrakePressure;

        private float _currentBrakeLevel;
        private float _breakForce => Mathf.Lerp(0, _config.BrakeConfig.MaxDrag, _config.BrakeConfig.Curve.Evaluate(_currentBrakeLevel));
        public float RPM => _rpm;
        public float AvaliableEnergy => _avaliableEnergy;
        private float _load => Mathf.InverseLerp(0, _config.PowerLevels[_currentLevel].Force, _actualForce);

        private float _sandLevel;

        public override float BrakeForce => _breakForce;
        public float SpeedInKMH => Speed * 3.6000f;
        public float EffectiveLoad => _load;

        public float WheelSlip { get; private set; }
        public float Fuel { get => _currentFuel; }
        public float MaxFuel { get => _config.EngineConfig.FuelCapacity; }

        private float _lastSpeed;
        private float _acceleration;

        protected override void OnAwake()
        {
            Rigidbody.useGravity = false;
        }

        protected override void OnStart()
        {
            SetConnectedCars();
            Activate();

            _engineCurrentMassLoad = Rigidbody.mass;
            _currentFuel = _config.EngineConfig.FuelCapacity;
            _currentBrakePressure = 0;
        }

        private void SetConnectedCars()
        {
            TrainBase current = this;

            while (current.ConnectedPartPrevious)
            {
                _connectedCars.Add(current.ConnectedPartPrevious);
                Debug.Log(current.name);
                current = current.ConnectedPartPrevious;
            }
        }

        public override void OnFixedUpdate()
        {
            _acceleration = (Speed - _lastSpeed) / Time.fixedDeltaTime;

            /*
            _wheelSlipAmount = _config.PowerLevels[Mathf.Clamp(_currentLevel - 1, 0, _config.PowerLevels.Length)].MaxSpeedInKilometersPerHour - SpeedInKMH;
            IsWheelSlipping = _wheelSlipAmount > _slipSpeedTolerance;
            */

            if (!_isApplyingBrakes)
            {
                _currentBrakePressure += _currentBrakePressure < _config.BrakeConfig.MaxPressure ? 10 * Time.fixedDeltaTime : 0;
            }
            if (_isApplyingBrakes)
            {
                _currentBrakePressure -= _currentBrakePressure > 0 ? _currentBrakeLevel * Time.fixedDeltaTime : 0;
            }
            /*
			if (_currentFuel > 0)
			{
				//_force = Mathf.Lerp(_force, _canAccelerate ? _config.PowerLevels[_currentLevel].Force : _config.PowerLevels[_currentLevel].Force * _sandLevel, 2 * Time.deltaTime);
				_force = ManageForce();
				foreach (TractionBogie bogie in _bogies)
				{
					bogie.AddBogieForce(_force * _forwardMultiplier);
				}
			}*/

            _force = ManageForce();

            foreach (TractionBogie bogie in Bogies)
            {
                bogie.AddBogieForce(_force * _forwardMultiplier);
            }

            ManageEngine();
            //brakes
            ManageIndependentBrakes();

            ManageTrainBrakes();

            _lastSpeed = Speed;
        }

        private float _actualForce;

        private float ManageForce()
        {
            float desiredTractiveForce = SpeedInKMH < _config.PowerLevels[_currentLevel].MaxSpeedInKilometersPerHour ? _config.PowerLevels[_currentLevel].Force : 0;
            float inertia = .8f;
            _actualForce = Mathf.MoveTowards(_actualForce, desiredTractiveForce, inertia);
            float slip = ((desiredTractiveForce - _actualForce) / Rigidbody.mass) - Mathf.Abs(_acceleration);
            WheelSlip = slip;

            return _actualForce * Mathf.InverseLerp(3, 0, slip);
        }

        private void ManageIndependentBrakes()
        {
            foreach (TractionBogie bogie in Bogies)
            {
                bogie.SetBrakeForce(_breakForce);
            }
        }

        private void ManageTrainBrakes()
        {
            foreach (TrainBase part in _connectedCars)
            {
                part.SetBreak(_breakForce);
            }
        }

        private void ManageEngine()
        {
            if (_currentFuel > 0)
            {
                _rpm = Mathf.Lerp(_config.EngineConfig.MinRPM, _config.EngineConfig.MaxRPM, _currentLevel / (float)_config.PowerLevels.Length);
                _currentFuel -= (_rpm / _config.EngineConfig.MaxRPM) * (_load * Time.fixedDeltaTime);
                _currentFuel -= _breakForce;
            }

            if (_currentFuel <= 0)
            {
                _rpm = 0;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
        }

        private void OnGUI()
        {
            return;
            GUILayout.Label($"K/h: {Speed * 3.6f}");
            GUILayout.Label($"M/s: {Speed}");
            GUILayout.Label($"Acceleration: {_acceleration}");
            GUILayout.Label($"Current Throttle Level: {_currentLevel}");
            GUILayout.Label($"Force in Tons: {Mathf.Round(ManageForce())}");
            GUILayout.Label($"Effective Load : {Mathf.InverseLerp(0, _config.PowerLevels[_currentLevel].Force, _force)}");
            GUILayout.Space(10);
            GUILayout.Label($"RPM: {_rpm}");
            GUILayout.Label($"Interpolator: {Mathf.InverseLerp(0, _config.PowerLevels[_currentLevel].MaxSpeedInKilometersPerHour, SpeedInKMH)}");
            GUILayout.Label($"Fuel: {_currentFuel}");
            GUILayout.Label($"Brakes: {_breakForce}");
            GUILayout.Space(10);
            GUILayout.Label($"BrakeApplication: {_currentBrakeLevel}");
            GUILayout.Label($"Brake Pressure: {_currentBrakePressure}");
            GUILayout.Label($"WheelSlip: {WheelSlip}");

            GUILayout.Space(10);
        }

        private float _engineCurrentMassLoad;

        public override void OnPartConnected(TrainBase part)
        {
            _connectedCars.Add(part);
            _engineCurrentMassLoad += part.Rigidbody.mass;
        }

        void ITrainEngine.SetAccelerationLevel(int value)
        {
            _currentLevel = Mathf.Clamp(value, 0, _config.PowerLevels.Length);
        }

        void ITrainEngine.SetBrakeLevel(float value)
        {
            _currentBrakeLevel = Mathf.Lerp(0, 0.1f, value);
        }

        void ITrainEngine.SetIndependentBrakeLevel(float value)
        {
        }

        void ITrainEngine.SetSandLevel(float value)
        {
        }

        void ITrainEngine.SetReverser(int value)
        {
            _forwardMultiplier = value;
        }

        void ITrainEngine.SetSleep(bool state)
        {
            //Rigidbody.isKinematic = state;
        }
    }
}