using Core.Engine;
using Core.Weapon;
using Game.Audio;
using Game.Entities;
using Game.Player.Weapon;
using Game.Player.Weapon.Engines;
using Game.Service;
using UnityEngine;

namespace Game.Life
{
    public enum WeaponAnimationType
    { NONE, PISTOL, SMG, RIFLE, SHOTGUN }

    public class AgentFireWeapon : MonoBehaviour
    {
        [SerializeField] private AudioClip _fireSound;
        [SerializeField] private AudioClip _fireFarSound;
        [SerializeField] private AudioClip _reloadSound;
        [SerializeField] private Transform _weaponTransform;
        [SerializeField] private ParticleSystem _weaponParticleSystem;
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private WeaponSettings _weapon;
        [SerializeField] private WeaponAnimationType _weaponType;

        [SerializeField] private float _bulletDispersion = 1.8f;

        [SerializeField] private GameObject WeaponVisual;
        private Vector3 _aimTarget;
        private Camera _playerCamera;

        private IWeapon _weaponEngine;

        public IWeapon WeaponEngine => _weaponEngine;
        public bool Empty => _weaponEngine.CurrentAmmo == 0;
        public bool IsShooting => _weaponEngine.IsShooting;
        public bool IsReloading => _weaponEngine.IsReloading;

        public int MaxAmmo { get => _weaponEngine.MaxAmmo; }
        public int CurrentAmmo { get => _weaponEngine.CurrentAmmo; }
        public bool AllowReload { get => _allowReload; set => _allowReload = value; }

        public bool AllowFire;
        private bool _allowReload;
        private bool _shooting;
        private float _lastBurstTime;
        private float _burstTime;

        public void SetFireTarget(Vector3 target) => _aimTarget = target;

        private void Start()
        {
            _weaponEngine = _weaponTransform.gameObject.AddComponent<WeaponEngine>();
            _weaponEngine.Initialize(_weapon, _weapon.Ammo.Size, true, false);
            _weaponEngine.Activate();
            _weaponEngine.WeaponChangedState += OnWeaponChangeState;
            _weaponEngine.SetHitScanMask(Bootstrap.Resolve<GameSettings>().RaycastConfiguration.EnemyGunLayers);
            _playerCamera = Bootstrap.Resolve<PlayerService>().PlayerCamera;

            GetComponent<Animator>().SetInteger("WEAPON_TYPE", (int)_weaponType);
        }

        public void DropWeapon()
        {
            if (WeaponVisual)
            {
                //instance a prefab
                WeaponVisual.AddComponent<MeshCollider>().convex = true;
                WeaponVisual.AddComponent<Rigidbody>().isKinematic = false;
                //WeaponVisual.AddComponent<PickableDropWeaponEntity>().SetAsset(_weapon);

                WeaponVisual.transform.parent = null;
            }

            _weaponEngine.ReleaseFire();
            Destroy(_weaponTransform.gameObject);
        }

        private void OnWeaponChangeState(object sender, WeaponStateEventArgs e)
        {
            GetComponent<Animator>().SetInteger("WEAPON_TYPE", (int)_weaponType);

            if (e.State == WeaponState.BEGIN_SHOOTING)
            {
                AudioToolService.PlayGunShot(_fireSound, _fireFarSound, _weaponTransform.position, _playerCamera.transform.position, 20, 1, AudioChannels.AGENT);
                _weaponParticleSystem.Play();
                GetComponent<Animator>().SetTrigger("FIRE");
            }

            if (e.State == WeaponState.BEGIN_RELOADING)
            {
                AudioToolService.PlayClipAtPoint(_reloadSound, transform.position, 1, AudioChannels.AGENT, 5);
                GetComponent<Animator>().SetTrigger("RELOAD");
            }
        }

        private void Update()
        {
            _weaponEngine.SetMovementDelta(Random.insideUnitCircle * 10f * _bulletDispersion);
            if (_weaponTransform != null) _weaponTransform.LookAt(_aimTarget);

            ManageFiring();
        }

        private void ManageFiring()
        {
            if (_allowReload)
            {
                if (Empty)
                {
                    WeaponEngine.ReleaseFire();
                    WeaponEngine.Reload(WeaponEngine.MaxAmmo);
                    _shooting = false;
                    return;
                }
            }

            if (!AllowFire)
            {
                WeaponEngine.ReleaseFire();
                _shooting = false;
                return;
            }

            if (Time.time - _lastBurstTime > _burstTime)
            {
                _burstTime = Random.Range(0.5f, 1f);
                _lastBurstTime = Time.time;
                _shooting = !_shooting;
            }

            if (_shooting) WeaponEngine.Fire();
            else WeaponEngine.ReleaseFire();
        }
    }
}