﻿using Core.Engine;
using Game.Audio;
using Game.Hit;
using Game.Player.Sound;
using Game.Service;
using Life.Controllers;
using Nomnom.RaycastVisualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Entities
{
    public delegate void LimbHitDelegate(LimboxHit payload);

    public struct LimboxHit
    {
        public Vector3 Position;
        public Vector3 Direction;
        public LimbHitbox Hitbox;
        public float Damage;

        public LimboxHit(Vector3 position, Vector3 direction, LimbHitbox hitbox, float damage)
        {
            Position = position;
            Direction = direction;
            Hitbox = hitbox;
            Damage = damage;
        }
    }

    public class LimbHitbox : MonoBehaviour, IHittableFromWeapon, IDamageableFromExplosive, IDamagableFromHurtbox
    {
        [SerializeField] private LimbType _type = LimbType.UNDEFINED;
        public LimbType Type { get => _type; }

        public event LimbHitDelegate LimbHitEvent;

        public bool IsMutilated;
        private AgentController _ownerAgent;

        [SerializeField] private AudioClipGroup _bodyFall;
        private Rigidbody _rb;

        public void NotifyAgent(LimboxHit payload) => LimbHitEvent?.Invoke(payload);
        public void SetOwner(AgentController owner) => _ownerAgent = owner;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _ownerAgent = transform.root.GetComponent<AgentController>();
        }

        void IDamageableFromExplosive.NotifyDamage(float damage, Vector3 position, Vector3 explosionDirection)
        {
            NotifyAgent(new LimboxHit(
                position,
                explosionDirection,
                this,
                damage
                ));
        }

        void IHittableFromWeapon.Hit(HitWeaponEventPayload payload)
        {
            NotifyAgent(new LimboxHit(
                payload.RaycastHit.point,
                payload.RaycastHit.normal,
                this,
                payload.Damage
                ));
            ManageVisual(payload);
        }

        void IDamagableFromHurtbox.NotifyDamage(float damage, Vector3 position, Vector3 direction)
        {
            _ownerAgent.Kick(position, direction, damage);
            Impulse(direction);
        }

        private void ManageVisual(HitWeaponEventPayload payload)
        {
            Bootstrap.Resolve<ImpactService>().System.ImpactAtPosition(payload.RaycastHit.point, payload.RaycastHit.normal, transform, Impact.SurfaceType.FLESH);

            if (VisualPhysics.Raycast(payload.RaycastHit.point, payload.RaycastHit.point - payload.Ray.origin, out RaycastHit hit, 4f, 1 << 0))
            {
                Bootstrap.Resolve<ImpactService>().System.BloodDecalAtPosition(hit.point, hit.normal, hit.collider.gameObject.transform);
            }
            else if (VisualPhysics.Raycast(payload.RaycastHit.point, Vector3.down, out RaycastHit hitground, 2f, 1 << 0))
            {
                Bootstrap.Resolve<ImpactService>().System.BloodDecalAtPosition(hitground.point, hitground.normal, hitground.collider.gameObject.transform);
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
            gameObject.layer = 17;
            _rb.isKinematic = false;
            _rb.velocity = (_ownerAgent.NavMeshAgent.velocity);
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

        internal void RunOver(Vector3 velocity, float damage)
        {
            _ownerAgent.KillAndPush(velocity);
        }

        internal void Impulse(Vector3 velocity)
        {
            _rb.AddForce(velocity + Vector3.up, ForceMode.Impulse);
        }

        private float _soundCooldown;

        private void OnCollisionEnter(Collision collision)
        {
            if (!_ownerAgent.IsDead) return;
            if (_type != LimbType.TORSO) return;
            if (collision.relativeVelocity.sqrMagnitude < 5) return;
            if (Time.time - _soundCooldown < 1) return;
            AudioToolService.PlayClipAtPoint(_bodyFall.GetRandom(), transform.position, .5f, AudioChannels.ENVIRONMENT, 15);
            _soundCooldown = Time.time;
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