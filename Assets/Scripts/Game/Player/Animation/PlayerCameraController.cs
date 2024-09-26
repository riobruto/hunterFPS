using Core.Engine;
using Game.Player.Controllers;
using Game.Player.Movement;
using Game.Player.Weapon;
using System;
using UnityEngine;

namespace Game.Player.Animation
{
    public class PlayerCameraController : MonoBehaviour, IObserverFromPlayerMovement, IObserverFromPlayerWeapon
    {
        private PlayerWeapons _wController;
        private PlayerMovementController _mController;
        private GameSettings _settings;
        private Camera _camera;

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