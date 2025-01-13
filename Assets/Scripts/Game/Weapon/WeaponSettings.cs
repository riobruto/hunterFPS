using Game.Inventory;
using Game.Player.Sound;
using System;
using UnityEngine;

namespace Core.Weapon
{
    [CreateAssetMenu(menuName = "Game/WeaponAsset", order = 815)]
    public class WeaponSettings : ScriptableObject
    {
        [Header("Weapon")]
        [Header("Sprites")]
        [SerializeField] private Sprite _weaponSprite;

        [SerializeField] private string _name;
        [SerializeField] private GameObject _weaponViewModel;
        [SerializeField] private GameObject _weaponWorldModel;
        [SerializeField, Range(0, 1000)] private float _damage;
        [SerializeField, Range(1, 1500)] private int _fireRatioPPM;

        [SerializeField] private int _bulletSpeed;
        [SerializeField] private WeaponFireModes _fireMode;

        [Header("")]
        [SerializeField] private WeaponSlotType _slotType;

        [Header("")]
        [Header("Spray and Recoil")]
        [SerializeField, Range(0, 30)] private float _sprayMultiplier = 1;

        [SerializeField] private AnimationCurve _sprayPatternAxisX;
        [SerializeField] private AnimationCurve _sprayPatternAxisY;

        [SerializeField] private Vector2 _recoilShake;
        [SerializeField] private Vector2 _recoilKick;
        [SerializeField] private float _recoilRecover;

        [Header("")]
        [SerializeField] private WeaponAmmo _ammo;

        [Header("")]
        [SerializeField] private WeaponSway _weaponSway;

        [Header("")]
        [SerializeField] private WeaponShot _weaponShot;

        [Header("")]
        [SerializeField] private WeaponReload _weaponReload;

        [Header("")]
        [SerializeField] private WeaponAim _weaponAim;

        [Header("")]
        [SerializeField] private WeaponAudio _weaponAudio;

        [Header("")]
        [SerializeField] private WeaponAttachments _weaponAttachments;

        public string Name => _name;
        public float Damage => _damage;
        public int FireRatioPPM => _fireRatioPPM;
        public float BulletSpeed => GetRandomBulletVelocity(_bulletSpeed);
        public float SprayMultiplier => _sprayMultiplier;
        public WeaponSlotType SlotType => _slotType;
        public WeaponAmmo Ammo => _ammo;
        public WeaponSway Sway => _weaponSway;
        public WeaponShot Shot => _weaponShot;
        public WeaponAudio Audio => _weaponAudio;

        public WeaponAim Aim => _weaponAim;
        public WeaponReload Reload => _weaponReload;
        public WeaponAttachments Attachments => _weaponAttachments;

        public GameObject ViewWeaponPrefab => _weaponViewModel;
        public GameObject WorldWeaponPrefab => _weaponWorldModel;
        public WeaponFireModes FireModes => _fireMode;
        public Vector2 RecoilShake => _recoilShake;
        public Vector2 RecoilKick => _recoilKick;
        public float RecoilRecoverSpeed => _recoilRecover;

        public Vector2 GetSprayPatternValue(float time) => new Vector2(_sprayPatternAxisX.Evaluate(time), _sprayPatternAxisY.Evaluate(time));

        public Sprite HUDSprite { get => _weaponSprite; }

        private int GetRandomBulletVelocity(int bulletSpeed)
        {
            return UnityEngine.Random.Range(-30, 30) + bulletSpeed;
        }

        [Serializable]
        public class WeaponAmmo
        {
            [SerializeField] private AmmunitionItem _ammunition;
            [SerializeField] private int _magSize;

            public int Size => _magSize;
            public AmmunitionItem Type => _ammunition;
        }

        [Serializable]
        public class WeaponAudio
        {
            [SerializeField] private AudioClipGroup _shootClips;

            [SerializeField] private float _hearingRange;

            public float HearingRange => _hearingRange;

            public AudioClipGroup ShootClips { get => _shootClips;  }
        }

        [Serializable]
        public class WeaponAim
        {
            [Header("Hip Offset")]
            [SerializeField] private Vector3 _positionOffset;

            [SerializeField] private Vector3 _rotationOffset;
            [SerializeField] private Vector3 _scaleOffset;

            [Header("Camera")]
            [SerializeField] private float _fieldOfView;

            [SerializeField] private float _velocity;

            [Header("Position And Rotation")]
            [SerializeField] private Vector3 _position;

            [SerializeField] private Vector3 _rotation;

            public float FieldOfView => _fieldOfView;
            public float Velocity => _velocity;
            public Vector3 Position => _position;
            public Vector3 RestPosition => _positionOffset;
            public Vector3 RestRotation => _rotationOffset;
            public Vector3 ScaleOffset => _scaleOffset;
            public Vector3 Rotation => _rotation;
        }

        [Serializable]
        public class WeaponShot
        {
            [SerializeField] private WeaponShotType _shotType;

            [Tooltip("Amount of rays in Shotgun and Meelee")]
            [SerializeField] private int _amount;

            [Tooltip("The shape of the rays, if weapon is meelee, the x axis represents the horizontal spray while y axis represent the offset of the rays vertically")]
            [SerializeField] private Vector2 _spead;

            public WeaponShotType Mode => _shotType;

            public int Amount => _amount;
            public Vector2 Spread => _spead;
        }

        [Serializable]
        public class WeaponReload
        {
            [SerializeField] private WeaponReloadMode _reloadMode;

            [SerializeField] private float _reloadEnterTime;
            [SerializeField] private float _reloadInsertTime;
            [SerializeField] private float _reloadExitTime;

            [Header("Only For Bolt Actions")]
            [SerializeField] private float _boltOpenTime;

            [SerializeField] private float _boltCloseTime;
            [SerializeField] private bool _grantFastReloadAtEmpty;
            public WeaponReloadMode Mode => _reloadMode;
            public float EnterTime => _reloadEnterTime;
            public float InsertTime => _reloadInsertTime;
            public float ExitTime => _reloadExitTime;
            public float BoltOpenTime => _boltOpenTime;
            public float BoltCloseTime => _boltCloseTime;
            public bool FastReloadOnEmpty => _grantFastReloadAtEmpty;
        }

        [Serializable]
        public class WeaponSway
        {
            [Header("Noise")]
            [SerializeField] private float _noiseMagnitude;

            [SerializeField] private float _noiseTime;

            [Header("Sway")]
            [SerializeField] private float _swayMagnitude;

            [SerializeField] private float _weaponWeight;
            public float NoiseMagnitude => _noiseMagnitude;
            public float NoiseTime => _noiseTime;
            public float Magnitude => _swayMagnitude;
            public float WeaponWeight => _weaponWeight / 10;
        }

        public static class WeaponRandom
        {
            private static float _lastIndexValue { get; set; }
            private static int _randomResult;

            public static int GetRandom(int max)
            {
                float result = _lastIndexValue;
                int attemp = 30;
                int _intToFloat;
                while (result == _lastIndexValue)
                {
                    attemp--;
                    _intToFloat = UnityEngine.Random.Range(0, max);
                    result = _intToFloat;
                    if (attemp == 0 || max < 2) break;
                }
                _lastIndexValue = result;

                _randomResult = (int)result;
                return _randomResult;
            }
        }

        [Serializable]
        public class WeaponAttachments
        {
            [SerializeField] private AttachmentSettings[] _allowedAttachmentForWeapon;
            public AttachmentSettings[] AllowedAttachments { get => _allowedAttachmentForWeapon; }
        }
    }
}