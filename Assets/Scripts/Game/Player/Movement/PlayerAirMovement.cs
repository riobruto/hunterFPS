using Core.Engine;
using Game.Enviroment;
using Nomnom.RaycastVisualization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    public delegate void PlayerAirMovementDelegate(AirMovementState last, AirMovementState current);

    public enum AirMovementState
    {
        FLYING,
        IDLE,
        BOOST,
    }

    public class PlayerAirMovement : PlayerBaseMovement
    {
        private AirMovementState _currentState;
        private AirMovementState _lastState;

        public event PlayerAirMovementDelegate ChangeStateEvent;

        private Vector2 _inputBody;
        private Vector3 _move;
        [SerializeField] private float _movementInertia = .025f;
        [SerializeField] private float _boostForce = 0.25f;
        internal bool AllowInput { get; set; }
        internal bool AllowBoost { get; set; }

        protected override void OnStart()
        {
            Manager.AirMovement = this;
            wind = Bootstrap.Resolve<WindService>().Instance;
        }

        public bool Active;

        private Vector3 _inertiaMove;
        private Vector3 _refInertiaMove;

        private bool _waitBoost;
        private Vector3 _boostVector;
        private bool _boost;

        //Boost Time
        private bool _canBoost => _boostTime > _boostCooldownTime && AllowBoost;

        [SerializeField] private float _boostCooldownTime = 3f;
        private float _boostTime = 0;
        private float _verticalAxis;
        [SerializeField] private float _noiseStrength = 0.1f;

        private WindSystem wind;

        private void OnMove(InputValue value)
        {
            _inputBody = value.Get<Vector2>();
        }

        private void OnSprint(InputValue value)
        {
            _waitBoost = value.isPressed && AllowInput;
        }

        private void OnVerticalMove(InputValue value)
        {
            if (!AllowInput)
            {
                _verticalAxis = 0;
                return;
            }
            _verticalAxis = value.Get<float>();
        }

        public void StopMovement()
        {
            _waitBoost = false;
            _move = Vector3.zero;
            _boostVector = Vector3.zero;
        }

        [SerializeField] private float _flapSpeed = 1f;
        [SerializeField] private float _flapIntensity = 0.1f;

        protected override void OnUpdate()
        {
            if (_currentState != _lastState)
            {
                NotifyState(_lastState, _currentState);
                _lastState = _currentState;
            }

            if (!Active) return;

            _boostTime = Mathf.Clamp(_boostTime + Time.deltaTime, 0, float.MaxValue);

            if (_waitBoost)
            {
                if (_canBoost)
                {
                    _boostVector = new Vector3(_inputBody.x, _verticalAxis, _inputBody.y) * _boostForce;
                    _boostTime = 0;
                    _currentState = AirMovementState.BOOST;
                    Manager.Stamina -= 10f;
                }
                _waitBoost = false;
            }
            _boostVector = Vector3.MoveTowards(_boostVector, Vector3.zero, Time.deltaTime * _boostForce);

            _move.x = _inputBody.x * Manager.Settings.FlySpeed * (AllowInput ? 1 : 0);
            _move.z = _inputBody.y * Manager.Settings.FlySpeed * (AllowInput ? 1 : 0);

            Manager.Stamina -= Time.deltaTime * _move.magnitude * .5f;

            float v = Mathf.Clamp(_verticalAxis - .25f, -1, 1) * Manager.Settings.FlySpeed;

            Vector3 noise = Random.insideUnitSphere.normalized * Mathf.PerlinNoise1D(Time.time) * _noiseStrength;
            Vector3 oscillation = new Vector3(0, Mathf.Sin(Time.time * _flapSpeed), 0) * _flapIntensity;

            Debug.DrawRay(transform.position, noise + oscillation + (wind.Direction * wind.MainIntensity), Color.blue);
            _inertiaMove = Vector3.SmoothDamp(_inertiaMove, Manager.Head.TransformDirection(_move) + noise + oscillation + new Vector3(0, v, 0) + (wind.Direction * wind.MainIntensity), ref _refInertiaMove, _movementInertia);

            Manager.Controller.Move((_inertiaMove + Manager.Head.TransformDirection(_boostVector)) * Time.deltaTime);
            _currentState = _inertiaMove == Vector3.zero ? AirMovementState.IDLE : AirMovementState.FLYING;
        }

        private bool CheckGroundCollision()
        {
            return VisualPhysics.BoxCast(transform.position + transform.up, Vector3.one / 2.25f, -transform.up, Quaternion.identity, .550f + Manager.Controller.skinWidth * 2);
        }

        public Vector3 CurrentMovement
        { get { return _inertiaMove; } }

        public bool IsFlying => AllowInput;

        internal void Impulse(Vector3 inertia)
        {
            _waitBoost = false;
            //lo transformo a local de vuelta porque lo van a convertir en local devuelta en un rato xddddddddddddddddddddd

            //inertia = transform.TransformDirection(inertia);

            _inertiaMove.x += inertia.x;
            _inertiaMove.y += inertia.y;
            _inertiaMove.z += inertia.z;
        }

        private void NotifyState(AirMovementState last, AirMovementState current)
        {
            ChangeStateEvent?.Invoke(last, current);
        }
    }
}