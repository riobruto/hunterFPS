using Game.Hit;
using Game.Player.Sound;
using Game.Service;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player.Controllers
{
    public class PlayerHealth : MonoBehaviour, IDamageableFromExplosive, IHittableFromWeapon
    {
        private float _maxHealth = 100f;
        private float _currentHealth = 100;
        private float _regenHealthLimit = 100f;
        private float _regenSpeed = 1f;

        private float _lastTimeHurt;
        private float _noHurtTimeForRegen = 2;
        private float _damageMultiplier = 0.1f;

        public float CurrentHealth => _currentHealth;
        public float CurrentMaxRegenHealth => _regenHealthLimit;

        public event UnityAction HurtEvent;

        private AudioSource _source;
        [SerializeField] private AudioClipCompendium _bodyHit;

        private void Update()
        {
            if (Time.time - _lastTimeHurt > _noHurtTimeForRegen)
            {
                _currentHealth += Time.deltaTime * _regenSpeed;
            }

            _currentHealth = Mathf.Clamp(_currentHealth, 0, _regenHealthLimit);
            _regenHealthLimit = Mathf.Clamp(_regenHealthLimit, 25f, 100f);
        }

        public void Hurt(float damage)
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

            _lastTimeHurt = Time.time;
            _currentHealth -= damage;
            _regenHealthLimit -= damage * _damageMultiplier;

            HurtEvent?.Invoke();
        }

        public void Heal(float amount)
        {
            _currentHealth += amount;
        }

        void IDamageableFromExplosive.NotifyDamage(float damage)
        {
            Hurt(damage);
        }

        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            Hurt(payload.Damage);
        }
    }
}