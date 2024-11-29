using Game.Player.Sound;
using System;
using System.Linq;
using UnityEngine;

namespace Game.Impact
{
    [CreateAssetMenu(fileName = "New ImpactDictionary", menuName = "Game/ImpactDictionary")]
    public class ImpactsDictionary : ScriptableObject
    {
        [SerializeField] private ImpactObject _grenadeExplosion;
        [SerializeField] private ImpactObject _concreteHit;
        [SerializeField] private ImpactObject _bloodHit;
        [SerializeField] private GameObject _tracer;
        [SerializeField] private float _tracerMinTime;
        [SerializeField] private float _tracerMaxTime;

        [SerializeField] private Texture _bulletHoles;
        [SerializeField] private BulletCoordinate[] _bulletCoordinates;

        [Header("Decal Sets")]
        [SerializeField] private DecalTextureSet _bloodDecalSet;

        public ImpactObject GrenadeExplosion => _grenadeExplosion;
        public ImpactObject ConcreteHit => _concreteHit;
        public ImpactObject BloodHit => _bloodHit;
        public GameObject Tracer { get => _tracer; }
        public DecalTextureSet BloodDecalSet => _bloodDecalSet;
        public float TracerMinSpeed { get => _tracerMinTime; }
        public float TracerMaxSpeed { get => _tracerMaxTime; }

        public Vector2Int GetBulletHoleFromType(SurfaceType type)
        {
            return _bulletCoordinates.First(x => x.SurfaceType == type).AtlasCoordinates;
        }
    }

    [Serializable]
    public class ImpactObject
    {
        [SerializeField] private int _amountPerScene;
        [SerializeField] private GameObject _impactPrefab;
        [SerializeField] private AudioClipCompendium _sound;
        [SerializeField] private bool _useDistanceBlend;
        [SerializeField] private AudioClipCompendium _soundFar;

        public int AmountPerScene { get => _amountPerScene; }
        public GameObject ImpactPrefab { get => _impactPrefab; }
        public AudioClipCompendium Sound { get => _sound; }
        public bool HasDistanceBlend { get => _useDistanceBlend; }
        public AudioClipCompendium SoundFar { get => _soundFar; }
    }
}