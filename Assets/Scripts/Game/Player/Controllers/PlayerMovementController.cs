using Core.Configuration;
using Core.Engine;
using Game.Player.Controllers;
using Game.Player.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerMovementController : MonoBehaviour
    {
        private CharacterController _characterController;
        private PlayerConfiguration.PlayerControlSettings _settings;

        private float _stamina;
        private float _maxStamina = 100f;
        private float _lastTimeStaminaReduced;
        private bool _canIncrementStamina => Time.time - _lastTimeStaminaReduced > 3;

        public float Stamina
        {
            get => _stamina;
            set
            {
                if (value < _stamina)
                {
                    _lastTimeStaminaReduced = Time.time;
                }

                _stamina = value;
            }
        }

        [SerializeField] private Transform _head;

        public CharacterController Controller
        { get { return _characterController; } }

        public Transform Head
        { get { return _head; } }

        public PlayerConfiguration.PlayerControlSettings Settings
        { get { return _settings; } }

        private PlayerGroundMovement _groundMovement;
        private PlayerAirMovement _airMovement;
        private PlayerLookMovement _lookMovement;
        private PlayerLeanMovement _leanMovement;
        private PlayerVaultMovement _vaultMovement;
        private PlayerBaseMovement[] _movements;

        private bool _canFly = false;
        private bool _canChangeFly = false;

        private float _lastTimeFlyChange = 0;
        private float _currentRadius;
        private float _targetRadius;
        private float _refRadiusVelocity;

        public Vector3 RelativeVelocity => transform.InverseTransformDirection(Controller.velocity - GroundMovement.RigidbodyFollowVelocity);

        private void OnGUI()
        {
            // GUILayout.TextArea($"Velocity: {RelativeVelocity}");
        }

        private void OnFly(InputValue value)
        {
            //TODO: Mejorar logica de movimiento.
            //conservar inercia cuando el el jugador se cansa de volar.

            if (_canChangeFly && Time.time - _lastTimeFlyChange > 1f && !_groundMovement.IsCrouching)
            {
                _canFly = !_canFly;
                if (_canFly)
                {
                    BeginFly();
                    return;
                }
                EndFly();

                _lastTimeFlyChange = Time.time;
            }
        }

        public void SetAllowGroundMovement(bool state)
        {
            LookMovement.AllowHorizontalLook = state;
            LookMovement.AllowVerticalLook = state;
            GroundMovement.AllowCrouch = state;
            GroundMovement.AllowSprint = state;
            GroundMovement.AllowInputMovement = state;
            GroundMovement.AllowJump = state;
            _canChangeFly = state;
        }

        private void GetMovementComponents()
        {
            _groundMovement = GetComponent<PlayerGroundMovement>();
            _airMovement = GetComponent<PlayerAirMovement>();
            _lookMovement = GetComponent<PlayerLookMovement>();
            _leanMovement = GetComponent<PlayerLeanMovement>();
            _vaultMovement = GetComponent<PlayerVaultMovement>();
            _movements = new PlayerBaseMovement[] { _groundMovement, _airMovement, _lookMovement, _leanMovement, _vaultMovement };

            foreach (var movement in _movements)
            {
                movement.SetManager(this);
                movement.Initialize();
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;

            _characterController = GetComponent<CharacterController>();
            _settings = Bootstrap.Resolve<GameSettings>().PlayerConfiguration.Settings;
            _stamina = _maxStamina;
            GetMovementComponents();
            _targetRadius = .5f;
            InitialFlags();
            InitializeEvents();

            foreach (IObserverFromPlayerMovement observer in GetComponentsInChildren<IObserverFromPlayerMovement>())
            {
                observer.Initalize(this);
            }
        }

        private void InitializeEvents()
        {
            _vaultMovement.VaultEvent += OnVault;
            _groundMovement.CrouchEvent += OnGroundMovementCrouch;
            _groundMovement.ChangeStateEvent += OnGroundMovementChangeState;
            _groundMovement.FallEvent += OnFall;
            _airMovement.ChangeStateEvent += OnAirMovementChangeState;
        }

        private void OnFall(float distance)
        {
            if (distance > 5)
            {
                GetComponent<PlayerHealth>().Hurt(distance);
            }
        }

        private void OnAirMovementChangeState(AirMovementState last, AirMovementState current)
        {
        }

        private void OnGroundMovementCrouch(bool state)
        {
            //Debug.Log(state ? "Crouch" : "Stood");
            _vaultMovement.AllowVault = !state;
        }

        private void OnGroundMovementChangeState(GroundMovementState last, GroundMovementState current)
        {
        }

        private void OnVault(bool state)
        {
            //Debug.Log(state ? "Vaulting" : "Vault Ended");

            _groundMovement.AllowInputMovement = !state;
            _leanMovement.AllowLean = !state;
            GroundMovement.Active = !state;
            AirMovement.Active = !state;
            _canChangeFly = !state;
            if (!state) EndFly();
        }

        private void InitialFlags()
        {
            GroundMovement.Active = true;
            AirMovement.Active = false;
            GroundMovement.AllowCrouch = true;
            GroundMovement.AllowSprint = true;
            GroundMovement.AllowInputMovement = true;
            VaultMovement.AllowVault = true;
            LookMovement.AllowVerticalLook = true;
            LookMovement.AllowHorizontalLook = true;
            LookMovement.Sensitivity = Settings.NormalSensitivity;
            LeanMovement.AllowLean = true;
            AirMovement.AllowBoost = true;
            //player starts in the ground.
            AirMovement.AllowInput = false;
        }

        internal void BeginFly()
        {
            _targetRadius = .5f;
            GroundMovement.Active = false;
            AirMovement.Active = true;
            AirMovement.Impulse(Vector3.up * 5f);
            AirMovement.AllowInput = true;
            AirMovement.AllowBoost = true;
            GroundMovement.AllowCrouch = false;
            GroundMovement.AllowSprint = false;
            GroundMovement.AllowInputMovement = false;
            LeanMovement.AllowLean = false;
            VaultMovement.AllowVault = false;
        }

        internal void EndFly()
        {
            _targetRadius = 0.5f;
            AirMovement.Active = false;
            GroundMovement.Active = true;
            GroundMovement.Impulse(inertia: RelativeVelocity);
            AirMovement.AllowInput = false;
            AirMovement.AllowBoost = false;
            GroundMovement.AllowCrouch = true;
            GroundMovement.AllowSprint = true;
            GroundMovement.AllowInputMovement = true;
            LeanMovement.AllowLean = true;
            VaultMovement.AllowVault = true;
        }

        private void Update()
        {
            Controller.radius = Mathf.SmoothDamp(Controller.radius, _targetRadius, ref _refRadiusVelocity, Time.deltaTime * 2f);

            IsSprinting = GroundMovement.IsSprinting;
            IsCrouching = GroundMovement.IsCrouching;
            IsFlying = AirMovement.IsFlying;
            IsFalling = !AirMovement.IsFlying && !GroundMovement.IsGrounded;

            if (_stamina < 10 && IsFlying)
            {
                _canFly = !_canFly;
                _lastTimeFlyChange = Time.time;
                EndFly();
            }

            if (_canIncrementStamina)
            {
                _stamina = Mathf.Clamp(_stamina + Time.deltaTime * 20f, 0, _maxStamina);
            }
        }

        internal void SetMovementFlags(bool value)
        {
            AirMovement.AllowInput = value;
            AirMovement.AllowBoost = value;
            GroundMovement.AllowCrouch = value;
            GroundMovement.AllowSprint = value;
            GroundMovement.AllowInputMovement = value;
            LeanMovement.AllowLean = value;
            VaultMovement.AllowVault = value;
            LookMovement.AllowHorizontalLook = value;
            LookMovement.AllowVerticalLook = value;
        }

        internal void Teletransport(Vector3 pushPos)
        {
            Controller.enabled = false;
            transform.position = pushPos;
            Controller.enabled = true;
        }

        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsFlying { get; private set; }
        public bool IsFalling { get; private set; }
        public bool IsVaulting { get; private set; }

        public PlayerGroundMovement GroundMovement { get => _groundMovement; internal set => _groundMovement = value; }
        public PlayerAirMovement AirMovement { get => _airMovement; internal set => _airMovement = value; }
        public PlayerLookMovement LookMovement { get => _lookMovement; internal set => _lookMovement = value; }
        public PlayerLeanMovement LeanMovement { get => _leanMovement; internal set => _leanMovement = value; }
        public PlayerVaultMovement VaultMovement { get => _vaultMovement; internal set => _vaultMovement = value; }
        public float MaxStamina { get => _maxStamina; }
    }
}