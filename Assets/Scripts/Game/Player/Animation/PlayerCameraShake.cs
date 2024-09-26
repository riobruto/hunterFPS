using Core.Engine;
using Game.Player.Controllers;
using Game.Service;
using UnityEngine;

namespace Game.Player.Animation
{
    public class PlayerCameraShake : MonoBehaviour
    {
        [SerializeField] private float _currentTime = 0;
        [SerializeField] private float _randomIntensity = 5f;
        [SerializeField] private float _recoverSpeed = 1f;

        private Vector3 _shakeVector;
        [SerializeField] private AnimationCurve _shakeCurve;

        private void Start()
        {
            Bootstrap.Resolve<PlayerService>().Player.gameObject.GetComponent<PlayerHealth>().HurtEvent += OnHurt;
        }

        private void OnHurt()
        {
            Shake();
        }

        public void Shake()
        {
            _shakeVector = Random.insideUnitSphere - (Vector3.one * .5f) * 2  * _randomIntensity;
            _currentTime = 1;
        }

        private void LateUpdate()
        {
            _currentTime = Mathf.Clamp01(_currentTime - (Time.deltaTime * _recoverSpeed));
            Vector3 final = Vector3.Lerp(Vector3.zero, _shakeVector, _shakeCurve.Evaluate(_currentTime));
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(final), Time.deltaTime * 15f);
        }
    }
}