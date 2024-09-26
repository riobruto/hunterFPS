using Game.Player.Controllers;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Player.PostProcessing
{
    public class DOFController : MonoBehaviour
    {
        [SerializeField] private Volume _dofVolume;
        private float _target;

        // Use this for initialization
        private void Start()
        {
            transform.root.GetComponent<PlayerWeapons>().WeaponAimEvent += OnAim;
        }

        private void OnAim(bool state)
        {
            _target = state ? 1 : 0;
        }

        private void LateUpdate()
        {
            _dofVolume.weight = _target;
        }
    }
}