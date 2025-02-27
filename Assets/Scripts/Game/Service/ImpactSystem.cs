using Core.Engine;
using Game.Entities;
using Game.Impact;
using Game.Player.Controllers;
using Game.Player.Sound;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace Game.Service
{
    public class ImpactService : GameGlobalService
    {
        private ImpactSystem _system;
        private bool _initialized;

        internal override void Initialize()
        {
            if (_initialized) return;
            Debug.Log("Initializing Impacts");
            _system = new GameObject("ImpactSystem").AddComponent<ImpactSystem>();
            GameObject.DontDestroyOnLoad(_system);
            _system.Create();
            _initialized = true;
        }

        public ImpactSystem System => _system;
    }

    public enum ExplosionType
    {
        LIGHT,
        HEAVY,
        COMBUSTIBLE
    }

    public class ImpactSystem : MonoBehaviour
    {
        private ImpactsDictionary _dictionary;

        //create buffer references to explosions
        private RingBuffer<GameObject> ExplosionBuffer;

        private RingBuffer<GameObject> LightExplosionBuffer;

        private RingBuffer<GameObject> _limbMutilatedBuffer;
        private RingBuffer<GameObject> Tracers;
        private RingBuffer<GameObject> BloodDecalBuffer;
        private RingBuffer<GameObject> ConcreteBuffer;
        private RingBuffer<GameObject> MetalBuffer;
        private RingBuffer<GameObject> WoodBuffer;
        private RingBuffer<GameObject> _defaultBuffer;
        private RingBuffer<GameObject> _bloodBuffer;

        private void CreateTracers()
        {
            GameObject[] tracers = new GameObject[10];

            for (int i = 0; i < tracers.Length; i++)
            {
                tracers[i] = Instantiate(_dictionary.Tracer);
                tracers[i].SetActive(false);
                DontDestroyOnLoad(tracers[i]);
            }
            Tracers = new RingBuffer<GameObject>(tracers);
        }

        private void CreateExplosion()
        {
            ExplosionBuffer = GenerateBufferFromGameObject(_dictionary.GrenadeExplosion);
            LightExplosionBuffer = GenerateBufferFromGameObject(_dictionary.LightExplosion);
        }

        private RingBuffer<GameObject> GenerateBufferFromGameObject(ImpactObject prefab)
        {
            GameObject[] explosions = new GameObject[prefab.AmountPerScene];
            for (int i = 0; i < explosions.Length; i++)
            {
                explosions[i] = Instantiate(prefab.ImpactPrefab);
                explosions[i].SetActive(false);
                explosions[i].hideFlags = HideFlags.HideInHierarchy;
                explosions[i].GetComponent<ExplosionSoundEntity>().Set(prefab);
                DontDestroyOnLoad(explosions[i]);
            }
            return new RingBuffer<GameObject>(explosions);
        }

        public void ExplosionAtPosition(Vector3 position, ExplosionType type = ExplosionType.HEAVY)
        {
            GameObject explosion;

            switch (type)
            {
                case ExplosionType.LIGHT:
                    explosion = LightExplosionBuffer.GetNext();
                    break;

                case ExplosionType.HEAVY:
                    explosion = ExplosionBuffer.GetNext();
                    break;

                case ExplosionType.COMBUSTIBLE:
                    //todo: add combustible explosion (very fuegosity)
                    explosion = ExplosionBuffer.GetNext(); break;
                default:
                    //default explosion type
                    explosion = LightExplosionBuffer.GetNext(); break;
            }

            explosion.transform.position = position;
            explosion.transform.up = Vector3.up;
            explosion.SetActive(true);

            explosion.GetComponent<ParticleSystem>().Play();
            explosion.GetComponent<ExplosionSoundEntity>().Play();
            Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerStunController>().Shock(explosion.transform.position);
        }

        private void CreateImpacts()
        {
            _defaultBuffer = GenerateBufferForImpact(_dictionary.GenericHit);
            ConcreteBuffer = GenerateBufferForImpact(_dictionary.ConcreteHit);
            MetalBuffer = GenerateBufferForImpact(_dictionary.MetalHit);
            WoodBuffer = GenerateBufferForImpact(_dictionary.WoodHit);
            _bloodBuffer = GenerateBufferForImpact(_dictionary.BloodHit);
        }

        public RingBuffer<GameObject> GenerateBufferForImpact(ImpactObject impact)
        {
            GameObject[] bufferImpact = new GameObject[impact.AmountPerScene];

            for (int i = 0; i < bufferImpact.Length; i++)
            {
                bufferImpact[i] = Instantiate(impact.ImpactPrefab);
                bufferImpact[i].SetActive(false);
                bufferImpact[i].hideFlags = HideFlags.HideInHierarchy;
                DontDestroyOnLoad(bufferImpact[i]);
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
                    return _defaultBuffer;

                case SurfaceType.FLESH:
                    return _bloodBuffer;

                default:
                    return _defaultBuffer;
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
                DontDestroyOnLoad(bloodDecals[i]);
            }
            BloodDecalBuffer = new RingBuffer<GameObject>(bloodDecals);
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
            BulletAirSound(from, to);

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

        private void BulletAirSound(Vector3 from, Vector3 to)
        {
            if (!PlayerService.Active) return;

            Vector3 cameraPos = Bootstrap.Resolve<PlayerService>().PlayerCamera.transform.position;
            Vector3 toCameraDir = cameraPos - from;
            Vector3 bulletDir = to - from;
            // calculate hit before reaching oido del player
            if (Vector3.Dot(toCameraDir.normalized, (to - cameraPos).normalized) < 0) return;
            if (Mathf.Abs(Vector3.Dot(toCameraDir.normalized, bulletDir.normalized)) > .8f)
            {
                AudioToolService.PlayClipAtPoint(_dictionary.NearBullet.GetRandom(), cameraPos + bulletDir.normalized, 1f, AudioChannels.ENVIRONMENT, 5);
            }
        }

        internal void ImpactAtPosition(Vector3 point, Vector3 normal, Transform transform, SurfaceType type = SurfaceType.CONCRETE)
        {
            RingBuffer<GameObject> buffer = GetImpactsFromSurfaceType(type);
            GameObject impact = buffer.GetNext();

            if (impact == null || buffer == null)
            {
                // if the impact is null, means we just started the game or we destroyed it somehow during gameplay
                //so we regenerate the buffer
                //medio caca?  da hiccup seguro.
                RegenerateBufferOfType(type);
                buffer = GetImpactsFromSurfaceType(type);
                impact = buffer.GetNext();
            }

            impact.transform.position = point;
            impact.transform.forward = normal;
            impact.transform.SetParent(transform, true);
            //impact.transform.localScale = impact.transform.parent.localScale;

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

        private void RegenerateBufferOfType(SurfaceType type)
        {
            RingBuffer<GameObject> buffer = GetImpactsFromSurfaceType(type);

            for (int i = 0; i < buffer.Values.Length; i++)
            {
                Destroy(buffer.Values[i]);
            }

            buffer = GenerateBufferForImpact(_dictionary.GetImpactObjectFromType(type));

            switch (type)
            {
                case SurfaceType.ROCK:
                case SurfaceType.CERAMIC:
                case SurfaceType.BRICK:
                case SurfaceType.CONCRETE:
                    ConcreteBuffer = buffer;
                    break;

                case SurfaceType.WOOD:
                case SurfaceType.WOOD_HARD:
                case SurfaceType.CARTBOARD:
                case SurfaceType.PAPER:
                    WoodBuffer = buffer;
                    break;

                case SurfaceType.METAL_SOFT:
                case SurfaceType.METAL:
                case SurfaceType.METAL_HARD:
                    MetalBuffer = buffer;
                    break;

                case SurfaceType.GLASS:
                case SurfaceType.RUBBER:
                case SurfaceType.NYLON:
                    _defaultBuffer = buffer;
                    break;

                case SurfaceType.FLESH:
                    _bloodBuffer = buffer;
                    break;

                default:
                    _defaultBuffer = buffer;
                    break;
            }

            Debug.LogWarning("Some Impacts were null and the buffer was regenerated");
        }

        internal void BloodDecalAtPosition(Vector3 point, Vector3 normal, Transform parent = null)
        {
            //TODO: Crear shader solo para decals de impacto
            GameObject decal = BloodDecalBuffer.GetNext();

            if (decal == null)
            {
                for (int i = 0; i < BloodDecalBuffer.Values.Length; i++)
                {
                    Destroy(BloodDecalBuffer.Values[i]);
                }
                CreateBloodDecals();
                Debug.Log("Regenerated Blood Decals");
                decal = BloodDecalBuffer.GetNext();
            }

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