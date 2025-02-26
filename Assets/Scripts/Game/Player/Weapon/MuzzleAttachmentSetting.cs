using Game.Player.Sound;
using UnityEngine;

namespace Core.Weapon
{
    [CreateAssetMenu(menuName = "Game/Muzzle Attachment", order = 815)]
    public class MuzzleAttachmentSetting : AttachmentSettings
    {
        [Header("Muzzle")]
        [SerializeField] private AudioClipGroup _soundOverride;

        [SerializeField] private float _soundRangeModifier;
        [SerializeField] private bool _muzzleOverride;
        public AudioClipGroup SoundOverride { get => _soundOverride; }
        public float SoundRangeModifier { get => _soundRangeModifier; }
        public bool UseMuzzle { get => _muzzleOverride; }
    }
}