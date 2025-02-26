using Core.Engine;
using Core.Weapon;
using Game.Service;
using Nomnom.RaycastVisualization;
using System;
using System.Collections;
using UnityEngine;

namespace Game.Player.Weapon.Engines
{
    public class AgentWeaponEngine : MonoBehaviour, IWeapon
    {
        //TODO: Simplificar comportamiento para NPCS
        private int _currentAmmo = 0;

        private float _fireRatio;
        private bool _hasReleasedTrigger;
        private bool _isActive;
        private bool _isBoltOpen;
        private bool _isInitialized;
        private bool _isInserting;
        private bool _isManipulatingBolt;
        private bool _isReloading;
        private int _maxAmmo;
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
        int IWeapon.MaxAmmo => _isInitialized ? _maxAmmo : 0;

        bool IWeapon.Empty => _currentAmmo == 0;

        bool IWeapon.Initialized => _isInitialized;

        Ray IWeapon.Ray => GetRay();

        private bool _canInsert => _currentAmmo < _maxAmmo && _isBoltOpen && !_isManipulatingBolt && !_isReloading && !_wantShooting && !_isInserting && _isInitialized && _hasReleasedTrigger;
        private bool _canShoot => !_isReloading && _validFireRatio && !_isBoltOpen && _isActive && !_isManipulatingBolt && !_isInserting && !_pinDeactivated && _isInitialized;

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

        private bool _validFireRatio => _fireRatio <= float.Epsilon && _isInitialized;

        bool IWeapon.IsReloading => _isReloading;

        bool IWeapon.IsShooting => _isShooting;

        private bool _isShooting => _wantShooting && _validFireRatio && CanFireFromFireMode() && _currentAmmo > 0;

        WeaponSettings IWeapon.WeaponSettings => _weaponSettings;

        public Vector2 RayNoise { get => _noise; set => _noise = value; }
        bool IWeapon.IsOwnedByPlayer { get => _playerIsOwner; set => _playerIsOwner = value; }

        float IWeapon.CurrentRecoil => _timeOfSpray;

        private bool _playerIsOwner = false;

        private InventorySystem _inventory;

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

            if (_playerIsOwner)
            {
                _inventory = InventoryService.Instance;
                _inventory.AttachmentAddedEvent += OnAddedAttachment;
                CheckForAttachmentOverrides();
            }
        }

        private void OnAddedAttachment(AttachmentSettings item)
        {
            CheckForAttachmentOverrides();
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

        private bool CanFireFromFireMode()
        {
            if (_weaponSettings.FireModes == WeaponFireModes.AUTO) return true;
            return _hasReleasedTrigger;
        }

        private IEnumerator ICloseBolt()
        {
            NotifyState(WeaponState.BEGIN_CLOSE_BOLT);
            _isManipulatingBolt = true;
            yield return new WaitForSeconds(_weaponSettings.Reload.BoltCloseTime);

            if (_currentAmmo > 0)
            {
                _pinDeactivated = false;
            }

            _isBoltOpen = false;
            _isManipulatingBolt = false;
         

            yield return null;
        }

        private IEnumerator IInsert()
        {
            
            _isManipulatingBolt = true;
            NotifyState(WeaponState.BEGIN_INSERT);
            _isInserting = true;

            if (_weaponSettings.Reload.FastReloadOnEmpty && _currentAmmo == 0)
            {
                //hack: la conmcha de dioooos
                //sobrescribe la municion disponible del player
                //deberia detectar si existe municion disponible para hacer esto.

                _currentAmmo = _weaponSettings.Ammo.Size;
            }
            else
            {
                _currentAmmo += 1;
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
           
            NotifyState(WeaponState.BEGIN_RELOADING);
            _isReloading = true;
            yield return new WaitForSeconds(_weaponSettings.Reload.EnterTime);
            //Si el arma no es bolt action
            if (_weaponSettings.FireModes != WeaponFireModes.BOLT)
            {
                if (_currentAmmo == 0)
                {
                    _currentAmmo = _maxAmmo;
                }
                else if (_currentAmmo != 0)
                {
                    _currentAmmo = _maxAmmo + 1;
                }
            }
            _pinDeactivated = false;
            _isReloading = false;
            yield return new WaitForSeconds(_weaponSettings.Reload.ExitTime);

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

                if (_weaponSettings.Shot.Mode == WeaponShotType.PROJECTILE) { CreateProjectile(); }
                else CreateHitScan();

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

        private void CreateHitScan()
        {
            Vector3 offset = _playerIsOwner ? transform.right * .25f + transform.up * -.25f : Vector3.zero;

            if (_weaponSettings.Shot.Mode == WeaponShotType.SHOTGUN)
            {
                Vector2 spread = _weaponSettings.Shot.Spread;

                for (int i = 0; i < _weaponSettings.Shot.Amount; i++)
                {
                    Vector3 spreadVector = new Vector3(UnityEngine.Random.Range(-spread.x, spread.x), UnityEngine.Random.Range(-spread.y, spread.y), UnityEngine.Random.Range(-spread.y, spread.y));
                    Ray ray = new Ray(GetRay().origin, GetRay().direction + spreadVector);

                    if (VisualPhysics.Raycast(ray, out RaycastHit hit, 1000, _currentLayerMask))
                    {
                        Bootstrap.Resolve<HitScanService>().Dispatch(new HitWeaponEventPayload(hit, new Ray(ray.origin, ray.direction), _damage / _weaponSettings.Shot.Amount, _playerIsOwner));

                        Bootstrap.Resolve<ImpactService>().System.TraceAtPosition(ray.origin + offset, hit.point);
                    }
                    else
                    {
                        Bootstrap.Resolve<ImpactService>().System.TraceAtPosition(ray.origin + offset, ray.origin + ray.direction * 100);
                    }
                }
            }
            else
            {
                Ray ray = new Ray(GetRay().origin, GetRay().direction);

                if (VisualPhysics.Raycast(ray, out RaycastHit hit, 1000, _currentLayerMask))
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
            /*
            pointA += transform.right;
            pointA += transform.up;
            pointA += transform.forward;
            */
            Vector3 pointB = pointA + transform.forward * 100;

            pointB += transform.right * (spray.x + _noise.x) + transform.up * (spray.y + _noise.y);
            pointB += (transform.right * _movementDelta.x + transform.up * _movementDelta.y) * _swayMagnitude;

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
        private float _damage;
        private int _firePPM;
        private float _swayMagnitude;

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

        private void CreateProjectile()
        {
            GameObject projectile = Instantiate(_weaponSettings.Shot.Projectile, GetRay().GetPoint(1.1f), Quaternion.LookRotation(GetRay().direction));
          
            projectile.TryGetComponent(out IProjectile projectileInterface);
            projectileInterface.Launch(GetRay().direction);
        }
    }
}