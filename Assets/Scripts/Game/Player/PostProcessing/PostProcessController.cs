using Game.Player.Controllers;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Player.PostProcessing
{
    public class PostProcessController : MonoBehaviour
    {
        [SerializeField] private Volume _dofVolume;
        [SerializeField] private Volume _hurtVolume;
        [SerializeField] private Volume _tiredVolume;
        private float _target;

        private PlayerHealth _health;
        private PlayerRigidbodyMovement _movement;

        // Use this for initialization
        private void Start()
        {
            transform.root.GetComponent<PlayerWeapons>().WeaponAimEvent += OnAim;
            _health = transform.root.GetComponent<PlayerHealth>();
            _movement = transform.root.GetComponent<PlayerRigidbodyMovement>();
        }

        private void OnAim(bool state)
        {
            _target = state ? 1 : 0;
        }

        private void LateUpdate()
        {
            _dofVolume.weight = _target;
            _hurtVolume.weight = Mathf.InverseLerp(50, 0, _health.CurrentHealth);
            _tiredVolume.weight = Mathf.InverseLerp(25, 0, _movement.Stamina);
        }
    }
}