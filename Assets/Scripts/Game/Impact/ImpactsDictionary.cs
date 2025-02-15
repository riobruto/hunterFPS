using Game.Player.Sound;
using System;
using System.Linq;
using UnityEngine;

namespace Game.Impact
{
    [CreateAssetMenu(fileName = "New ImpactDictionary", menuName = "Game/ImpactDictionary")]
    public class ImpactsDictionary : ScriptableObject
    {
        [Header("Hits")]
        [SerializeField] private ImpactObject _grenadeExplosion;

        [SerializeField] private ImpactObject _concreteHit;
        [SerializeField] private ImpactObject _metalHit;
        [SerializeField] private ImpactObject _woodHit;
        [SerializeField] private ImpactObject _genericHit;
        [SerializeField] private ImpactObject _bloodHit;

        [Header("Tracers")]
        [SerializeField] private GameObject _tracer;
        [SerializeField] private float _tracerMinTime;
        [SerializeField] private float _tracerMaxTime;
        [SerializeField] private Texture _bulletHoles;
        [SerializeField] private BulletCoordinate[] _bulletCoordinates;
        [SerializeField] private AudioClipGroup _nearBullet;

        [Header("Decal Sets")]
        [SerializeField] private DecalTextureSet _bloodDecalSet;
        

        public ImpactObject GrenadeExplosion => _grenadeExplosion;
        public ImpactObject ConcreteHit => _concreteHit;
        public ImpactObject WoodHit => _woodHit;
        public ImpactObject MetalHit => _metalHit;
        public ImpactObject GenericHit => _genericHit;
        public ImpactObject BloodHit => _bloodHit;
        public GameObject Tracer { get => _tracer; }
        public DecalTextureSet BloodDecalSet => _bloodDecalSet;
        public float TracerMinSpeed { get => _tracerMinTime; }
        public float TracerMaxSpeed { get => _tracerMaxTime; }
        public AudioClipGroup NearBullet { get => _nearBullet; }

        public Vector2Int GetBulletHoleFromType(SurfaceType type)
        {
            return _bulletCoordinates.First(x => x.SurfaceType == type).AtlasCoordinates;
        }

        public ImpactObject GetImpactObjectFromType(SurfaceType type)
        {
            switch (type)
            {
                case SurfaceType.ROCK:
                case SurfaceType.CERAMIC:
                case SurfaceType.BRICK:
                case SurfaceType.CONCRETE:
                    return ConcreteHit;

                case SurfaceType.WOOD:
                case SurfaceType.WOOD_HARD:
                case SurfaceType.CARTBOARD:
                case SurfaceType.PAPER:
                    return WoodHit;

                case SurfaceType.METAL_SOFT:
                case SurfaceType.METAL:
                case SurfaceType.METAL_HARD:
                    return MetalHit;

                case SurfaceType.GLASS:
                case SurfaceType.RUBBER:
                case SurfaceType.NYLON:
                    return GenericHit;

                case SurfaceType.FLESH:
                    return BloodHit;

                default:
                    return GenericHit;
            }
        }
    }

    [Serializable]
    public class ImpactObject
    {
        [SerializeField] private int _amountPerScene;
        [SerializeField] private GameObject _impactPrefab;
        [SerializeField] private AudioClipGroup _sound;
        [SerializeField] private bool _useDistanceBlend;
        [SerializeField] private AudioClipGroup _soundFar;

        public int AmountPerScene { get => _amountPerScene; }
        public GameObject ImpactPrefab { get => _impactPrefab; }
        public AudioClipGroup Sound { get => _sound; }
        public bool HasDistanceBlend { get => _useDistanceBlend; }
        public AudioClipGroup SoundFar { get => _soundFar; }
    }
}