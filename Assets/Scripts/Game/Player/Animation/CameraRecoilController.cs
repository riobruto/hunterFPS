using Core.Weapon;
using Game.Player.Controllers;
using Game.Player.Movement;
using Game.Player.Weapon;
using UnityEngine;

namespace Game.Player.Animation
{
    public class CameraRecoilController : MonoBehaviour, IObserverFromPlayerWeapon
    {
        private Vector3 _recoilVector;
        private PlayerWeapons _c;
        private float _currentTime;
        [SerializeField] private AnimationCurve _recoilCurve;
        [SerializeField] private float _recoverSpeed = 1;
        [SerializeField] private float _randomIntensity;

        void IObserverFromPlayerWeapon.Initalize(PlayerWeapons controller)
        {
            _c = controller;
            controller.WeaponDrawEvent += OnDraw;

        }

        private void OnDraw(bool state)
        {
            if (state)
            {
                _c.WeaponEngine.WeaponChangedState += OnChangeState;
            }
            if (!state)
            {
                _c.WeaponEngine.WeaponChangedState -= OnChangeState;
            }
        }

        private void OnChangeState(object sender, WeaponStateEventArgs e)
        {
            if (e.State == WeaponState.BEGIN_SHOOTING)
            {
                _currentTime = 1;
                _recoilVector = e.Sender.WeaponSettings.RecoilShake;
                _recoilVector += UnityEngine.Random.insideUnitSphere * _randomIntensity;
                FindObjectOfType<PlayerLookMovement>().ImpulseLook(e.Sender.WeaponSettings.RecoilKick);
                _recoverSpeed = e.Sender.WeaponSettings.RecoilRecoverSpeed;
            }
        }

        private void LateUpdate()
        {
            _currentTime = Mathf.Clamp01(_currentTime - (Time.deltaTime * _recoverSpeed));
            Vector3 final = Vector3.Lerp(Vector3.zero, _recoilVector, _recoilCurve.Evaluate(_currentTime));
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(final), Time.deltaTime * 15f);
        }

        void IObserverFromPlayerWeapon.Detach(PlayerWeapons controller)
        {
            controller.WeaponEngine.WeaponChangedState -= OnChangeState;
        }
    }
}