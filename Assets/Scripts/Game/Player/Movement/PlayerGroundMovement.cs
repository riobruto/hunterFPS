using Nomnom.RaycastVisualization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    public enum GroundMovementState
    {
        SPRINT,
        WALK,
        IDLE,
        LANDING
    }

    public delegate void PlayerGroundMovementDelegate(GroundMovementState last, GroundMovementState current);

    public delegate void PlayerGroundCrouchDelegate(bool state);

    public class PlayerGroundMovement : PlayerBaseMovement
    {
        public event PlayerGroundMovementDelegate ChangeStateEvent;

        public event PlayerGroundCrouchDelegate CrouchEvent;

        private GroundMovementState _currentState;
        private GroundMovementState _lastState;
        private Vector2 _inputBody;
        private float _walkSpeed => Manager.Settings.WalkSpeed;
        private float _sprintSpeed => Manager.Settings.RunSpeed;
        private float _crouchSpeed => Manager.Settings.CrouchSpeed;
        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;

        private float _desiredSpeed;
        private float _currentSpeed;
        private Vector3 _move = new Vector3(0, 0, 0);
        private float _jumpTime = 0;
        private bool _isGrounded => CheckGroundCollision();
        private bool _sprint;
        private bool _canSprint => _isGrounded && _move.z > 0.01f && !_crouch && AllowSprint && Manager.Stamina > 10f;
        private Vector3 _sprintVector;

        private bool _crouch;
        private bool _canCrouch => AllowCrouch;

        internal bool IsSprinting => _sprint && _canSprint;
        internal bool AllowSprint;
        internal bool IsCrouching => _crouch && _canCrouch;
        internal bool AllowCrouch;

        internal bool AllowJump;

        internal bool AllowInputMovement;
        private Vector3 _smoothVector;
        private Vector3 _refSmoothVelocity;
        private Vector3 _verticalVector;

        private float _refHeightVelocity;
        [SerializeField] private float _crouchTime = 2f;
        private float _controllerDesiredHeight = 2f;

        private RaycastHit _slopeHit;
        private float _maxSlopeAngle = 5;
        private bool _jump;

        public Vector3 CurrentMovement
        { get { return _smoothVector + _verticalVector; } }

        public bool IsGrounded { get => _isGrounded; }

        public bool Active;

        protected override void OnStart()
        {
            _desiredSpeed = Manager.Settings.WalkSpeed;
            _currentState = GroundMovementState.IDLE;
        }

        private void OnCrouch(InputValue value)
        {
            if (!_canCrouch) return;

            _crouch = !_crouch;

            if (_crouch)
            {
                _controllerDesiredHeight = 1.25f;
                _desiredSpeed = _crouchSpeed;
                CrouchEvent?.Invoke(true);
            }

            if (!_crouch)
            {
                if (!CheckHeadObstruction())
                {
                    _desiredSpeed = _walkSpeed;
                    _controllerDesiredHeight = 2;
                    CrouchEvent?.Invoke(false);
                }
                else
                {
                    _desiredSpeed = _crouchSpeed;
                    _controllerDesiredHeight = 1.25f;
                }
            }
        }

        private void OnMove(InputValue value)
        {
            _inputBody = value.Get<Vector2>();
        }

        private void OnSprint(InputValue value)
        {
            _sprint = !_sprint;
            _sprintVector = new Vector3(0, 0, _sprint ? _sprintSpeed - _walkSpeed : 0);
        }

        private void OnJump(InputValue value)
        {
            _jump = value.isPressed && _isGrounded && Manager.Stamina > 20f && !_crouch;
        }

        // Update is called once per frame
        protected override void OnUpdate()
        {
            ManageCrouchResize();

            if (_currentState != _lastState)
            {
                NotifyState(_lastState, _currentState);
                _lastState = _currentState;
            }

            if (Active)
            {
                ManageGravity();
                ManageMovement();

                _currentState = _smoothVector == Vector3.zero ? GroundMovementState.IDLE : _canSprint && _sprint ? GroundMovementState.SPRINT : GroundMovementState.WALK;
            }

            //ManageLook();
        }

        private void ManageMovement()
        {
            _currentSpeed = _desiredSpeed;
            _move.x = _inputBody.x * _currentSpeed * (AllowInputMovement ? 1 : 0);
            _move.z = _inputBody.y * _currentSpeed * (AllowInputMovement ? 1 : 0);

            //Add the speed vector only if we can run;

            if (_canSprint)
            {
                _move += _sprintVector;
                if (_sprintVector != Vector3.zero)
                {
                    Manager.Stamina -= Time.deltaTime * 2f;
                }
            }

            _smoothVector = Vector3.SmoothDamp(_smoothVector, _move, ref _refSmoothVelocity, .10f);

            Vector3 target = transform.TransformDirection(_smoothVector);
            CheckGroundSlope();
            target = Vector3.ProjectOnPlane(target, _slopeHit.normal.normalized);
            Debug.DrawRay(transform.position, target.normalized, Color.yellow);
            Manager.Controller.Move((target + _verticalVector) * Time.deltaTime);
        }

        public void StopMovement()
        {
            _move = Vector3.zero;
            _smoothVector = Vector3.zero;
            _verticalVector = Vector3.zero;
        }

        private void ManageGravity()
        {
            if (_jump && AllowJump)
            {
                _verticalVector.y = Manager.Settings.JumpHeight;
                Manager.Stamina -= 10f;
                _jump = false;
                return;
            }

            if (_isGrounded)
            {
                _verticalVector.y = -0.001f;
                _jumpTime = Mathf.Clamp(_jumpTime + Time.deltaTime, 0, 1);
            }
            else if (!_isGrounded)
            {
                //Prevent Head Clipping

                if (Manager.Controller.collisionFlags == CollisionFlags.Above)
                {
                    if (_verticalVector.y > 0)
                    {
                        _verticalVector.y = 0;
                    }
                }
                _verticalVector.y -= Manager.Settings.Gravity * Time.deltaTime;
            }
        }

        private void ManageCrouchResize()
        {
            Manager.Controller.height = Mathf.SmoothDamp(Manager.Controller.height, _controllerDesiredHeight, ref _refHeightVelocity, _crouchTime);
            Manager.Controller.center = Vector3.up * ((Manager.Controller.height / 2f));
            Manager.Head.localPosition = (Vector3.up * (Manager.Controller.height)) - Vector3.up * 0.25f;
        }

        private bool CheckHeadObstruction()
        {
            return VisualPhysics.Raycast(transform.position + transform.up * 1.75f, transform.up, 1);//~_raycastConfiguration.IgnoreLayers);
        }

        private bool CheckGroundSlope()
        {
            if (VisualPhysics.Raycast(transform.position, Vector3.down, out _slopeHit, Manager.Controller.height * .5f + Manager.Controller.skinWidth * 2))
            {
                float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                Debug.DrawRay(_slopeHit.point, _slopeHit.normal, Color.red);
                return angle < _maxSlopeAngle && angle != 0;
            }

            return false;
        }

        private bool CheckGroundCollision()
        {
            Quaternion dir = _slopeHit.normal == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(_slopeHit.normal.normalized);
            return VisualPhysics.BoxCast(transform.position + transform.up, Vector3.one / 2.25f, -transform.up, out _slopeHit, dir, .550f + Manager.Controller.skinWidth * 2);
        }

        private void NotifyState(GroundMovementState last, GroundMovementState current)
        {
            ChangeStateEvent?.Invoke(last, current);
        }

        public void Impulse(Vector3 inertia)
        {
            _controllerDesiredHeight = 2f;

            _smoothVector.x = inertia.x;
            _smoothVector.z = inertia.z;
            _verticalVector.y = inertia.y;
        }
    }
}