using Core.Engine;
using Game.Impact;
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
        private RingBuffer<GameObject> ImpactBuffer;
        private RingBuffer<GameObject> LimbMutilatedBuffer;

        private void Start()
        {
            _dictionary = Resources.Load("ImpactDictionary") as ImpactsDictionary;
            CreateExplosion();
            CreateImpacts();
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
    }
}