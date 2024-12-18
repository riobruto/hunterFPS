﻿using Core.Engine;
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

        [SerializeField] private GameObject WeaponVisual;
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

            GetComponent<Animator>().SetInteger("WEAPON_TYPE", (int)_weaponType);
        }

        public void DropWeapon()
        {
            if (WeaponVisual)
            {
                WeaponVisual.AddComponent<MeshCollider>().convex = true;

                WeaponVisual.AddComponent<Rigidbody>().isKinematic = false;
                WeaponVisual.AddComponent<PickableDropWeaponEntity>().SetAsset(_weapon);
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
                ManageFireSound();
                _weaponParticleSystem.Play();
                GetComponent<Animator>().SetTrigger("FIRE");
            }

            if (e.State == WeaponState.BEGIN_RELOADING)
            {
                AudioToolService.PlayClipAtPoint(_reloadSound, transform.position, 1, AudioChannels.AGENT);
                GetComponent<Animator>().SetTrigger("RELOAD");
            }
        }

        private void ManageFireSound()
        {
            AudioToolService.PlayGunShot(_fireSound, _fireFarSound, _weaponTransform.position, _playerCamera.transform.position, 20, 1, AudioChannels.AGENT);
        }

        private void Update()
        {
            _aimTarget = _playerCamera.transform.position - Vector3.up * .5f;
            _weaponEngine.SetMovementDelta(Random.insideUnitCircle * 10f);
            if (_weaponTransform != null) _weaponTransform.LookAt(_aimTarget);
        }
    }
}