using UnityEngine;

namespace Core.Weapon
{
    [CreateAssetMenu(menuName = "Game/Action Attachment", order = 815)]
    public class ActionAttachmentSetting : AttachmentSettings
    {
        [Header("Action")]
        [SerializeField] private float _damageOverride;

        [SerializeField] private int _fireRateOverride;

        public float DamageOverride { get => _damageOverride; }
        public int FireRatePPMOverride { get => _fireRateOverride; }
    }
}