﻿using System.Collections;
using UnityEngine;

namespace Game.Player.Particle
{
    public class WeaponParticleAnimationEventListener : MonoBehaviour
    {
        [SerializeField] private GameObject _fire;

        public void FireParticle()
        {
            _fire.GetComponent<ParticleSystem>().Play();
        }
    }
}