using Core.Engine;
using Core.Weapon;
using Game.Service;
using Nomnom.RaycastVisualization;
using System;
using System.Collections;
using UnityEngine;

namespace Game.Player.Weapon.Engines
{
    public class WeaponEngine : MonoBehaviour, IWeapon
    {
        private int _currentAmmo = 0;
        private float _fireRatio;
        private bool _hasReleasedTrigger;
        private bool _isActive;
        private bool _isBoltOpen;
        private bool _isInitialized;
        private bool _isInserting;
        private bool _isManipulatingBolt;
        private bool _isReloading;
        private int _maxAmmo => _weaponSettings.Ammo.Size;
        private bool _pinDeactivated;
        private bool _wantShooting;
        private float _timeOfSpray;

        private WeaponSettings _weaponSettings;

        public event EventHandler<bool> WeaponActivatedState;

        public event EventHandler<WeaponStateEventArgs> WeaponChangedState;

        bool IWeapon.Active => _isActive;

        bool IWeapon.BoltOpen => _isBoltOpen;

        bool IWeapon.Cocked => _weaponSettings.FireModes == WeaponFireModes.BOLT ? !_pinDeactivated : true;

        int IWeapon.CurrentAmmo => _currentAmmo;
        int IWeapon.MaxAmmo => _isInitialized ? _weaponSettings.Ammo.Size : 0;

        bool IWeapon.Empty => _currentAmmo == 0;

        bool IWeapon.Initialized => _isInitialized;

        Ray IWeapon.Ray => GetRay();

        private bool _canInsert => _currentAmmo < _maxAmmo && _isBoltOpen && !_isManipulatingBolt && !_isReloading && !_wantShooting && !_isInserting && _isInitialized && _hasReleasedTrigger;
        private bool _canShoot => !_isReloading && ValidFireRatio && !_isBoltOpen && _isActive && !_isManipulatingBolt && !_isInserting && !_pinDeactivated && _isInitialized;

        private bool _canOpenBolt => !_wantShooting && !_isReloading && !_isManipulatingBolt && _isActive && _isInitialized && _weaponSettings.Reload.Mode == WeaponReloadMode.SINGLE && _hasReleasedTrigger;

        private bool _canReload => !_wantShooting && _isActive && !_isManipulatingBolt && !_isInserting && _canReloadFromFiremode && !_isReloading && !_isBoltOpen && _isInitialized && _hasReleasedTrigger && _currentAmmo < _maxAmmo;

        private bool _canReloadFromFiremode
        {
            get
            {
                if (_weaponSettings.FireModes != WeaponFireModes.BOLT) return true;
                else return _pinDeactivated;
            }
        }

        //this variable checks if the weapon has been cocked in order to shoot o re-cock bolt actions. SEMI and AUTO are automatically set to false

        private bool ValidFireRatio => _fireRatio <= float.Epsilon && _isInitialized;

        bool IWeapon.IsReloading => _isReloading;

        bool IWeapon.IsShooting => _isShooting;

        private bool _isShooting => _wantShooting && ValidFireRatio && CanFireFromFireMode() && _currentAmmo > 0;

        WeaponSettings IWeapon.WeaponSettings => _weaponSettings;

        public Vector2 RayNoise { get => _noise; set => _noise = value; }
        bool IWeapon.IsOwnedByPlayer { get => _playerIsOwner; set => _playerIsOwner = value; }

        float IWeapon.CurrentRecoil => _timeOfSpray;

        private bool _playerIsOwner = false;

        void IWeapon.Initialize(WeaponSettings settings, int currentAmmo, bool cocked, bool isPlayerOwner)
        {
            _weaponSettings = settings;
            _currentAmmo = currentAmmo;
            _pinDeactivated = !cocked;
            _isInitialized = true;
        }

        void IWeapon.Activate()
        {
            _isActive = true;
            WeaponActivatedState?.Invoke(this, _isActive);
        }

        void IWeapon.CloseBolt()
        {
            if (_isManipulatingBolt) return;
            StartCoroutine(ICloseBolt());
        }

        void IWeapon.Deactivate()
        {
            _isActive = false;
            WeaponActivatedState?.Invoke(this, _isActive);
        }

        bool IWeapon.Fire()
        {
            if (!_canShoot)
            {
                NotifyState(WeaponState.FAIL_SHOOTING); return false;
            }

            if (!CanFireFromFireMode()) return false;

            _wantShooting = true;
            return true;
        }

        bool IWeapon.Insert()
        {
            if (!_canInsert) return false;
            StartCoroutine(IInsert());
            return true;
        }

        void IWeapon.OpenBolt()
        {
            if (!_canOpenBolt) return;
            StartCoroutine(IOpenBolt());
        }

        void IWeapon.ReleaseFire()
        {
            _hasReleasedTrigger = true;

            _wantShooting = false;
        }

        void IWeapon.Reload(int amount)
        {
            if (!_canReload) return;

            if (_weaponSettings.Reload.Mode == WeaponReloadMode.SINGLE && _weaponSettings.FireModes != WeaponFireModes.BOLT)
            {
                //TODO: EN EL ESPECIFICO CASO DE QUE EL ARMA SE DISPARE COMO SEMI PERO SE RECARGUE COMO BOLT!!!!!!!!!!
                return;
            }

            StartCoroutine(IReload(amount));
        }

        private bool CanFireFromFireMode()
        {
            if (_weaponSettings.FireModes == WeaponFireModes.AUTO) return true;
            return _hasReleasedTrigger;
        }

        private IEnumerator ICloseBolt()
        {
            Debug.Log("CloseBoltBegin");
            NotifyState(WeaponState.BEGIN_CLOSE_BOLT);
            _isManipulatingBolt = true;
            yield return new WaitForSeconds(_weaponSettings.Reload.BoltCloseTime);

            if (_currentAmmo > 0)
            {
                _pinDeactivated = false;
            }

            _isBoltOpen = false;
            _isManipulatingBolt = false;
            Debug.Log("CloseBoltEnd");
            NotifyState(WeaponState.END_CLOSE_BOLT);
            yield return null;
        }

        private IEnumerator IInsert()
        {
            Debug.Log("InsertBegin");
            _isManipulatingBolt = true;
            NotifyState(WeaponState.BEGIN_INSERT);
            _isInserting = true;

            if (_weaponSettings.Reload.FastReloadOnEmpty && _currentAmmo == 0)
            {
                _currentAmmo = _weaponSettings.Ammo.Size;
            }
            else
            {
                _currentAmmo += 1;
            }

            yield return new WaitForSeconds(_weaponSettings.Reload.InsertTime);
            _isInserting = false;
            Debug.Log("InsertEnd");
            NotifyState(WeaponState.END_INSERT);
            _isManipulatingBolt = false;
            yield return null;
        }

        private IEnumerator IOpenBolt()
        {
            Debug.Log("OpenBoltBegin");
            NotifyState(WeaponState.BEGIN_OPEN_BOLT);
            _isManipulatingBolt = true;
            _isBoltOpen = true;
            yield return new WaitForSeconds(_weaponSettings.Reload.BoltOpenTime);
            _isManipulatingBolt = false;
            NotifyState(WeaponState.END_OPEN_BOLT);
            Debug.Log("OpenBoltEnd");
            yield return null;
        }

        private IEnumerator IReload(int amount)
        {
            Debug.Log("ReloadBegin");
            NotifyState(WeaponState.BEGIN_RELOADING);
            _isReloading = true;
            yield return new WaitForSeconds(_weaponSettings.Reload.EnterTime);
            //Si el arma no es bolt action
            if (_weaponSettings.FireModes != WeaponFireModes.BOLT)
            {
                if (_currentAmmo == 0)
                {
                    _currentAmmo = amount;
                }
                else if (_currentAmmo != 0)
                {
                    _currentAmmo = amount + 1;
                }
            }
            _pinDeactivated = false;
            _isReloading = false;
            yield return new WaitForSeconds(_weaponSettings.Reload.ExitTime);
            NotifyState(WeaponState.END_RELOADING);
            Debug.Log("ReloadEnd");
            yield return null;
        }

        private void NotifyState(WeaponState state)
        {
            WeaponStateEventArgs args = new WeaponStateEventArgs(state, this);
            WeaponChangedState?.Invoke(this, args);
        }

        private void Update()
        {
            if (!_isInitialized) return;

            _fireRatio = Mathf.Clamp(_fireRatio - Time.deltaTime, 0, float.MaxValue);

            if (_wantShooting)
            {
                if (!ValidFireRatio) return;
                if (!CanFireFromFireMode()) return;

                if (_currentAmmo <= 0)
                {
                    if (!_pinDeactivated)
                    {
                        if (_isReloading || _isManipulatingBolt || _isBoltOpen) 
                            return;

                        NotifyState(WeaponState.FAIL_SHOOTING);
                        _pinDeactivated = true;
                        _hasReleasedTrigger = false;
                    }

                    return;
                }

                NotifyState(WeaponState.BEGIN_SHOOTING);
                CreateHitScan();
                _pinDeactivated = _weaponSettings.FireModes == WeaponFireModes.BOLT;
                _fireRatio = 60 / _weaponSettings.FireRatioPPM;
                _hasReleasedTrigger = false;
                _currentAmmo -= 1;
                _timeOfSpray = Mathf.Clamp(_timeOfSpray + _fireRatio, 0, 1);
                
                NotifyState(WeaponState.END_SHOOTING);
            }

            if (!_isShooting)
            {
                _timeOfSpray = Mathf.Clamp(_timeOfSpray - Time.deltaTime * 2f, 0, float.MaxValue);
            }
        }

        private void CreateHitScan()
        {
            //TODO: Crear logica de escopeta. (para q se pueda noma)

            Ray ray = new Ray(GetRay().origin, GetRay().direction);

            if (VisualPhysics.Raycast(ray, out RaycastHit hit, 1000, _currentLayerMask))
            {
                Bootstrap.Resolve<HitScanService>().Dispatch(new HitWeaponEventPayload(hit, new Ray(ray.origin, ray.direction), _weaponSettings.Damage));

                Bootstrap.Resolve<ImpactService>().System.TraceAtPosition(ray.origin, hit.point);
            }
            else
            {
                Bootstrap.Resolve<ImpactService>().System.TraceAtPosition(ray.origin, ray.origin + ray.direction * 100);
            }
        }

        private Ray GetRay()
        {
            Vector3 pointA = transform.position;
            Vector2 spray = GetSprayValue() * 10f;
            /*
            pointA += transform.right;
            pointA += transform.up;
            pointA += transform.forward;
            */
            Vector3 pointB = pointA + transform.forward * 100;

            pointB += transform.right * (spray.x + _noise.x) + transform.up * (spray.y + _noise.y);
            pointB += (transform.right * _movementDelta.x + transform.up * _movementDelta.y) * _weaponSettings.Sway.Magnitude;

            return new Ray(pointA, pointB - pointA);
        }

        public Vector2 GetSprayValue() => _isInitialized ? _weaponSettings.GetSprayPatternValue(_timeOfSpray) * _weaponSettings.SprayMultiplier : Vector2.zero;

        public float Remap(float value, float maxIn, float minIn, float maxOut, float minOut)
        {
            float t = Mathf.InverseLerp(minIn, maxIn, value);
            return Mathf.Lerp(minOut, maxOut, t);
        }

        private Vector2 _movementDelta;
        private Vector2 _noise;
        private LayerMask _currentLayerMask;

        void IWeapon.SetMovementDelta(Vector2 value)
        {
            _movementDelta = value;
        }

        private void OnDrawGizmos()
        {
            if (!_isInitialized) return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(GetRay());
            Gizmos.DrawWireSphere(transform.localPosition, 0.05f);
        }

        void IWeapon.SetHitScanMask(LayerMask mask)
        {
            _currentLayerMask = mask;
        }
    }
}