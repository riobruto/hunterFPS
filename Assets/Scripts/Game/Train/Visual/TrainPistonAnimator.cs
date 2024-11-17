using Game.Train;
using UnityEngine;

namespace Train.Visual
{
    public class TrainPistonAnimator : MonoBehaviour
    {
        [SerializeField] private TrainEngine _engine;
        [SerializeField] private Animator _animator;
        [SerializeField] private AudioClip _pistonClip;

        AudioSource _source;
        private void Start()
        {
            _source = gameObject.AddComponent<AudioSource>();

            _source.loop = false;
            _source.spatialBlend = 1;
            _source.playOnAwake = false;
            _source.dopplerLevel = .5f;

        }
        // Update is called once per frame
        private void LateUpdate()
        {
            float speed = _engine.Speed;
            if (Vector3.Dot(_engine.Rigidbody.velocity, transform.forward) < 0) speed *= -1;
            _animator.SetFloat("SPEED", speed);
        }

        public void Piston()
        {
            _source.pitch = Random.Range(0.95f, 1.05f);
            _source.PlayOneShot(_pistonClip, _engine.EffectiveLoad);
        }
    }
}