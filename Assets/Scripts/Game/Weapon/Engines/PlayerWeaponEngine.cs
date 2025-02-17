using Core.Engine;
using Core.Weapon;
using Game.Service;
using Nomnom.RaycastVisualization;
using System;
using System.Collections;
using UnityEngine;

namespace Game.Player.Weapon.Engines
{
    public class PlayerWeaponEngine : MonoBehaviour, IWeapon
    {
        // todo: Esta clase debera:
        // Ser responsable de implementar el inventario, tanto la municion como los attachments.
        // esta clase puede considerar al inventario.
        // luego, al momento de la recarga, la municion debera sustraerse en el momento de INSERT.
        // tambien debera extenderse el tiempo de la recarga cuando este EMPTY.
        //

        private int _currentAmmo = 0;
        private LayerMask _currentLayerMask;
        private float _damage;
        private int _firePPM;
        private float _fireRatio;
        private bool _hasReleasedTrigger;
        private bool _isActive;
        private bool _isBoltOpen;
        private bool _isInitialized;
        private bool _isInserting;
        private bool _isManipulatingBolt;
        private bool _isReloading;
        private int _maxAmmo;
        private Vector2 _movementDelta;
        private Vector2 _noise;
        private bool _pinDeactivated;
        private bool _playerIsOwner = true;
        private float _swayMagnitude;
        private float _timeOfSpray;
        private bool _wantShooting;
        private InventorySystem _inventory;

        private WeaponSettings _weaponSettings;

        public event EventHandler<bool> WeaponActivatedState;

        public event EventHandler<WeaponStateEventArgs> WeaponChangedState;

        //interface fields
        bool IWeapon.Active => _isActive;

        bool IWeapon.BoltOpen => _isBoltOpen;
        bool IWeapon.Cocked => _weaponSettings.FireModes != WeaponFireModes.BOLT || !_pinDeactivated;
        int IWeapon.CurrentAmmo => _currentAmmo;
        float IWeapon.CurrentRecoil => _timeOfSpray;
        bool IWeapon.Empty => _currentAmmo == 0;
        bool IWeapon.Initialized => _isInitialized;
        bool IWeapon.IsOwnedByPlayer { get => _playerIsOwner; set => _playerIsOwner = value; }
        bool IWeapon.IsReloading => _isReloading;
        bool IWeapon.IsShooting => _isShooting;
        int IWeapon.MaxAmmo => _isInitialized ? _maxAmmo : 0;
        WeaponSettings IWeapon.WeaponSettings => _weaponSettings;
        Ray IWeapon.Ray => GetRay();

        public Vector2 RayNoise { get => _noise; set => _noise = value; }
        private bool _canInsert => _currentAmmo < _maxAmmo && _isBoltOpen && !_isManipulatingBolt && !_isReloading && !_wantShooting && !_isInserting && _isInitialized && _hasReleasedTrigger;
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

        private bool _canShoot => !_isReloading && _validFireRatio && !_isBoltOpen && _isActive && !_isManipulatingBolt && !_isInserting && !_pinDeactivated && _isInitialized;
        //this variable checks if the weapon has been cocked in order to shoot o re-cock bolt actions. SEMI and AUTO are automatically set to false

        private bool _isShooting => _wantShooting && _validFireRatio && CanFireFromFireMode() && _currentAmmo > 0;
        private bool _validFireRatio => _fireRatio <= float.Epsilon && _isInitialized;

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

        public Vector2 GetSprayValue() => _isInitialized ? _weaponSettings.GetSprayPatternValue(_timeOfSpray) * _weaponSettings.SprayMultiplier : Vector2.zero;

        void IWeapon.Initialize(WeaponSettings settings, int currentAmmo, bool cocked, bool isPlayerOwner)
        {
            _weaponSettings = settings;
            _pinDeactivated = !cocked;
            _isInitialized = true;
            _playerIsOwner = isPlayerOwner;
            _currentAmmo = currentAmmo;
            _maxAmmo = settings.Ammo.Size;
            _firePPM = settings.FireRatioPPM;
            _damage = settings.Damage;
            _swayMagnitude = settings.Sway.Magnitude;

            _inventory = InventoryService.Instance;
            _inventory.AttachmentAddedEvent += OnAddedAttachment;
            CheckForAttachmentOverrides();
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

        void IWeapon.Reload()
        {
            if (!_canReload) return;
            if (_weaponSettings.Reload.Mode == WeaponReloadMode.SINGLE && _weaponSettings.FireModes != WeaponFireModes.BOLT)
            {
                //TODO: EN EL ESPECIFICO CASO DE QUE EL ARMA SE DISPARE COMO SEMI PERO SE RECARGUE COMO BOLT!!!!!!!!!!
                return;
            }
            StartCoroutine(IReload());
        }

        public float Remap(float value, float maxIn, float minIn, float maxOut, float minOut)
        {
            float t = Mathf.InverseLerp(minIn, maxIn, value);
            return Mathf.Lerp(minOut, maxOut, t);
        }

        void IWeapon.SetHitScanMask(LayerMask mask)
        {
            _currentLayerMask = mask;
        }

        void IWeapon.SetMovementDelta(Vector2 value)
        {
            _movementDelta = value;
        }

        private bool CanFireFromFireMode()
        {
            if (_weaponSettings.FireModes == WeaponFireModes.AUTO) return true;
            return _hasReleasedTrigger;
        }

        private void CheckForAttachmentOverrides()
        {
            foreach (AttachmentSettings attachment in _weaponSettings.Attachments.AllowedAttachments)
            {
                if (_inventory.HasAttachment(attachment))
                {
                    if (attachment is MagazineAttachmentSetting)
                    {
                        _maxAmmo = (attachment as MagazineAttachmentSetting).CapacityOverride;
                    }
                    if (attachment is ActionAttachmentSetting)
                    {
                        _damage = (attachment as ActionAttachmentSetting).DamageOverride;
                        _firePPM = (attachment as ActionAttachmentSetting).FireRatePPMOverride;
                    }
                    if (attachment is GripAttachmentSetting)
                    {
                        _swayMagnitude = (attachment as GripAttachmentSetting).Sway;
                    }
                }
            }
        }

        private void CreateHitScan()
        {
            Vector3 offset = transform.right * .25f + transform.up * -.25f;

            if (_weaponSettings.Shot.Mode == WeaponShotType.SHOTGUN)
            {
                Vector2 spread = _weaponSettings.Shot.Spread;

                for (int i = 0; i < _weaponSettings.Shot.Amount; i++)
                {
                    Vector3 spreadVector = new Vector3(UnityEngine.Random.Range(-spread.x, spread.x), UnityEngine.Random.Range(-spread.y, spread.y), UnityEngine.Random.Range(-spread.y, spread.y));
                    Ray ray = new Ray(GetRay().origin, GetRay().direction + spreadVector);

                    if (VisualPhysics.Raycast(ray, out RaycastHit hit, 1000, _currentLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        Bootstrap.Resolve<HitScanService>().Dispatch(new HitWeaponEventPayload(hit, new Ray(ray.origin, ray.direction), _damage / _weaponSettings.Shot.Amount, _playerIsOwner));
                        Bootstrap.Resolve<ImpactService>().System.TraceAtPosition(ray.origin + offset, hit.point);
                    }
                    else Bootstrap.Resolve<ImpactService>().System.TraceAtPosition(ray.origin + offset, ray.origin + ray.direction * 100);
                }
            }
            else
            {
                Ray ray = new Ray(GetRay().origin, GetRay().direction);

                if (VisualPhysics.Raycast(ray, out RaycastHit hit, 1000, _currentLayerMask, QueryTriggerInteraction.Ignore))
                {
                    Bootstrap.Resolve<HitScanService>().Dispatch(new HitWeaponEventPayload(hit, new Ray(ray.origin, ray.direction), _damage, _playerIsOwner));
                    Bootstrap.Resolve<ImpactService>().System.TraceAtPosition(ray.origin + offset, hit.point);
                }
                else
                {
                    Bootstrap.Resolve<ImpactService>().System.TraceAtPosition(ray.origin + offset, ray.origin + ray.direction * 100);
                }
            }
        }

        private Ray GetRay()
        {
            Vector3 pointA = transform.position;
            Vector2 spray = GetSprayValue() * 10f;
            Vector3 pointB = pointA + transform.forward * 100;
            pointB += transform.right * (spray.x + _noise.x) + transform.up * (spray.y + _noise.y);
            pointB += (transform.right * _movementDelta.x + transform.up * _movementDelta.y) * _swayMagnitude;
            return new Ray(pointA, pointB - pointA);
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

            yield return null;
        }

        private IEnumerator IInsert()
        {
            if (!_inventory.HasAmmoOfType(_weaponSettings.Ammo.Type))
            {
                UIService.CreateMessage($"No {_weaponSettings.Ammo.Type.Name}");
                yield break;
            }

            _isManipulatingBolt = true;
            NotifyState(WeaponState.BEGIN_INSERT);
            _isInserting = true;

            if (_weaponSettings.Reload.FastReloadOnEmpty && _currentAmmo == 0)
            {
                _currentAmmo = _inventory.TakeAvaliableAmmo(_weaponSettings.Ammo.Type, _weaponSettings.Ammo.Size);
            }
            else
            {
                _currentAmmo += _inventory.TakeAvaliableAmmo(_weaponSettings.Ammo.Type, 1);
            }

            yield return new WaitForSeconds(_weaponSettings.Reload.InsertTime);
            _isInserting = false;
            _isManipulatingBolt = false;
            yield return null;
        }

        private IEnumerator IOpenBolt()
        {
            NotifyState(WeaponState.BEGIN_OPEN_BOLT);
            _isManipulatingBolt = true;
            _isBoltOpen = true;
            yield return new WaitForSeconds(_weaponSettings.Reload.BoltOpenTime);
            _isManipulatingBolt = false;
            yield return null;
        }

        private IEnumerator IReload()
        {
            if (_weaponSettings.FireModes != WeaponFireModes.BOLT)
            {
                if (!_inventory.HasAmmoOfType(_weaponSettings.Ammo.Type))
                {
                    UIService.CreateMessage($"No {_weaponSettings.Ammo.Type.Name}");
                    yield break;
                }
            }

            NotifyState(WeaponState.BEGIN_RELOADING);
            _isReloading = true;
            yield return new WaitForSeconds(_weaponSettings.Reload.EnterTime);
            if (_weaponSettings.FireModes != WeaponFireModes.BOLT)
            {
                bool isWeaponEmpty = _currentAmmo == 0;
                if (_currentAmmo == 0)
                {
                    _currentAmmo = _inventory.TakeAvaliableAmmo(_weaponSettings.Ammo.Type, _maxAmmo);
                }
                else if (_currentAmmo > 0)
                {
                    _currentAmmo += _inventory.TakeAvaliableAmmo(_weaponSettings.Ammo.Type, _maxAmmo - (_currentAmmo));
                }
                if (isWeaponEmpty) yield return new WaitForSeconds(_weaponSettings.Reload.ExitTime);
            }
            _pinDeactivated = false;
            _isReloading = false;

            yield return null;
        }

        private void NotifyState(WeaponState state)
        {
            WeaponStateEventArgs args = new WeaponStateEventArgs(state, this);
            WeaponChangedState?.Invoke(this, args);
        }

        private void OnAddedAttachment(AttachmentSettings item)
        {
            CheckForAttachmentOverrides();
        }

        private void OnDrawGizmos()
        {
            if (!_isInitialized) return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(GetRay());
            Gizmos.DrawWireSphere(transform.localPosition, 0.05f);
        }

        private void Update()
        {
            if (!_isInitialized) return;

            _fireRatio = Mathf.Clamp(_fireRatio - Time.deltaTime, 0, float.MaxValue);

            if (_wantShooting)
            {
                if (!_validFireRatio) return;
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


                if (_weaponSettings.Shot.Mode != WeaponShotType.PROJECTILE)
                {
                    CreateHitScan();
                }
                else CreateProjectile();
                _pinDeactivated = _weaponSettings.FireModes == WeaponFireModes.BOLT;
                _fireRatio = 60 / (float)_firePPM;
                _hasReleasedTrigger = false;
                _currentAmmo -= 1;
                _timeOfSpray = Mathf.Clamp(_timeOfSpray + _fireRatio, 0, float.MaxValue);
                NotifyState(WeaponState.BEGIN_SHOOTING);
            }

            if (!_isShooting)
            {
                _timeOfSpray = Mathf.Clamp(_timeOfSpray - Time.deltaTime * 2f, 0, float.MaxValue);
            }
        }

        private void CreateProjectile(){

            GameObject projectile = Instantiate(_weaponSettings.Shot.Projectile, GetRay().GetPoint(1.1f), Quaternion.LookRotation(GetRay().direction));
            projectile.TryGetComponent(out IProjectile projectileInterface);
            projectileInterface.Launch(GetRay().direction);

        }
    }
}