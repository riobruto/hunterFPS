using UnityEngine;

namespace Core.Weapon
{
    [CreateAssetMenu(menuName = "Game/Magazine Attachment", order = 815)]
    public class MagazineAttachmentSetting : AttachmentSettings
    {
        [Header("Magazine")]
        [SerializeField] private int _capacityOverride;

        public int CapacityOverride { get => _capacityOverride; }
    }
}