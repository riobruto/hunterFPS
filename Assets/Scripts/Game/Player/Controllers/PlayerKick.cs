using Core.Engine;
using Game.Audio;
using Game.Hit;
using Game.Player.Animation;
using Game.Player.Sound;
using Game.Service;
using Nomnom.RaycastVisualization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Controllers
{
    public delegate void KickDelegate();

    public class PlayerKick : MonoBehaviour
    {
        [SerializeField] private int _frameDuration;
        [SerializeField] private AnimationHurtbox _hurtbox;
        [SerializeField] private Animator _animator;

        private float _lastKickTime = 0;
        private bool _isCooledDown => Time.time - _lastKickTime > 1;

        private bool _canKick => AllowKick;
        private bool _isKicking => _hurtbox.IsScanning;

        public bool AllowKick { get; internal set; }

        private PlayerCameraShake _shake;

        [SerializeField] private AudioClipGroup _kick;
        [SerializeField] private AudioClipGroup _swoosh;

        public event KickDelegate KickStartEvent;

        public event KickDelegate KickFinishEvent;

        private PlayerRigidbodyMovement _movement;

        private void Start()
        {
            _hurtbox.Initialize(Bootstrap.Resolve<GameSettings>().RaycastConfiguration.PlayerGunLayers, 25f);
            _movement = GetComponent<PlayerRigidbodyMovement>();
            _shake = FindObjectOfType<PlayerCameraShake>();
            _hurtbox.HurtContactEvent += OnContact;
        }

        private void OnContact(AnimationHurtbox hurtbox, IDamagableFromHurtbox[] contactedDamagables)
        {
            bool hit = contactedDamagables.Length > 0;
            KickFinishEvent?.Invoke();

            if (hit)
            {
                AudioToolService.PlayPlayerSound(_kick.GetRandom(), 1);
                _movement.Push(_hurtbox.transform.forward * -100+ transform.up * 10);
            }
            else
            {
                Ray kickRay = new Ray(_hurtbox.transform.position, _hurtbox.transform.forward);

                if (VisualPhysics.SphereCast(kickRay, .25f, 1.15f, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.PlayerGunLayers, QueryTriggerInteraction.Ignore))
                {
                    AudioToolService.PlayPlayerSound(_kick.GetRandom(), 1);
                    _movement.Push(_hurtbox.transform.forward * -100 + transform.up * 10);
                }
            }
        }

        private void OnKick(InputValue value)
        {
            if (!_canKick || !_isCooledDown || _isKicking) return;

            if (_movement.Stamina < 10) return;
            _hurtbox.StartScan(_frameDuration);
            _animator.SetTrigger("KICK");
            _lastKickTime = Time.time;
            _shake.Shake(-Vector3.right * 2f);
            KickStartEvent?.Invoke();
            AudioToolService.PlayPlayerSound(_swoosh.GetRandom(), 1);
            _movement.Stamina -= 10;
        }
    }
}