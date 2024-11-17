using Core.Engine;
using Nomnom.RaycastVisualization;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Hit
{
    public class AnimationHurtbox : MonoBehaviour
    {
        [SerializeField] private Vector3 _center;
        [SerializeField] private Vector3 _halfExtents;
        [SerializeField] private bool _checkCollisions;

        private int _remainingFrames;
        private List<IDamagableForHurtbox> _hurtInScan;

        public void StartScan(int durationInframes)
        {
            _checkCollisions = true;
            _remainingFrames = durationInframes;
            _hurtInScan = new List<IDamagableForHurtbox>();
        }

        private IDamagableForHurtbox[] CheckCollision()
        {
            Collider[] current = VisualPhysics.OverlapBox(transform.position + transform.TransformVector(_center), _halfExtents, transform.rotation, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.EnemyGunLayers);
            List<IDamagableForHurtbox> result = new List<IDamagableForHurtbox>();

            foreach (Collider collider in current)
            {
                collider.TryGetComponent(out IDamagableForHurtbox damagable);
                result.Add(damagable);
            }

            return result.ToArray();
        }

        private void Update()
        {
            if (!_checkCollisions) return;
            _remainingFrames--;

            if (_remainingFrames == 0) _checkCollisions = false;

            foreach (IDamagableForHurtbox hurtable in CheckCollision())
            {
                if (hurtable != null)
                {
                    if (_hurtInScan.Contains(hurtable))
                    {
                        continue;
                    }

                    hurtable.NotifyDamage(10);
                    _hurtInScan.Add(hurtable);
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