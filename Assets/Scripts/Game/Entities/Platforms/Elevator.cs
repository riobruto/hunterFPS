using System.Collections;
using UnityEngine;

namespace Game.Entities.Platforms
{
    public class Elevator : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private float heightOffset;
        [SerializeField] private float _duration;
        [SerializeField] private Animator _animator;

        private bool _finished;
        private bool _active;

        public void Open() => _animator.SetTrigger("OPEN");

        public void Close() => _animator.SetTrigger("CLOSE");

        public void StartSequence()
        {
            if (_active) return;
            if (_finished) return;

            _active = true;
            StartCoroutine(IStartSequence());
        }

        private IEnumerator IStartSequence()
        {
            _animator.SetTrigger("CLOSE");
            yield return new WaitForSeconds(3);
            StartCoroutine(Move());
            yield return new WaitUntil(() => _finished);
            yield return new WaitForSeconds(3);
            _animator.SetTrigger("OPEN");
        }

        private IEnumerator Move()
        {
            float time = 0;
            Vector3 from = transform.position;
            Vector3 to = transform.position + new Vector3(0, heightOffset, 0);

            while (time < _duration)
            {
                time += Time.deltaTime;
                _rigidbody.position = Vector3.Lerp(from, to, time / _duration);
                yield return null;
            }
            _rigidbody.position = to;
            _finished = true;
        }
    }
}