﻿using Core.Engine;
using Game.Hit;
using Game.Service;
using Life.Controllers;
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

        private AgentController _ownerAgent;

        private void Start()
        {
            GetComponent<Rigidbody>().isKinematic = true;
            _ownerAgent = transform.root.GetComponent<AgentController>();
        }

        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            LimbHitEvent?.Invoke(CalculateDamage(payload.Damage, payload.Distance), this);

            _ownerAgent.NotifyHurt(CalculateDamage(payload.Damage, payload.Distance));

            Bootstrap.Resolve<ImpactService>().System.BloodImpactAtPosition(payload.RaycastHit.point, payload.RaycastHit.normal, transform);


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
            GetComponent<Collider>().excludeLayers = 8;

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
            _ownerAgent.NotifyHurt(damage);
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