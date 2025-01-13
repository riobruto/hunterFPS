using Core.Engine;
using Game.Audio;
using Game.Impact;
using Game.Player.Sound;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

namespace Game.Service
{
    public class ImpactService : SceneService
    {
        private ImpactSystem _system;
        private bool _initialized;

        internal override void Initialize()
        {
            if (_initialized) return;
            Debug.Log("Initializing Impacts");
            _system = new GameObject("ImpactSystem").AddComponent<ImpactSystem>();
            _system.Create();
            _initialized = true;
        }

        public ImpactSystem System => _system;
    }

    public class ImpactSystem : MonoBehaviour
    {
        private ImpactsDictionary _dictionary;

        private RingBuffer<GameObject> ExplosionBuffer;
        private RingBuffer<GameObject> LimbMutilatedBuffer;
        private RingBuffer<GameObject> Tracers;
        private RingBuffer<GameObject> BloodDecalBuffer;

        private RingBuffer<GameObject> ConcreteBuffer;
        private RingBuffer<GameObject> MetalBuffer;
        private RingBuffer<GameObject> WoodBuffer;
        private RingBuffer<GameObject> DefaultBuffer;
        private RingBuffer<GameObject> BloodBuffer;

        //TODO add support for multiple impacts!

        private void Start()
        {
        }

        private void CreateTracers()
        {
            GameObject[] tracers = new GameObject[10];

            for (int i = 0; i < tracers.Length; i++)
            {
                tracers[i] = Instantiate(_dictionary.Tracer);
                tracers[i].SetActive(false);
            }
            Tracers = new RingBuffer<GameObject>(tracers);
        }

        private void CreateExplosion()
        {
            GameObject[] explosions = new GameObject[_dictionary.GrenadeExplosion.AmountPerScene];
            for (int i = 0; i < explosions.Length; i++)
            {
                explosions[i] = Instantiate(_dictionary.GrenadeExplosion.ImpactPrefab);
                explosions[i].SetActive(false);
            }
            ExplosionBuffer = new RingBuffer<GameObject>(explosions);
        }

        public void ExplosionAtPosition(Vector3 position)
        {
            GameObject explosion = ExplosionBuffer.GetNext();
            explosion.transform.position = position;
            explosion.transform.up = Vector3.up;
            explosion.SetActive(true);
            explosion.GetComponent<ParticleSystem>().Play();

            Vector3 playerPos = Bootstrap.Resolve<PlayerService>().Player.transform.position;

            float distance = (position - playerPos).magnitude;
            AudioSource.PlayClipAtPoint(_dictionary.GrenadeExplosion.Sound.GetRandom(), playerPos + (position - playerPos).normalized, Mathf.InverseLerp(50, 0, distance));
            AudioSource.PlayClipAtPoint(_dictionary.GrenadeExplosion.SoundFar.GetRandom(), playerPos + (position - playerPos).normalized, Mathf.InverseLerp(0, 50, distance));
        }

        public void ExplosionAtPosition(Vector3 position, Vector3 direction)
        {
            GameObject explosion = ExplosionBuffer.GetNext();
            explosion.transform.position = position;
            explosion.transform.up = direction;

            explosion.SetActive(true);
            explosion.GetComponent<ParticleSystem>().Play();
            Vector3 playerPos = Bootstrap.Resolve<PlayerService>().Player.transform.position;

            float distance = (position - playerPos).magnitude;
            AudioSource.PlayClipAtPoint(_dictionary.GrenadeExplosion.Sound.GetRandom(), playerPos + (position - playerPos).normalized, Mathf.InverseLerp(50, 0, distance));
            AudioSource.PlayClipAtPoint(_dictionary.GrenadeExplosion.SoundFar.GetRandom(), playerPos + (position - playerPos).normalized, Mathf.InverseLerp(0, 50, distance));
        }

        private void CreateImpacts()
        {
            ConcreteBuffer = GenerateBufferForImpact(_dictionary.ConcreteHit);
            MetalBuffer = GenerateBufferForImpact(_dictionary.MetalHit);
            WoodBuffer = GenerateBufferForImpact(_dictionary.WoodHit);
            DefaultBuffer = GenerateBufferForImpact(_dictionary.GenericHit);
            BloodBuffer = GenerateBufferForImpact(_dictionary.BloodHit);
        }

        public RingBuffer<GameObject> GenerateBufferForImpact(ImpactObject impact)
        {
            GameObject[] bufferImpact = new GameObject[impact.AmountPerScene];
            for (int i = 0; i < bufferImpact.Length; i++)
            {
                bufferImpact[i] = Instantiate(impact.ImpactPrefab);
                bufferImpact[i].SetActive(false);
            }
            return new RingBuffer<GameObject>(bufferImpact);
        }

        public RingBuffer<GameObject> GetImpactsFromSurfaceType(SurfaceType type)
        {
            switch (type)
            {
                case SurfaceType.ROCK:
                case SurfaceType.CERAMIC:
                case SurfaceType.BRICK:
                case SurfaceType.CONCRETE:
                    return ConcreteBuffer;

                case SurfaceType.WOOD:
                case SurfaceType.WOOD_HARD:
                case SurfaceType.CARTBOARD:
                case SurfaceType.PAPER:
                    return WoodBuffer;

                case SurfaceType.METAL_SOFT:
                case SurfaceType.METAL:
                case SurfaceType.METAL_HARD:
                    return MetalBuffer;

                case SurfaceType.GLASS:
                case SurfaceType.RUBBER:
                case SurfaceType.NYLON:
                    return DefaultBuffer;

                case SurfaceType.FLESH:
                    return BloodBuffer;

                default:
                    return DefaultBuffer;
            }
        }

        private void CreateBloodDecals()
        {
            int amount = 32;
            GameObject[] bloodDecals = new GameObject[amount];
            for (int i = 0; i < amount; i++)
            {
                bloodDecals[i] = new GameObject($"bloodDecal.{i}");
                DecalProjector decal = bloodDecals[i].AddComponent<DecalProjector>();
                decal.material = new Material(_dictionary.BloodDecalSet.Material);
                decal.pivot = Vector3.zero;
                Vector3 size = Vector3.one * Random.Range(1.5f, .5f);
                size.z = 0.03f;
                decal.size = size;
                decal.gameObject.SetActive(false);
                decal.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
            BloodDecalBuffer = new RingBuffer<GameObject>(bloodDecals);
        }

        public void ImpactAtPosition(Vector3 position, Vector3 direction)
        {
            GameObject explosion = ConcreteBuffer.GetNext();
            explosion.transform.position = position;
            explosion.transform.forward = direction;
            explosion.SetActive(true);
            /*
             *
            explosion.GetComponent<VisualEffect>().Play();
            AudioSource.PlayClipAtPoint(_dictionary.ConcreteHit.Sound.GetRandom(), position, 1);
            */
        }

        internal void TraceAtPosition(Vector3 from, Vector3 to)
        {
            StartCoroutine(SpawnTrace(from, to));
        }

        private IEnumerator SpawnTrace(Vector3 from, Vector3 to)
        {
            float time = 0;
            GameObject trace = Tracers.GetNext();
            trace.SetActive(true);
            TrailRenderer rendered = trace.GetComponent<TrailRenderer>();
            rendered.transform.position = from;
            rendered.startWidth = Random.Range(0.05f, 0.15f);
            rendered.Clear();

            float trailTime = .25f;
            float speed = UnityEngine.Random.Range(_dictionary.TracerMinSpeed, _dictionary.TracerMaxSpeed);

            while (time < trailTime)
            {
                rendered.transform.position = Vector3.Lerp(from, to, time / trailTime);
                time += trailTime / Vector3.Distance(from, to) * speed;

                yield return null;
            }
            rendered.transform.position = to;
            yield break;
        }

        internal void ImpactAtPosition(Vector3 point, Vector3 normal, Transform transform, SurfaceType type = SurfaceType.CONCRETE)
        {
            GameObject impact = GetImpactsFromSurfaceType(type).GetNext();



            impact.transform.position = point;
            impact.transform.forward = normal;
            impact.transform.SetParent(transform, true);
            impact.transform.localScale = impact.transform.parent.localScale;

            impact.SetActive(true);
            if (impact.TryGetComponent(out VisualEffect fx)) fx.Play();
            if (impact.TryGetComponent(out ParticleSystem ps)) ps.Play();

            Vector2Int coord = _dictionary.GetBulletHoleFromType(type);
            DecalProjector proyector = impact.GetComponentInChildren<DecalProjector>();
            //TOO BAD!
            Material mat = new(proyector.material);
            mat.SetVector("_coordinates", new Vector4(coord.x, coord.y) + new Vector4(Random.Range(0, 2), Random.Range(0, 2), 0, 0));
            proyector.material = mat;

            AudioClipGroup c = _dictionary.GetImpactObjectFromType(type).Sound;
            if (c != null) AudioToolService.PlayClipAtPoint(c.GetRandom(), point, 1, AudioChannels.ENVIRONMENT, 5f);
        }

        internal void BloodDecalAtPosition(Vector3 point, Vector3 normal, Transform parent = null)
        {//TODO: Crear shader solo para decals de impacto
            GameObject decal = BloodDecalBuffer.GetNext();
            decal.GetComponent<DecalProjector>().material.SetTexture("_Color", _dictionary.BloodDecalSet.GetRandom());
            decal.transform.position = point;
            decal.transform.forward = normal;
            decal.transform.Rotate(0, 0, Random.Range(0, 360), Space.Self);
            decal.gameObject.SetActive(true);

            if (parent != null)
            {
                decal.transform.parent = parent;
            }
        }

        internal void Create()
        {
            _dictionary = Resources.Load("ImpactDictionary") as ImpactsDictionary;
            CreateExplosion();
            CreateImpacts();
            CreateTracers();
            CreateBloodDecals();
            Debug.Log("Impacts Done");
        }
    }
}