﻿using Game.Player.Controllers;
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
            transform.root.gameObject.GetComponent<PlayerHealth>().HurtEvent += OnHurt;
        }

        private void OnHurt(HurtPayload arg0)
        {
            Shake();
        }

        public void Shake()
        {
            _shakeVector = Random.insideUnitSphere * _randomIntensity * 2f;
            _currentTime = 1;
        }

        public void Shake(Vector3 direction)
        {
            _shakeVector = direction * _randomIntensity;
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