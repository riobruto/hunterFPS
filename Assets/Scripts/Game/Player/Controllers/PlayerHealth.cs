using Game.Hit;
using Game.Player.Sound;
using Game.Service;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player.Controllers
{
    public class PlayerHealth : MonoBehaviour, IDamageableFromExplosive, IHittableFromWeapon, IDamagableFromHurtbox
    {
        private float _maxHealth = 100f;
        private float _lastTimeHurt;
        private bool _dead;
        private float _damageMultiplier = 0.1f;
        private float _regenSpeed = 1f;
        private float _noHurtTimeForRegen = 2;
        private float _currentHealth = 100;
        private float _regenHealthLimit = 100f;

        private float _damageResistanceModifier = 0;

        public void SetDamageResistanceModifier(float value) => _damageResistanceModifier = value;

        public float CurrentHealth => _currentHealth;
        public float CurrentMaxRegenHealth => _regenHealthLimit;
        public bool Dead { get => _dead; }

        public event UnityAction<HurtPayload> HurtEvent;

        public event UnityAction DeadEvent;

        private AudioSource _source;
        [SerializeField] private AudioClipGroup _bodyHit;
        private Vector3 _lastDirection;
        private bool _inmune;

        public void SetInmunity(bool value) => _inmune = value;

        private void Update()
        {
            if (Time.time - _lastTimeHurt > _noHurtTimeForRegen) _currentHealth += Time.deltaTime * _regenSpeed;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _regenHealthLimit);
            _regenHealthLimit = Mathf.Clamp(_regenHealthLimit, 25f, 100f);
        }

        //TODO: Crear metodo para pasar direccion de daño
        public void Hurt(float damage, Vector3 direction)
        {
            if (_inmune) return;

            ManageSound();

            if (_dead) return;
            _lastTimeHurt = Time.time;
            _currentHealth -= damage - (damage * _damageResistanceModifier);
            _regenHealthLimit -= damage * _damageMultiplier;

            if (_currentHealth <= 0 && !_dead)
            {
                _dead = true;

                DeadEvent?.Invoke();
            }

            HurtEvent?.Invoke(new HurtPayload(damage, direction));
        }

        private void ManageSound()
        {
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }
            if (!_source.isPlaying)
            {
                _source.spatialBlend = 0;
                _source.volume = .5f;
                _source.PlayOneShot(_bodyHit.GetRandom());
            }
        }

        public void Heal(float amount)
        {
            _currentHealth += amount;
        }

        public void Restore()
        {
            _dead = false;
            _currentHealth = 100;
            _regenHealthLimit = 100;
        }

        void IDamageableFromExplosive.NotifyDamage(float damage, Vector3 position, Vector3 explosionDirection)
        {
            Hurt(damage, position - transform.position);
            _lastDirection = -explosionDirection;
        }

        void IHittableFromWeapon.Hit(HitWeaponEventPayload payload)
        {
            Hurt(payload.Damage, (payload.Ray.origin - transform.position).normalized);
        }

        void IDamagableFromHurtbox.NotifyDamage(float damage, Vector3 position, Vector3 direction)
        {
            Hurt(damage, direction);
        }
    }

    public class HurtPayload
    {
        public float Damage;
        public Vector3 Direction;

        public HurtPayload(float damage, Vector3 direction)
        {
            Damage = damage;
            Direction = direction;
        }
    }
}