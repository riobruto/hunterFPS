﻿using Core.Engine;
using Game.Audio;
using Game.Hit;
using Game.Player.Sound;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public delegate void BreakDelegate(BreakableEntity entity);

    public class BreakableEntity : MonoBehaviour, IHittableFromWeapon, IDamageableFromExplosive
    {
        [SerializeField] private Transform[] _brokenMeshes;
        [SerializeField] private Transform _healthyMesh;

        [Tooltip("This allows the entity to create the components of the broken pieces in runtime")]
        [SerializeField] private bool _generateGibs;

        [SerializeField] private float health = 1;
        private bool _broken;
        private Rigidbody _rigidbody;
        [SerializeField] private int _piecesDuration = 60;
        [SerializeField] private AudioClipCompendium _breakAudio;

        public event BreakDelegate BreakEvent;

        public int PiecesDuration { get => _piecesDuration; set => _piecesDuration = value; }
        public bool Broken { get => _broken; }

        void IDamageableFromExplosive.NotifyDamage(float damage)
        {
            Hurt(damage);
        }

        private void Hurt(float damage)
        {
            health -= damage;

            if (health <= 0 && !_broken)
            {
                Break();
            }
        }

        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            Bootstrap.Resolve<ImpactService>().System.ImpactAtPosition(payload.RaycastHit.point, payload.RaycastHit.normal);

            Hurt(payload.Damage);
        }

        private void Break()
        {
            _broken = true;
            _healthyMesh.gameObject.SetActive(false);
            GetComponent<MeshCollider>().enabled = false;
            AudioToolService.PlayClipAtPoint(_breakAudio.GetRandom(), transform.position, 1, AudioChannels.ENVIRONMENT);
            foreach (Transform t in _brokenMeshes)
            {
                t.gameObject.SetActive(true);
                var collider = t.gameObject.AddComponent<MeshCollider>();
                collider.convex = true;
                Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();
                rb.mass = _rigidbody.mass / _brokenMeshes.Length;
                rb.AddExplosionForce(5, transform.position, 1);
            }
            BreakEvent?.Invoke(this);
            Destroy(gameObject, _piecesDuration);
        }

        private void Start()
        {
            foreach (Transform t in _brokenMeshes)
            {
                t.gameObject.SetActive(false);
            }
            _rigidbody = GetComponent<Rigidbody>();
        }
    }
}