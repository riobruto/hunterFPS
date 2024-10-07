using Core.Engine;
using Game.Impact;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Service
{
    public class ImpactService : SceneService
    {
        private ImpactSystem _system;

        internal override void Initialize()
        {
            _system = new GameObject("ImpactSystem").AddComponent<ImpactSystem>();
        }

        public ImpactSystem System => _system;
    }

    public class ImpactSystem : MonoBehaviour
    {
        private ImpactsDictionary _dictionary;

        private RingBuffer<GameObject> ExplosionBuffer;
        private RingBuffer<GameObject> Tracers;
        private RingBuffer<GameObject> ImpactBuffer;
        private RingBuffer<GameObject> BloodBuffer;
        private RingBuffer<GameObject> LimbMutilatedBuffer;

        private void Start()
        {
            _dictionary = Resources.Load("ImpactDictionary") as ImpactsDictionary;
            CreateExplosion();
            CreateImpacts();
            CreateTracers();
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
            GameObject[] concreteHits = new GameObject[_dictionary.ConcreteHit.AmountPerScene];
            for (int i = 0; i < concreteHits.Length; i++)
            {
                concreteHits[i] = Instantiate(_dictionary.ConcreteHit.ImpactPrefab);
                concreteHits[i].SetActive(false);
            }
            ImpactBuffer = new RingBuffer<GameObject>(concreteHits);


            GameObject[] bloodHits = new GameObject[_dictionary.BloodHit.AmountPerScene];
            for (int i = 0; i < bloodHits.Length; i++)
            {
                bloodHits[i] = Instantiate(_dictionary.BloodHit.ImpactPrefab);
                bloodHits[i].SetActive(false);
            }

            BloodBuffer = new RingBuffer<GameObject>(bloodHits);
        }

        public void ImpactAtPosition(Vector3 position)
        {
        }

        public void ImpactAtPosition(Vector3 position, Vector3 direction)
        {
            GameObject explosion = ImpactBuffer.GetNext();
            explosion.transform.position = position;
            explosion.transform.forward = direction;
            explosion.SetActive(true);
            explosion.GetComponent<VisualEffect>().Play();

            AudioSource.PlayClipAtPoint(_dictionary.ConcreteHit.Sound.GetRandom(), position, 1);
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

            rendered.Clear();

            float trailTime = .25f;

            while (time < trailTime)
            {
                rendered.transform.position = Vector3.Lerp(from, to, time / trailTime);
                time += Time.deltaTime * Vector3.Distance(from, to) / trailTime;
                yield return null;
            }
            rendered.transform.position = to;
        }

        internal void ImpactAtPosition(Vector3 point, Vector3 normal, Transform transform)
        {
            GameObject explosion = ImpactBuffer.GetNext();
            explosion.transform.position = point;
            explosion.transform.forward = normal;
            explosion.transform.parent = transform;
            explosion.SetActive(true);
            explosion.GetComponent<VisualEffect>().Play();

            AudioSource.PlayClipAtPoint(_dictionary.ConcreteHit.Sound.GetRandom(), point, 1);
        }

        internal void BloodImpactAtPosition(Vector3 point, Vector3 normal, Transform transform)
        {
            GameObject blood = BloodBuffer.GetNext();
            blood.transform.position = point;
            blood.transform.forward = normal;
            blood.transform.parent = transform;
            blood.SetActive(true);
            blood.GetComponent<ParticleSystem>().Play();

            AudioSource.PlayClipAtPoint(_dictionary.BloodHit.Sound.GetRandom(), point, 1);
        }
    }
}