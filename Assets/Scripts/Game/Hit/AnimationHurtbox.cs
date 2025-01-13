using Game.Audio;
using Game.Player.Sound;
using Nomnom.RaycastVisualization;
using System.Collections.Generic;
using UnityEngine;
using static Game.Hit.AnimationHurtbox;

namespace Game.Hit
{
    public delegate void AnimationHurtboxDelegate(AnimationHurtbox hurtbox, IDamagableFromHurtbox[] contactedDamagables);

    public class AnimationHurtbox : MonoBehaviour
    {
        [SerializeField] private Vector3 _center;
        [SerializeField] private Vector3 _halfExtents;
        [SerializeField] private bool _checkCollisions;
        private int _remainingFrames;
        private List<IDamagableFromHurtbox> _hurtInScan;
        private float _damage;
        private LayerMask _layermask;

        public struct HurtboxContact
        {
            public IDamagableFromHurtbox Damagable;
            public Vector3 contactPosition;
            public Vector3 contactDirection;
        }

        public bool IsScanning { get => _checkCollisions && _remainingFrames > 1; }

        public event AnimationHurtboxDelegate HurtContactEvent;

        public void Initialize(LayerMask mask, float damage)
        {
            _layermask = mask;
            _damage = damage;
        }

        public void StartScan(int durationInframes)
        {
            _checkCollisions = true;
            _remainingFrames = durationInframes;
            _hurtInScan = new List<IDamagableFromHurtbox>();
        }

        private HurtboxContact[] CheckCollision()
        {
            Collider[] current = VisualPhysics.OverlapBox(transform.position + transform.TransformVector(_center), _halfExtents / 2, transform.rotation, _layermask, QueryTriggerInteraction.Ignore);

            List<HurtboxContact> result = new List<HurtboxContact>();

            foreach (Collider collider in current)
            {
                if (collider.gameObject.isStatic)
                {
                    continue;
                }
                if (collider.gameObject.layer == 3)
                {
                    continue;
                }

                if (!collider.TryGetComponent(out IDamagableFromHurtbox damagable)) continue;

                HurtboxContact contact = new();

                contact.Damagable = damagable;
                contact.contactPosition = collider.ClosestPoint(transform.position + transform.TransformVector(_center));
                contact.contactDirection = (contact.contactPosition - transform.position + transform.TransformVector(_center));

                result.Add(contact);
            }

            return result.ToArray();
        }

        private void Update()
        {
            if (!_checkCollisions) return;
            _remainingFrames--;

            if (_remainingFrames == 0)
            {
                bool hit = _hurtInScan.Count > 0;
                HurtContactEvent?.Invoke(this, _hurtInScan.ToArray());
                _checkCollisions = false;
            }

            foreach (HurtboxContact contact in CheckCollision())
            {
                if (contact.Damagable != null)
                {
                    if (_hurtInScan.Contains(contact.Damagable))
                    {
                        continue;
                    }

                    contact.Damagable.NotifyDamage(_damage, contact.contactPosition, contact.contactDirection);
                    _hurtInScan.Add(contact.Damagable);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _checkCollisions ? Color.red : Color.gray;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(_center, _halfExtents);
        }
    }
}