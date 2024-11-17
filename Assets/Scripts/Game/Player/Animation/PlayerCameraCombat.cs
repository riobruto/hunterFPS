using Core.Engine;
using Game.Player.Controllers;
using Game.Player.Movement;
using Game.Player.Weapon;
using System.Collections;
using UnityEngine;

namespace Game.Player.Animation
{
    public class PlayerCameraCombat : MonoBehaviour, IObserverFromPlayerMovement, IObserverFromPlayerWeapon
    {
        private PlayerWeapons _wController;
        private PlayerMovementController _mController;
        private GameSettings _settings;
        private Camera _camera;
        private PlayerHealth _health;

        void IObserverFromPlayerWeapon.Initalize(PlayerWeapons controller)
        {
            _camera = Camera.main;
            _settings = Bootstrap.Resolve<GameSettings>();
            _wController = controller;
            _wController.WeaponAimEvent += OnAim;
        }

        private void OnAim(bool state)
        {
            _isAimig = state;
        }

        private Transform tracker;

        void IObserverFromPlayerMovement.Initalize(PlayerMovementController controller)
        {
            _mController = controller;
            _mController.GroundMovement.ChangeStateEvent += OnStateChange;
            _health = controller.GetComponent<PlayerHealth>();
            _health.DeadEvent += OnDie;
        }

        private void OnDie()
        {
            StartCoroutine(KillCam());
        }

        private IEnumerator KillCam()
        {
            Vector3 targetPos = Vector3.down;
            Quaternion targetRot = Quaternion.Euler(-10, 0, 30);
            Vector3 startPos = _camera.transform.localPosition;
            Quaternion startRot = _camera.transform.localRotation;
            float time = 0;

            while (time < .25f)
            {
                _camera.transform.localPosition = Vector3.Lerp(startPos, targetPos, time / 1);
                _camera.transform.localRotation = Quaternion.Slerp(startRot, targetRot, time / 1);
                time += 0.01f;
                yield return null;
            }
            _camera.transform.localPosition = targetPos;
            _camera.transform.localRotation = targetRot;

            yield return null;
        }

        private void OnStateChange(GroundMovementState last, GroundMovementState current)
        {
            switch (current)
            {
                case GroundMovementState.SPRINT:
                case GroundMovementState.WALK:
                case GroundMovementState.IDLE:
                    _desiredFov = _settings.PlayerConfiguration.Settings.FOVGround;
                    break;
            }
        }

        private float _desiredFov = 60;
        private bool _isAimig;
        private float _refFOVVelocity;





        private void LateUpdate()
        {
            _camera.fieldOfView = Mathf.SmoothDamp(_camera.fieldOfView, _desiredFov, ref _refFOVVelocity, 0.15f);

            if (_isAimig)
            {
                _desiredFov = _wController.WeaponEngine.WeaponSettings.Aim.FieldOfView;
                return;
            }

            _desiredFov = _settings.PlayerConfiguration.Settings.FOVGround;
        }

        void IObserverFromPlayerWeapon.Detach(PlayerWeapons controller)
        {
            _wController = null;
        }

        void IObserverFromPlayerMovement.Detach(PlayerMovementController controller)
        {
            _mController = null;
        }
    }
}