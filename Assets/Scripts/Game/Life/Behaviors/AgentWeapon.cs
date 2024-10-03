using Core.Engine;
using Core.Weapon;
using Game.Player.Weapon;
using Game.Player.Weapon.Engines;
using Game.Service;
using System.Collections;
using UnityEngine;

namespace Game.Life
{
    public class AgentWeapon : MonoBehaviour
    {
        [SerializeField] private AudioClip _fireSound;
        [SerializeField] private AudioClip _reloadSound;
        [SerializeField] private Transform _weaponTransform;
        [SerializeField] private ParticleSystem _weaponParticleSystem;

        [SerializeField] private WeaponSettings _weapon;
        private Vector3 _aimTarget;
        private Camera _playerCamera;
        private Transform _player => _playerCamera.transform;
        private IWeapon _weaponEngine;

        public Vector3 _target;
        public IWeapon WeaponEngine => _weaponEngine;
        public bool HasNoAmmo => _weaponEngine.CurrentAmmo == 0;
        public bool IsShooting => _weaponEngine.IsShooting;
        public bool IsReloading => _weaponEngine.IsReloading;

        private void Start()
        {
            _weaponEngine = _weaponTransform.gameObject.AddComponent<WeaponEngine>();
            _weaponEngine.Initialize(_weapon, _weapon.Ammo.Size, true, false);
            _weaponEngine.Activate();
            _weaponEngine.WeaponChangedState += OnWeaponChangeState;
            _weaponEngine.SetHitScanMask(Bootstrap.Resolve<GameSettings>().RaycastConfiguration.EnemyGunLayers);
            _playerCamera = Bootstrap.Resolve<PlayerService>().PlayerCamera;
        }

        private void OnWeaponChangeState(object sender, WeaponStateEventArgs e)
        {
            if (e.State == WeaponState.BEGIN_SHOOTING)
            {
                AudioSource.PlayClipAtPoint(_fireSound, _player.position + (transform.position - _player.position).normalized);
                _weaponParticleSystem.Play();
                GetComponent<Animator>().SetTrigger("FIRE");
            }

            if (e.State == WeaponState.BEGIN_RELOADING)
            {
                AudioSource.PlayClipAtPoint(_reloadSound, transform.position);
                GetComponent<Animator>().SetTrigger("RELOAD");
            }
        }

        private void Update()
        {
            _aimTarget = _playerCamera.transform.position - Vector3.up * .5f;
            _weaponEngine.SetMovementDelta(Random.insideUnitCircle * 3f);
            _weaponTransform.LookAt(_aimTarget);
        }
    }
}