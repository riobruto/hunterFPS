using Game.Player.Controllers;
using System;
using System.Collections;
using UnityEngine;

namespace Game.Player.Attachment
{
    public class ScopeAttachment : MonoBehaviour
    {
        [SerializeField] float _smoothVelocity = .125f;

        private void Start()
        {
            transform.root.GetComponent<PlayerWeapons>().WeaponAimEvent += OnAim;
            _scale = transform.localScale;
            _scaleAim = _scale;
            _scaleAim.z = 0.001f;
            _targetScale = _scale;
        }

        private Vector3 _scale;
        private Vector3 _targetScale;
        private Vector3 _scaleAim;
        private Vector3 _scaleVelocity;

        private void OnAim(bool state)
        {
            _targetScale = state ? _scaleAim : _scale;
        }

        private void Update()
        {
            transform.localScale = Vector3.SmoothDamp(transform.localScale, _targetScale, ref _scaleVelocity, _smoothVelocity);
        }
    }
}