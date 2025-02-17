using Core.Weapon;
using Game.Audio;
using Game.Entities;
using Game.Player.Movement;
using Game.Player.Sound;
using Game.Player.Weapon;
using Game.Service;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player.Controllers
{
    public class PlayerSoundController : MonoBehaviour, IObserverFromPlayerWeapon, IObserverFromPlayerMovement
    {
        public event UnityAction<Vector3, float> StepSound;

        public event UnityAction<Vector3, float> GunSound;

        [SerializeField] private AudioClipGroup _holster;
        [SerializeField] private AudioClipGroup _deploy;
        [SerializeField] private AudioClipGroup _aim;
        [SerializeField] private AudioClipGroup _hurt;

        [Header("Footsteps")]
        [SerializeField] private AudioClipGroup _footstepTerrain;

        [SerializeField] private AudioClipGroup _footstepConcrete;
        [SerializeField] private AudioClipGroup _footstepWood;
        [SerializeField] private AudioClipGroup _footstepMetal;
        [SerializeField] private AudioClipGroup _footstepDefault;
        [SerializeField] private AudioClipGroup _footstepWater;

        [Header("One Shots")]
        [SerializeField] private AudioClip _land;

        [SerializeField] private AudioClip _crouch;
        [SerializeField] private AudioClip _uncrouch;
        [SerializeField] private AudioClip _sprint;
        [SerializeField] private AudioClip _fall;
        [SerializeField] private AudioClip _jump;

        [SerializeField] private float _timeBetweenFootstep = 2.33f;
        private float _time;

        private PlayerRigidbodyMovement _controller;
        private PlayerWeapons _weapons;
        private PhysicalSurface _currentSurface;
        private PlayerHealth _health;

        // Update is called once per frame
        private void Update()
        {
            ManageFootsteps();
        }

        private void ManageFootsteps()
        {
            if (_controller.IsGrounded)
            {
                _time += (_controller.RelativeVelocity.magnitude) * Time.deltaTime;

                if (_time > _timeBetweenFootstep)
                {
                    PlayFootstep();
                    _time = 0;
                }
            }
        }

        private void PlayFootstep()
        {
            AudioClipGroup current = GetAudioClipFromSurface();
            AudioToolService.PlayPlayerSound(current.GetRandom(), .5f, .1f);
        }

        private AudioClipGroup GetAudioClipFromSurface()
        {
            _currentSurface = GetSurface();
            if (_currentSurface == null) return _footstepDefault;

            switch (_currentSurface.Type)
            {
                case Impact.SurfaceType.CERAMIC:
                case Impact.SurfaceType.GLASS:
                case Impact.SurfaceType.BRICK:
                case Impact.SurfaceType.ROCK:
                case Impact.SurfaceType.CONCRETE:
                    return _footstepConcrete;

                case Impact.SurfaceType.METAL:
                case Impact.SurfaceType.METAL_HARD:
                case Impact.SurfaceType.METAL_SOFT:
                    return _footstepMetal;

                case Impact.SurfaceType.CARTBOARD:
                case Impact.SurfaceType.PAPER:
                case Impact.SurfaceType.WOOD:
                case Impact.SurfaceType.WOOD_HARD:
                    return _footstepWood;

                case Impact.SurfaceType.RUBBER:
                case Impact.SurfaceType.NYLON:
                case Impact.SurfaceType.FLESH:
                    return _footstepDefault;

                case Impact.SurfaceType.DIRT:
                case Impact.SurfaceType.GRASS:
                    return _footstepTerrain;

                case Impact.SurfaceType.WATER:
                    return _footstepTerrain;

                default:
                    return _footstepDefault;
            }
        }

        private PhysicalSurface GetSurface()
        {
            if (_controller.Hit.point == Vector3.zero) return null;

            bool HasSurface = _controller.Hit.transform.root.TryGetComponent(out PhysicalSurface surface);
            if (HasSurface) return surface;
            else return null;
        }

        void IObserverFromPlayerWeapon.Initalize(PlayerWeapons controller)
        {
            _weapons = controller;
            _weapons.WeaponEngine.WeaponChangedState += OnWeaponChange;
            _weapons.WeaponAimEvent += OnAim;
            _weapons.WeaponSwapEvent += OnSwap;
        }

        private void OnSwap(WeaponSlotType type)
        {
            AudioToolService.PlayPlayerSound(_deploy.GetRandom(), .5f, .1f);
        }

        private void OnAim(bool state)
        {
            AudioToolService.PlayPlayerSound(_aim.GetRandom(), .5f, .1f);
        }

        private void OnWeaponChange(object sender, WeaponStateEventArgs e)
        {
            if (e.State == WeaponState.BEGIN_SHOOTING)
            {
                GunSound?.Invoke(transform.position, 40);
                StartCoroutine(PlayShellSound(e));
                DrawDebug(40f);

                foreach (AttachmentSettings attachment in e.Sender.WeaponSettings.Attachments.AllowedAttachments)
                {
                    if (attachment is MuzzleAttachmentSetting && InventoryService.Instance.HasAttachment(attachment))
                    {
                        AudioToolService.PlayPlayerSound((attachment as MuzzleAttachmentSetting).SoundOverride.GetRandom(), 1, .1f);
                        return;
                    }
                }
                AudioToolService.PlayPlayerSound(e.Sender.WeaponSettings.Audio.ShootClips.GetRandom(), 1, .1f);
            }

            if (e.State == WeaponState.BEGIN_RELOADING)
            {
                AudioToolService.PlayPlayerSound(_holster.GetRandom(), 1, .1f);
                GunSound?.Invoke(transform.position, 10);
                DrawDebug(10f);
            }
        }

        private IEnumerator PlayShellSound(WeaponStateEventArgs e)
        {
            if (e.Sender.WeaponSettings.FireModes == WeaponFireModes.BOLT) yield break;
            yield return new WaitForSeconds(1.25f);
            AudioToolService.PlayPlayerSound(e.Sender.WeaponSettings.Ammo.Type.ShellImpact.GetRandom(), .20f, .1f);
            yield return null;
        }

        private void DrawDebug(float v)
        {
            DrawLine(new Vector3(1, 0, 0) * v);
            DrawLine(new Vector3(-1, 0, 0) * v);
            DrawLine(new Vector3(0, 1, 0) * v);
            DrawLine(new Vector3(0, -1, 0) * v);
            DrawLine(new Vector3(0, 0, 1) * v);
            DrawLine(new Vector3(0, 0, -1) * v);
        }

        private void DrawLine(Vector3 direction)
        {
            Debug.DrawLine(transform.position, transform.position + direction, Color.blue, 1);
        }

        void IObserverFromPlayerMovement.Initalize(PlayerRigidbodyMovement controller)
        {
            _controller = controller;
            _controller.PlayerFallEvent += OnFall;
            _controller.PlayerStateEvent += OnMovementState;
            _health = _controller.GetComponent<PlayerHealth>();
            _health.HurtEvent += OnHurt;
        }

        private void OnHurt(HurtPayload arg0)
        {
            AudioToolService.PlayPlayerSound(_hurt.GetRandom(), .5f, .1f);
        }

        private void OnMovementState(PlayerMovementState current, PlayerMovementState next)
        {
            if (next == PlayerMovementState.JUMP)
            {
                AudioToolService.PlayPlayerSound(_jump, 1, .1f);
            }
            if (next == PlayerMovementState.CROUCH)
            {
                AudioToolService.PlayPlayerSound(_crouch, .75f, .1f);
            }
            if (next == PlayerMovementState.FALLING && current != PlayerMovementState.JUMP)
            {
                AudioToolService.PlayPlayerSound(_fall, .75f, .1f);
            }
            if (current == PlayerMovementState.FALLING && next == PlayerMovementState.LANDING)
            {
                AudioToolService.PlayPlayerSound(GetAudioClipFromSurface().GetRandom(), 1f, .1f);
                AudioToolService.PlayPlayerSound(_land, .75f, .1f);
            }
            if (current == PlayerMovementState.CROUCH)
            {
                AudioToolService.PlayPlayerSound(_uncrouch, .75f, .1f);
            }
        }

        private void OnFall(Vector3 start, Vector3 end)
        {
        }

        void IObserverFromPlayerWeapon.Detach(PlayerWeapons controller)
        {
        }

        void IObserverFromPlayerMovement.Detach(PlayerRigidbodyMovement controller)
        {
            _controller = null;
        }
    }
}