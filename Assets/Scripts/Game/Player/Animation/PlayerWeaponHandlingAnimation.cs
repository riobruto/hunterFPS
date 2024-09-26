using Game.Animation;
using Game.Player.Controllers;
using Game.Player.Movement;
using Game.Player.Weapon;
using System.Collections;
using UnityEngine;

namespace Game.Player.Animation
{
    public class PlayerWeaponHandlingAnimation : MonoBehaviour, IObserverFromPlayerMovement, IObserverFromPlayerWeapon
    {
        private bool _active;

        [SerializeField] private AnimationTransformCurve _crouchAnimation;

        [Header("One Shot Animation")]
        [SerializeField] private AnimationTransformCurve _crouchChangeAnimation;

        private Vector3 _crouchPosition;
        private Vector3 _crouchRotation;
        private Vector3 _currentPosition;
        private Vector3 _currentRotation;

        private Quaternion _finalRotation;

        [Header("Loop Animations")]
        [SerializeField] private AnimationTransformCurve _flyAnimation;

        private Vector3 _flyPosition;
        private Vector3 _flyRotation;
        [SerializeField] private AnimationTransformCurve _idleAnimation;
        private Vector3 _idlePosition;
        private Vector3 _idleRotation;
        private PlayerMovementController _mController;
        [SerializeField] private float _noiseFrequency;
        private Vector3 _obstructedRotation;

        private Vector3 _refSmoothVelocity;

        [SerializeField] private AnimationTransformCurve _runAnimation;
        private Vector3 _runPosition;
        private Vector3 _runRotation;
        [SerializeField] private float _smoothFactor;
        [SerializeField] private float _swayIntensity;
        private Transform _transform;
        private Vector3 _triggerPosition;
        private Vector3 _triggerRotation;
        [SerializeField] private AnimationTransformCurve _vaultAnimation;
        [SerializeField] private AnimationTransformCurve _walkAnimation;
        private Vector3 _walkPosition;
        private Vector3 _walkRotation;
        private PlayerWeapons _wController;
        private Vector3 _aimPosition;
        private Vector3 _aimRotation;
        private float _aimIntensityMultiplier = 1;

        void IObserverFromPlayerWeapon.Detach(PlayerWeapons controller)
        {
            _wController.WeaponObstructedEvent -= OnWeaponObstructed;
            //throw new System.NotImplementedException();
        }

        void IObserverFromPlayerMovement.Detach(PlayerMovementController controller)
        {
            throw new System.NotImplementedException();
        }

        void IObserverFromPlayerWeapon.Initalize(PlayerWeapons controller)
        {
            // Debug.LogWarning("AnimationHandler esta desactivado por defecto desde su inicializador");
            _active = true;
            _wController = controller;
            _transform = _wController.WeaponVisualElementHolder;
            SetWeaponEvents();
        }

        void IObserverFromPlayerMovement.Initalize(PlayerMovementController controller)
        {
            _mController = controller;
            SetMovementEvents();
            _aimPosition = Vector3.zero;
            _aimRotation = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_wController.WeaponEngine == null) return;

            Vector3 planeVelocity = _mController.RelativeVelocity;
            planeVelocity.y = 0;

            if (!_active) return;
            _flyAnimation.Evaluate(Time.time, out _flyPosition, out _flyRotation);
            _walkAnimation.Evaluate(Time.time, out _walkPosition, out _walkRotation);
            _runAnimation.Evaluate(Time.time, out _runPosition, out _runRotation);
            _idleAnimation.Evaluate(Time.time, out _idlePosition, out _idleRotation);
            _crouchAnimation.Evaluate(Time.time, out _crouchPosition, out _crouchRotation);

            if (_mController.IsFlying)
            {
                _currentPosition = _flyPosition;
                _currentRotation = _flyRotation;
            }
            else if (_mController.RelativeVelocity.magnitude < 0.01f)
            {
                _currentPosition = _idlePosition * _aimIntensityMultiplier;
                _currentRotation = _idleRotation * _aimIntensityMultiplier;
            }
            else
            {
                float walkRunInterpolation = Mathf.InverseLerp(_mController.GroundMovement.WalkSpeed, _mController.GroundMovement.SprintSpeed, planeVelocity.magnitude);

                Vector3 WalkRunPosition = Vector3.Lerp(_walkPosition, _runPosition, walkRunInterpolation) * _aimIntensityMultiplier;
                Vector3 WalkRunRotation = Vector3.Lerp(_walkRotation, _runRotation, walkRunInterpolation) * _aimIntensityMultiplier;

                _currentPosition = Vector3.Lerp(WalkRunPosition, _crouchPosition, _mController.IsCrouching ? 1 : 0);
                _currentRotation = Vector3.Lerp(WalkRunRotation, _crouchRotation, _mController.IsCrouching ? 1 : 0);
            }
            Vector3 speedRotation = new Vector3(_mController.RelativeVelocity.y, 0, _mController.RelativeVelocity.x);
            Quaternion rayDirection;

            rayDirection = _wController.WeaponEngine.Initialized ? Quaternion.LookRotation(transform.InverseTransformDirection(_wController.WeaponEngine.Ray.direction), Vector3.up) : Quaternion.identity;

            _transform.localPosition = Vector3.SmoothDamp(_transform.localPosition, _currentPosition + _triggerPosition + _aimPosition, ref _refSmoothVelocity, _smoothFactor);
            Vector3 swayRotation = new Vector3(_wController.MouseDelta.y, 0, -_wController.MouseDelta.x) * _aimIntensityMultiplier;
            _finalRotation = rayDirection * Quaternion.Euler(_currentRotation + _triggerRotation + _obstructedRotation + speedRotation + _aimRotation + swayRotation);
            _transform.localRotation = Quaternion.Slerp(_transform.localRotation, _finalRotation, Time.deltaTime / _smoothFactor);
            //transform.localScale = _wController.WeaponEngine.WeaponSettings.Aim.ScaleOffset;
        }

        private void OnCrouch(bool state)
        {
            { StartCoroutine(TriggerClip(_crouchChangeAnimation, !state)); }
        }

        private void OnVault(bool state)
        {
            if (state) { StartCoroutine(TriggerClip(_vaultAnimation, false)); }
        }

        private void OnWeaponObstructed(bool state)
        {
            if (state)
            {
                _obstructedRotation = new Vector3(10, -60, 10);
                return;
            }
            _obstructedRotation = Vector3.zero;
        }

        private void SetMovementEvents()
        {
            _mController.VaultMovement.VaultEvent += OnVault;
            _mController.GroundMovement.CrouchEvent += OnCrouch;
        }

        private void SetWeaponEvents()
        {
            _wController.WeaponObstructedEvent += OnWeaponObstructed;
            _wController.WeaponAimEvent += OnAim;
        }

        private void OnAim(bool state)
        {

            _aimPosition = state ? _wController.WeaponEngine.WeaponSettings.Aim.Position : _wController.WeaponEngine.WeaponSettings.Aim.RestPosition;
            _aimRotation = state ? _wController.WeaponEngine.WeaponSettings.Aim.Rotation : _wController.WeaponEngine.WeaponSettings.Aim.RestRotation;
            _aimIntensityMultiplier = state ? 0.125f : 1f;
        }

        private IEnumerator TriggerClip(AnimationTransformCurve clip, bool flipped)
        {
            float time = flipped ? 1 : 0;

            if (!flipped)
            {
                while (time < 1)
                {
                    _triggerRotation = clip.EulerRotation.Evaluate(time) * clip.RotationScaleMultiplier;
                    _triggerPosition = clip.Position.Evaluate(time) * clip.PositionScaleMultiplier;
                    time += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }

            if (flipped)
            {
                while (time > 0)
                {
                    _triggerRotation = clip.EulerRotation.Evaluate(time) * clip.RotationScaleMultiplier;
                    _triggerPosition = clip.Position.Evaluate(time) * clip.PositionScaleMultiplier;
                    time -= Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }

            _triggerRotation = Vector3.zero;
            _triggerPosition = Vector3.zero;

            yield return null;
        }
    }
}