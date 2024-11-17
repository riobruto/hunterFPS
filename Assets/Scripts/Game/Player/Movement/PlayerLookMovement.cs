using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    public class PlayerLookMovement : PlayerBaseMovement
    {
        private Vector2 _inputHead;
        private float _verticalLookAngle;

        internal bool AllowVerticalLook;
        internal bool AllowHorizontalLook;
        private bool _IsRecenteringLook = false;
        internal float Sensitivity;

        public float VerticalLookAngle => _verticalLookAngle;

        public void RecenterLook()
        {
            _IsRecenteringLook = true;
            StartCoroutine(IRecenterLook());
        }

        public void RecenterLook(float target)
        {
            _IsRecenteringLook = true;
            StartCoroutine(IRecenterLook(target));
        }

        public void ImpulseLook(Vector2 target)
        {
            _verticalLookAngle += target.x;
        }

        private void OnLook(InputValue value)
        {
            _inputHead = value.Get<Vector2>();
        }

        public Vector2 LookDelta => _inputHead;

        protected override void OnUpdate()
        {

            if (_IsRecenteringLook) return;

            if (AllowVerticalLook)
            {
                _verticalLookAngle = Mathf.Clamp(_verticalLookAngle - _inputHead.y * Sensitivity * Time.unscaledDeltaTime, -70, 80);
                Manager.Head.localRotation = Quaternion.Euler(_verticalLookAngle, 0, 0);
            }

            /*if (_smoothCameraYAxle)
            {
                if(_lastYheadPos - Manager.Head.position.y > .2f)
                {

                }

                Manager.Head.position = new Vector3(Manager.Head.position.x, Mathf.SmoothDamp(_lastYheadPos, Manager.Head.position.y, ref _currentYVelocity, 0.1f), Manager.Head.position.z);
            }*/

            if (AllowVerticalLook)
            {
                Manager.Controller.transform.Rotate(Vector3.up, _inputHead.x * Sensitivity * Time.unscaledDeltaTime);
            }

            _lastYheadPos = Manager.Head.position.y;
        }

        private float _refRecenterVelocity;

        private IEnumerator IRecenterLook()
        {
            while (Mathf.Abs(_verticalLookAngle) > 0.01f)
            {
                _verticalLookAngle = Mathf.SmoothDamp(_verticalLookAngle, 0, ref _refRecenterVelocity, .1f);
                Manager.Head.localRotation = Quaternion.Euler(_verticalLookAngle, 0, 0);
                yield return null;
            }
            _verticalLookAngle = 0;
            Manager.Head.localRotation = Quaternion.Euler(_verticalLookAngle, 0, 0);
            _IsRecenteringLook = false;
        }

        private float _refRecenterTargetVelocity;
        private bool _smoothCameraYAxle = true;
        private float _currentYVelocity;
        private float _lastYheadPos;

        private IEnumerator IRecenterLook(float target)
        {
            while (Mathf.Abs(_verticalLookAngle) - Mathf.Abs(target) > 0.01)
            {
                _verticalLookAngle = Mathf.SmoothDamp(_verticalLookAngle, target, ref _refRecenterTargetVelocity, .1f);
                Manager.Head.localRotation = Quaternion.Euler(_verticalLookAngle, 0, 0);
                yield return null;
            }
            _verticalLookAngle = target;
            Manager.Head.localRotation = Quaternion.Euler(_verticalLookAngle, 0, 0);
            _IsRecenteringLook = false;
        }
    }
}