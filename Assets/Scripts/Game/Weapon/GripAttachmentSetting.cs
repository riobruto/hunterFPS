using UnityEngine;

namespace Core.Weapon
{
    [CreateAssetMenu(menuName = "Game/Grip Attachment", order = 815)]
    public class GripAttachmentSetting : AttachmentSettings
    {
        [Header("Action")]
        [SerializeField] private float _recoilAmountOverride;

        [SerializeField] private float _swayOverride;
        public float Recoil { get => _recoilAmountOverride; }
        public float Sway { get => _swayOverride; }
    }
}