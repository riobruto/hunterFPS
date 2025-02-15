using Core.Engine;
using Game.Service;
using Game.Train;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Train
{
    public class TrainControlPossesable : MonoBehaviour
    {
        [SerializeField] private TrainEnterEntity _enter;
        [SerializeField] private Transform _playerSeatPosition;
        [SerializeField] private Transform _playerExitPosition;

        private PlayerTrainController _controller;
        private ITrainEngine _currentTrain;
        private bool _wantExit;
        private bool _trainPossessed;

        public Transform PlayerSeatPosition { get => _playerSeatPosition; }
        public Transform PlayerExitPosition { get => _playerExitPosition; }

        public int CurrentAccelerationLevel { get => _currentAccelerationLevel; }
        public int CurrentBrakeLevel { get => _currentBrakeLevel; }
        public int CurrentReverser { get => _currentReverser; }
        public float SpeedInKmh { get => (_currentTrain as TrainBase).Speed * 3.6000f; }
        public float MaxFuel { get => (_currentTrain as TrainEngine).MaxFuel; }
        public float Fuel { get => (_currentTrain as TrainEngine).Fuel; }
        public float SidewayStress => (_currentTrain as TrainEngine).Bogies[0].SidewayStress;


        private AudioSource _controlSound;

        [SerializeField] private AudioClip _reverserClip;
        [SerializeField] private AudioClip _accelClip;
        [SerializeField] private AudioClip _brakeClip;

        protected void Start()
        {
            _currentTrain = GetComponent<ITrainEngine>();

            _controller = Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerTrainController>();
            _enter.EnterEvent += EnterTrain;
            _reverser = 0;
            _currentTrain.SetReverser(0);
            _currentTrain.SetBrakeLevel(1);

            _controlSound = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            if (_trainPossessed)
                ManageControls();

            if (_wantExit)
            {
                ExitTrain();
                _wantExit = false;
            }
        }

        private int _currentAccelerationLevel = 0;
        private int _currentBrakeLevel = 0;
        private int _currentReverser = 0;
        private int _lastAccelerationLevel = 0;
        private int _lastBrakeLevel = 0;
        private int _maxAccelLevel = 8;
        private int _maxBrakeLevel = 8;
        private int _reverser = 0;
        private int _lastReverser = 0;

        private void ManageControls()
        {
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                if (_currentBrakeLevel == 0)
                {
                    _currentAccelerationLevel = Mathf.Clamp(_currentAccelerationLevel + 1, 0, _maxAccelLevel);
                }
                else _currentBrakeLevel = Mathf.Clamp(_currentBrakeLevel - 1, 0, _maxBrakeLevel);
            }
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                if (_currentAccelerationLevel == 0)
                {
                    _currentBrakeLevel = Mathf.Clamp(_currentBrakeLevel + 1, 0, _maxBrakeLevel);
                }
                else _currentAccelerationLevel = Mathf.Clamp(_currentAccelerationLevel - 1, 0, _maxAccelLevel);
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                if (_currentAccelerationLevel == 0 && (_currentTrain as TrainBase).Speed < 0.01f)
                {
                    _reverser += 1;
                    _currentReverser = (int)Mathf.PingPong(_reverser, 2) - 1;
                    _currentTrain.SetReverser(_currentReverser);
                }
            }

            if (_lastAccelerationLevel != _currentAccelerationLevel)
            {
                _controlSound.PlayOneShot(_accelClip);
                _currentTrain.SetAccelerationLevel(_currentAccelerationLevel);
                _lastAccelerationLevel = _currentAccelerationLevel;
            }

            if (_lastBrakeLevel != _currentBrakeLevel)
            {
                _controlSound.PlayOneShot(_brakeClip);
                _currentTrain.SetBrakeLevel(_currentBrakeLevel / 8f);
                _lastBrakeLevel = _currentBrakeLevel;
            }

            if (_lastReverser != _reverser)
            {
                _controlSound.PlayOneShot(_reverserClip);
                _lastReverser = _reverser;
            }
        }

        private void EnterTrain()
        {
            (_currentTrain as TrainBase).Rigidbody.excludeLayers += LayerMask.GetMask("Player");
            _currentTrain.SetSleep(false);
            _controller.Enter(this);
            _trainPossessed = true;
            _currentTrain.SetReverser(1);
        }

        private void ExitTrain()
        {
            (_currentTrain as TrainBase).Rigidbody.excludeLayers -= LayerMask.GetMask("Player");
            _controller.Exit(this);
            _currentTrain.SetSleep(true);
            _trainPossessed = false;
            _enter.Reset();
            _currentTrain.SetReverser(0);
        }

        internal void ExitRequest()
        {
            _wantExit = true;
        }

        internal void CoupleRequest()
        {
          



        }
    }
}