using Game.Hit;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public delegate void LimbHitDelegate(float damage, CharacterLimbHitbox sender);

    public class CharacterLimbHitbox : MonoBehaviour, IHittableFromWeapon, IDamageableFromExplosive
    {
        [SerializeField] private LimbType _type = LimbType.UNDEFINED;

        public LimbType Type { get => _type; }

        public event LimbHitDelegate LimbHitEvent;

        public bool IsMutilated;

        private void Start()
        {
            GetComponent<Rigidbody>().isKinematic = true;
        }

        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            LimbHitEvent?.Invoke(CalculateDamage(payload.Damage, payload.Distance), this);

            if (gameObject.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForceAtPosition(-payload.RaycastHit.normal.normalized, payload.RaycastHit.point, ForceMode.VelocityChange);
            }
        }

        public void Mutilate()
        {
            //TODO: Gore Logic
            transform.localScale = Vector3.one * 0.001f;

            if (gameObject.TryGetComponent(out CharacterJoint joint))
            {
                joint.breakForce = 0;
            }

            if (gameObject.TryGetComponent(out Collider collider))
            {
                collider.enabled = false;
            }

            if (gameObject.TryGetComponent(out Rigidbody rrb))
            {
                rrb.isKinematic = true;
            }

            IsMutilated = true;
        }

        public void Ragdoll()
        {
            if (IsMutilated) return;

            if (gameObject.TryGetComponent(out Rigidbody rrb))
            {
                rrb.isKinematic = false;
            }
        }

        private float CalculateDamage(float damage, float distance)
        {
            switch (_type)
            {
                case LimbType.UNDEFINED:
                    return damage / distance;

                case LimbType.HEAD:
                    return 100;

                case LimbType.TORSO:

                case LimbType.ARM:

                case LimbType.LEG:

                case LimbType: return damage;
            }
        }

        void IDamageableFromExplosive.NotifyDamage(float damage)
        {
            LimbHitEvent?.Invoke(damage, this);

        }
    }

    public enum LimbType
    {
        UNDEFINED,
        HEAD,
        TORSO,
        ARM,
        LEG,
    }
}