using Game.Entities;
using Game.Player.Sound;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Life
{
    public class AgentHealthBehavior : MonoBehaviour
    {
        private float _health = 100;
        private CharacterLimbHitbox[] _limbs;

        [SerializeField] private AudioClipCompendium _hurtSounds;
        private bool _dead => _health < 0;
        public float Health { get => _health; }

        public UnityEvent<float, float> DamageRecieved;

        public void SetHealth(float health)
        {
            _health = health;
        }

        private void Start()
        {
            _limbs = GetComponentsInChildren<CharacterLimbHitbox>(true);

            foreach (CharacterLimbHitbox limb in _limbs)
            {
                limb.LimbHitEvent += OnLimbHit;
            }
        }

        private void OnDisable()
        {
            foreach (CharacterLimbHitbox limb in _limbs)
            {
                limb.LimbHitEvent -= OnLimbHit;
            }
        }

        private void OnLimbHit(float damage, CharacterLimbHitbox sender)
        {
            _health = Mathf.Clamp(_health - damage, 0, float.MaxValue);
            DamageRecieved?.Invoke(damage, _health);
            //only mutilate when transitioning from alive to dead. NO WAR CRIMES!!!!
        }
    }
}