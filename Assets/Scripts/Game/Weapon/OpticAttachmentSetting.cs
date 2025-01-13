using UnityEngine;

namespace Core.Weapon
{
    [CreateAssetMenu(menuName = "Game/Optic Attachment", order = 815)]
    public class OpticAttachmentSetting : AttachmentSettings
    {
        [Header("Sight")]
        [SerializeField] private Vector3 _aimPositionOverride;

        [SerializeField] private Vector3 _aimRotationOverride;
        [SerializeField] private float _fovOverride;

        public Vector3 AimPositionOverride { get => _aimPositionOverride; }
        public Vector3 AimRotationOverride { get => _aimRotationOverride; }
        public float FovOverride { get => _fovOverride; }
    }
}