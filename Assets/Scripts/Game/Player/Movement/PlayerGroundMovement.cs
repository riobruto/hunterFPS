using Nomnom.RaycastVisualization;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    public enum GroundMovementState
    {
        SPRINT,
        WALK,
        IDLE,
        LANDING,
        JUMP_START,
        JUMP_LAND
    }

    public delegate void PlayerGroundMovementDelegate(GroundMovementState last, GroundMovementState current);

    public delegate void PlayerGroundCrouchDelegate(bool state);

    public delegate void PlayerGroundFallDelegate(float distance);

    public class PlayerGroundMovement : PlayerBaseMovement
    {
        public event PlayerGroundMovementDelegate ChangeStateEvent;

        public event PlayerGroundCrouchDelegate CrouchEvent;

        public event PlayerGroundFallDelegate FallEvent;

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
        private bool _isGrounded => _groundedInPlane;
        private bool _sprint;
        private bool _groundedInPlane;
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

        private Vector3 _fallStartingPosition;
        private Vector3 _fallEndPosition;

        private float _refHeightVelocity;
        [SerializeField] private float _crouchTime = 2f;
        private float _controllerDesiredHeight = 2f;

        private RaycastHit _slopeHit;
        private float _maxSlopeAngle = 45f;
        private bool _jump;
        private Vector3 _smoothStepCorrection;

        public Vector3 CurrentMovement
        { get { return (_smoothVector + _verticalVector); } }

        public Vector3 RigidbodyFollowVelocity => _rigidbodyFollowVelocity;
        public bool IsGrounded { get => _isGrounded; }

        public bool Active;

        protected override void OnStart()
        {
            _desiredSpeed = Manager.Settings.WalkSpeed;
            _currentState = GroundMovementState.IDLE;
            _fallStartingPosition = transform.position;
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
                ManageFallDamage();
                _currentState = _smoothVector == Vector3.zero ? GroundMovementState.IDLE : _canSprint && _sprint ? GroundMovementState.SPRINT : GroundMovementState.WALK;
            }
        }

        private void ManageFallDamage()
        {
            if (_wasGroundedLastFrame != _isGrounded)
            {
                if (!_isGrounded) { _fallStartingPosition = transform.position; }

                if (_isGrounded)
                {
                    _fallEndPosition = transform.position;
                    float falldistance = Vector3.Distance(_fallStartingPosition, _fallEndPosition);
                    FallEvent?.Invoke(falldistance);
                }

                _wasGroundedLastFrame = _isGrounded;
            }
        }

        protected override void OnFixedUpdate()
        {
            if (_steppedRigidbody != null)
            {
                _rigidbodyFollowVelocity = _steppedRigidbody.velocity;
            }
            else _rigidbodyFollowVelocity = Vector3.zero;
        }

        private void ManageMovement()
        {
            bool slide = !CheckPlaneSlope();

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

            if (_groundedInPlane)
            {
                if (slide) target += Vector3.down * Vector3.Dot(transform.up, _slopeHit.normal.normalized);
                target = Vector3.ProjectOnPlane(target, _slopeHit.normal.normalized);
                Debug.DrawRay(transform.position, target.normalized, Color.yellow);
            }

            Manager.Controller.Move((target + _verticalVector + _rigidbodyFollowVelocity) * Time.deltaTime);
        }

        public void StopMovement()
        {
            _move = Vector3.zero;
            _smoothVector = Vector3.zero;
            _verticalVector = Vector3.zero;
        }

        private IEnumerator Jump()
        {
            _verticalVector.y = Manager.Settings.JumpHeight;
            Manager.Stamina -= 5f;
            _tryingToJump = true;
            _currentState = GroundMovementState.JUMP_START;
            yield return new WaitForSeconds(1);
            _tryingToJump = false;
            yield return null;
        }

        private bool _wasGroundedLastFrame;

        private void ManageGravity()
        {
            if (_jump && AllowJump && !_tryingToJump)
            {
                StartCoroutine(Jump());
                _jump = false;
                return;
            }

            if (_isGrounded && !_tryingToJump)
            {
                _verticalVector.y = -0.001f;
                _currentState = GroundMovementState.JUMP_LAND;
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


        [SerializeField] private float _stepCorrectionDistance = .5f;



        [SerializeField] private float _planeCheckRadius = 0.1f;
        [SerializeField] private float _planeCheckDistance = 0.1f;
        [SerializeField] private float _planeCheckAngle = 80f;
        private bool _tryingToJump;

        private bool _useBoxcast = true;

        private Rigidbody _steppedRigidbody;
        private Vector3 _rigidbodyFollowVelocity;

        private bool CheckPlaneSlope()
        {
            if (_useBoxcast)
            {
                if (VisualPhysics.BoxCast(transform.position + Manager.Controller.center,
                   Vector3.one / 2 * (Manager.Controller.radius - Manager.Controller.skinWidth - 0.05f + _planeCheckRadius),
                   Vector3.down,
                   out _slopeHit,
                   transform.rotation,
                  (Manager.Controller.height * .5f) - Manager.Controller.radius + Manager.Controller.skinWidth + 0.1f + _planeCheckDistance))
                {
                    FetchRigidbodyOnPlane();

                    float angle  = Vector3.Angle(Vector3.up, _slopeHit.normal);
                    //Debug.DrawRay(_slopeHit.point, _slopeHit.normal, Color.red);
                    _groundedInPlane = angle < _planeCheckAngle;
                    return angle < _maxSlopeAngle;
                }
                _groundedInPlane = false;
                return false;
            }

            if (VisualPhysics.SphereCast(transform.position + Manager.Controller.center,
                Manager.Controller.radius - Manager.Controller.skinWidth - 0.05f + _planeCheckRadius,
                Vector3.down,
                out _slopeHit,
               (Manager.Controller.height * .5f) - Manager.Controller.radius + Manager.Controller.skinWidth + 0.1f + _planeCheckDistance))
            {
                FetchRigidbodyOnPlane();
                float angle  = Vector3.Angle(Vector3.up, _slopeHit.normal);
                //Debug.DrawRay(_slopeHit.point, _slopeHit.normal, Color.red);
                _groundedInPlane = angle < _planeCheckAngle;
                return angle < _maxSlopeAngle;
            }
            _groundedInPlane = false;
            return false;
        }

        private void FetchRigidbodyOnPlane()
        {
            if (_slopeHit.collider.TryGetComponent(out Rigidbody rb))
            {
                _steppedRigidbody = rb;
            }
            else _steppedRigidbody = null;
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