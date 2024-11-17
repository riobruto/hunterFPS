using Game.Player.Sound;
using System;
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

        [Header("Decal Sets")]
        [SerializeField] private DecalTextureSet _bloodDecalSet;

        public ImpactObject GrenadeExplosion => _grenadeExplosion;
        public ImpactObject ConcreteHit => _concreteHit;
        public ImpactObject BloodHit => _bloodHit;
        public GameObject Tracer { get => _tracer; }

        public DecalTextureSet BloodDecalSet => _bloodDecalSet;

        public float TracerMinSpeed { get => _tracerMinTime; }
        public float TracerMaxSpeed { get => _tracerMaxTime; }
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