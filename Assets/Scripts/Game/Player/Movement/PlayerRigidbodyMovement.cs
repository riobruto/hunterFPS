using Core.Engine;
using Game.Player.Movement;
using Nomnom.RaycastVisualization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public enum PlayerMovementState
    {
        SPRINT,
        WALK,
        IDLE,
        LANDING,
        JUMP,
        FALLING,
        CROUCH
    }

    public delegate void PlayerMovementStateDelegate(PlayerMovementState current, PlayerMovementState next);

    public delegate void PlayerFallDelegate(Vector3 start, Vector3 end);

    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerRigidbodyMovement : MonoBehaviour
    {
        private RaycastHit _hit;

        [Header("References")]
        [SerializeField] private Rigidbody _rigidBody;

        [SerializeField] private CapsuleCollider _collider;
        [SerializeField] private Rigidbody _otherRigidBody;

        [Header("Spring")]
        [SerializeField] private float _rideHeight = 2;

        [SerializeField] private float _rideSpringStrength;
        [SerializeField] private float _rideSpringDamper;

        [Header("Speeds")]
        [SerializeField] private float _walkSpeed = 3;

        [SerializeField] private float _sprintSpeed = 6;
        [SerializeField] private float _crouchSpeed = 1.5f;
        [SerializeField] private float _airSpeed = 3;

        [Header("Misc")]
        [SerializeField] private float _walkAcceleration = 200;

        [SerializeField] private float _maxWalkAcceleration = 140;
        [SerializeField] private float _jumpForce = 40;
        [SerializeField] private float _maxSteepAngle = 45;

        [SerializeField] private Transform _head;

        private Vector2 _inputBody;
        private Vector2 _inputHead;
        private Vector3 _move;
        private Vector3 _totalGoalVelocity;
        private LayerMask _layer;
        private float _sensitivity = 10;
        private bool _wantJump;
        private float _jumpCool;
        private bool _slipping;
        private bool _grounded;
        private Vector3 _jumpboost;
        private float _verticalLookAngle;
        private bool _wantCrouching;
        private float _crouchRefVelocity;

        public bool IsSprinting => CurrentState == PlayerMovementState.SPRINT;
        public bool IsCrouching => CurrentState == PlayerMovementState.CROUCH;
        public bool IsFalling => CurrentState == PlayerMovementState.FALLING;

        private bool CastRay() => VisualPhysics.SphereCast(transform.position + transform.up,
            _collider.radius - 0.05f, -transform.up, out _hit, _rideHeight, _layer, QueryTriggerInteraction.Ignore);

        //Public fields

        public event PlayerMovementStateDelegate PlayerStateEvent;

        public event PlayerFallDelegate PlayerFallEvent;

        public Vector3 Velocity => _rigidBody.velocity;
        public Vector3 RelativeVelocity => transform.InverseTransformDirection(_rigidBody.velocity - (_otherRigidBody ? _otherRigidBody.velocity : Vector3.zero));
        public PlayerMovementState CurrentState => _currentState;
        public bool IsGrounded => _grounded;

        public bool AllowMovement { get; set; } = true;
        public bool AllowLookMovement { get; set; } = true;
        public bool AllowCrouch { get; set; } = true;
        public bool AllowSprint { get; set; } = true;
        public bool AllowJump { get; set; } = true;
        public float Sensitivity { get => _sensitivity; set => _sensitivity = value; }

        private void Start()
        {
            _collider = GetComponent<CapsuleCollider>();
            _rigidBody = GetComponent<Rigidbody>();
            _layer = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.IgnoreLayers;
            _rigidBody.freezeRotation = true;
            foreach (IObserverFromPlayerMovement movement in GetComponentsInChildren<IObserverFromPlayerMovement>()) { movement.Initalize(this); }
        }

        private void OnDisable()
        {
            foreach (IObserverFromPlayerMovement movement in GetComponentsInChildren<IObserverFromPlayerMovement>()) { movement.Detach(this); }
        }

        private void Update()
        {
            ManageLook();
            ManageCrouchRezise();
            ManageState();
            ManageStamina();
        }

        private float _stamina;
        private float _maxStamina = 100f;
        private float _lastTimeStaminaReduced;
        private float _staminaDecrement = 4f;
        private float _staminaResistance = 0;
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

        public float MaxStamina => _maxStamina;
        public float StaminaResistance { set => _staminaResistance = value; }
        public float VerticalLookAngle { get => _verticalLookAngle; }
        public float WalkSpeed { get => _walkSpeed; }
        public float SprintSpeed { get => _sprintSpeed; }

        private void ManageStamina()
        {
            if (_canIncrementStamina)
            {
                _stamina = Mathf.Clamp(_stamina + Time.deltaTime * 20f, 0, _maxStamina);
            }
        }

        private void ManageState()
        {
            if (_currentState != _lastState)
            {
                NotifyStates(_currentState, _lastState);
                _lastState = _currentState;
            }
        }

        private void NotifyStates(PlayerMovementState next, PlayerMovementState current)
        {
            PlayerStateEvent?.Invoke(current, next);
        }

        private float _crouchAmount = 0;
        private float _crouchChangeSpeed = 0.25f;
        private bool _wantSprinting;
        private Vector3 airControl;
        private PlayerMovementState _lastState;
        private PlayerMovementState _currentState;

        private Vector3 _fallStart;
        private Vector3 _fallEnd;
        private bool _lastIsGrounded;

        private void OnCrouch(InputValue value)
        {
            if (!AllowCrouch) { _wantCrouching = false; return; }

            if (CheckHeadObstruction() && _wantCrouching) return;
            else _wantCrouching = !_wantCrouching;
            if (_wantSprinting) _wantSprinting = false;
        }

        private void OnSprint(InputValue value)
        {
            if (_pauseMovement) return;
            if (!AllowSprint) { _wantSprinting = false; return; }
            if (_wantCrouching && !_wantSprinting) return;
            else _wantSprinting = value.isPressed;
        }

        private void OnJump(InputValue value)
        {
            if (_pauseMovement) return;
            if (!AllowJump) { _wantJump = false; return; }
            _wantJump = value.isPressed && !_wantCrouching;
        }

        private void ManageCrouchRezise()
        {
            if (_pauseMovement) { _crouchAmount = 0; return; }

            _crouchAmount = Mathf.SmoothDamp(_crouchAmount, _wantCrouching && AllowCrouch || !_grounded ? 1 : 0, ref _crouchRefVelocity, _crouchChangeSpeed);
            _collider.center = Vector3.up * Mathf.Lerp(1.25f, .75f, _crouchAmount);
            _collider.height = Mathf.Lerp(1.75f, 1f, _crouchAmount);
            _head.localPosition = Vector3.up * Mathf.Lerp(1.75f, 1f, _crouchAmount);
        }

        private bool CheckHeadObstruction()
        {
            return VisualPhysics.Raycast(transform.position + transform.up, transform.up, 1, _layer); //~_raycastConfiguration.IgnoreLayers);
        }

        private void ManageLook()
        {
            if (AllowLookMovement)
            {
                _verticalLookAngle = Mathf.Clamp(_verticalLookAngle - _inputHead.y * _sensitivity * Time.deltaTime, -70, 80);
                _head.localRotation = Quaternion.Euler(_verticalLookAngle, 0, 0);

                if (_pauseMovement)
                {
                    transform.rotation = transform.rotation * Quaternion.Euler(Vector3.up * _inputHead.x * _sensitivity * Time.fixedDeltaTime);
                }
                else
                    _rigidBody.MoveRotation(_rigidBody.rotation * Quaternion.Euler(Vector3.up * _inputHead.x * _sensitivity * Time.fixedDeltaTime));
            }
        }

        public void ImpulseLook(Vector2 target)
        {
            _verticalLookAngle += target.x;
        }

        private void FixedUpdate()
        {
            _jumpCool = Mathf.Clamp(_jumpCool - Time.fixedDeltaTime, 0f, 1f);

            if (_pauseMovement) return;
            if (_grounded = CastRay())
            {
                _otherRigidBody = null;
                Vector3 otherVelocity = Vector3.zero;
                //We check if we have a rigidbody bellow us
                _hit.collider.TryGetComponent(out Rigidbody other);
                if (other != null)
                {
                    _otherRigidBody = other;
                    otherVelocity = other.velocity;
                    _rigidBody.AddForce(otherVelocity, ForceMode.VelocityChange);
                }
                //We create jump forces
                Vector3 jumpImpulse = Vector3.zero;
                //todo: manage stamina
                if (_wantJump && _grounded && _jumpCool == 0 && AllowJump && Stamina > 10)
                {
                    _jumpCool = 1;
                    _currentState = PlayerMovementState.JUMP;
                    jumpImpulse = (Vector3.up) * _jumpForce;
                    _wantJump = false;
                    Stamina -= 10 - (10*_staminaResistance);
                }

                //Spring and Slipping forces
                Vector3 velocity = _rigidBody.velocity;
                Vector3 rayDirection = -transform.up;
                float rayDirectionVelocity = Vector3.Dot(rayDirection, velocity);
                float otherDirectionVelocity = Vector3.Dot(rayDirection, otherVelocity);
                float relativeVelocity = rayDirectionVelocity - otherDirectionVelocity;
                float x = _hit.distance - _rideHeight;
                float springForce = (x * _rideSpringStrength) - (relativeVelocity * _rideSpringDamper);
                //float dot = Vector3.Dot(transform.forward, _hit.normal);

                _rigidBody.AddForce((rayDirection * springForce));

                if (jumpImpulse.magnitude > 0)
                {
                    _rigidBody.AddForce(jumpImpulse + _jumpboost, ForceMode.Acceleration);
                }
                ManageMoving();

                /*
                if (other != null)
                {
                    other.AddForceAtPosition(rayDirection * -springForce, _hit.point);
                }*/
            }
            else ManageAirControl();
            ManageFall();
        }

        private void ManageFall()
        {
            if (!_grounded && _fallStart.y < transform.position.y) _fallStart = transform.position;

            if (_grounded != _lastIsGrounded)
            {
                if (!_grounded) _fallStart = transform.position;
                else
                {
                    _fallEnd = transform.position;
                    _currentState = PlayerMovementState.LANDING;
                    if (_fallEnd.y < _fallStart.y) { NotifyFall(_fallEnd, _fallStart); } // como carajo no?
                }
                _lastIsGrounded = _grounded;
            }
        }

        private void NotifyFall(Vector3 end, Vector3 start)
        {
            PlayerFallEvent?.Invoke(start, end);
        }

        private void ManageAirControl()
        {
            airControl = transform.right * _inputBody.x * _airSpeed;
            if (Mathf.Abs(transform.InverseTransformDirection(_rigidBody.velocity).x) < _airSpeed)
            {
                _rigidBody.AddForce(airControl);
            }
            //if (_wantCrouching) { _currentState = PlayerMovementState.CROUCH; return; }
            _currentState = PlayerMovementState.FALLING;
        }

        private void OnMove(InputValue value)
        {
            _inputBody = value.Get<Vector2>();
        }

        private void OnLook(InputValue value)
        {
            _inputHead = value.Get<Vector2>();
        }

        private void ManageMoving()
        {
            Vector3 slideForce = Vector3.zero;
            float angle = Vector3.Angle(Vector3.up, _hit.normal);
            _slipping = angle > _maxSteepAngle;
            Vector3 planeVelocity = _rigidBody.velocity - (_otherRigidBody ? _otherRigidBody.velocity : Vector3.zero);
            planeVelocity.y = 0;

            if (AllowMovement)
            {
                _move.x = _inputBody.x;
                _move.z = _inputBody.y;
            }
            else _move = Vector3.zero;

            Vector3.ClampMagnitude(_move, 1);
            _move *= _walkSpeed; 
            bool canSprint = _move.z > 0 && _grounded && AllowSprint && Stamina > 5;
            if (canSprint && _wantSprinting)
            {
                _move.z += (_sprintSpeed - _walkSpeed);

                Debug.Log(_staminaDecrement * _staminaResistance);
                Stamina -= (Time.fixedDeltaTime * (_staminaDecrement - (_staminaDecrement * _staminaResistance)));
            }
            if (_wantCrouching) { _move = Vector3.ClampMagnitude(_move, _crouchSpeed); }

            _currentState = _wantCrouching ? PlayerMovementState.CROUCH : _wantSprinting && canSprint ? PlayerMovementState.SPRINT : _move.magnitude > 0 ? PlayerMovementState.WALK : PlayerMovementState.IDLE;

            _move = transform.TransformDirection(_move);
            _move.y = 0;
            _jumpboost = _move;

            Vector3 goalVelocity = _move;
            _totalGoalVelocity = Vector3.MoveTowards(_totalGoalVelocity, goalVelocity, _walkAcceleration * Time.fixedDeltaTime);
            Vector3 neededAcceleration = (_totalGoalVelocity - _rigidBody.velocity) / Time.fixedDeltaTime;
            Vector3.ClampMagnitude(neededAcceleration, _maxWalkAcceleration);
            neededAcceleration = Vector3.MoveTowards(neededAcceleration, Vector3.zero, angle * Time.fixedDeltaTime);
            neededAcceleration.y = 0;
            Vector3 movingForce = neededAcceleration * _rigidBody.mass;

            if (_hit.collider != null)
            {
                movingForce = Vector3.ProjectOnPlane(movingForce, _hit.normal.normalized);
            }
            if (_slipping)
            {
                slideForce = Vector3.ProjectOnPlane(-transform.up, _hit.normal);
            }

            _rigidBody.AddForce(movingForce + slideForce * _rideSpringStrength);

            Debug.DrawRay(transform.position, movingForce, Color.red);
            Debug.DrawRay(transform.position, slideForce, Color.green);
        }

        private void OnGUI()
        {
            GUILayout.Space(100);
            GUILayout.Label($"Velocity: {Velocity}");
            GUILayout.Label($"Relative Velocity: {RelativeVelocity}");
            GUILayout.Label($"Grounded: {_grounded}");
            GUILayout.Label($"CurrentState: {_currentState}");
            GUILayout.Label($"LastState: {_lastState}");
            GUILayout.Label($"Fall Damage from last fall: {_fallEnd.y - _fallStart.y}");
            GUILayout.Space(100);
            GUILayout.Label($"Stamina Resistance{_staminaResistance}");
            GUILayout.Label($"Stamina{_staminaDecrement - (_staminaDecrement * _staminaResistance)}");
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(_fallStart, .25f);
            Gizmos.DrawSphere(_fallEnd, .25f);
            Gizmos.DrawLine(_fallStart, _fallEnd);
        }

        public void Push(Vector3 worldDirection)
        {
            _rigidBody.AddForce(worldDirection);
        }

        internal void Teletransport(Vector3 position)
        {
            _rigidBody.position = position;
        }

        private bool _pauseMovement;
       

        internal void Simulate(bool value)
        {
            _rigidBody.detectCollisions = value;
            _rigidBody.useGravity = value;
            _rigidBody.isKinematic = !value;
            _rigidBody.interpolation = value ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            _pauseMovement = !value;
        }

        internal void Die()
        {
            _pauseMovement = true;
            _rigidBody.isKinematic = false;
            _rigidBody.useGravity = true;
            _rigidBody.freezeRotation = false;
            _collider.material = default;
            _rigidBody.AddForce(_rigidBody.velocity);
        }
    }
}