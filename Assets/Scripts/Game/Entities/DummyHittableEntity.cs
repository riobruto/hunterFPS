using Game.Hit;
using Game.Service;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities
{
    public class DummyHit
    {
        public Vector3 HitPos;
        public Vector3 HitDirection;
        public float Time;

        public DummyHit(Vector3 hitPos, Vector3 hitDirection, float time)
        {
            HitPos = hitPos;
            HitDirection = hitDirection;
            Time = time;
        }
    }

    public class DummyHittableEntity : MonoBehaviour, IHittableFromWeapon, IDamageableFromExplosive
    {
        private List<DummyHit> _hitPoints = new List<DummyHit>();
        [SerializeField] private MeshRenderer _renderer;
        private float _time;

        public void NotifyDamage(float damage)
        {
            _time = 1;
            _hitPoints.Add(new DummyHit(transform.position, transform.forward, Time.time));
        }

        public void Hit(HitWeaponEventPayload payload)
        {
            _time = 1;
            _hitPoints.Add(new DummyHit(payload.RaycastHit.point, payload.RaycastHit.normal, Time.time));
        }

        private void LateUpdate()
        {
            _time = Mathf.Clamp(_time - Time.deltaTime, 0, 1);

            for (int i = 0; i < _hitPoints.Count; i++)
            {
                if (Mathf.Abs(Time.time - _hitPoints[i].Time) > 5)
                {
                    _hitPoints.Remove(_hitPoints[i]);
                }
            }

            _renderer.material.color = Color.Lerp(Color.white, Color.red, _time);
        }

        private void OnDrawGizmos()
        {
            foreach (DummyHit hit in _hitPoints)
            {
                Gizmos.color = Color.red;

                Gizmos.DrawSphere(hit.HitPos, ((hit.Time + 5) - Time.time) * 0.03f);
            }
        }
    }
}