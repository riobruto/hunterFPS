using Core.Engine;
using Game.Audio;
using Game.Hit;
using Game.Player.Sound;
using Game.Service;
using Life.Controllers;
using Nomnom.RaycastVisualization;
using UnityEngine;

namespace Game.Entities
{
    public delegate void LimbHitDelegate(float damage, LimbHitbox sender);

    public class LimbHitbox : MonoBehaviour, IHittableFromWeapon, IDamageableFromExplosive
    {
        [SerializeField] private LimbType _type = LimbType.UNDEFINED;
        public LimbType Type { get => _type; }

        public event LimbHitDelegate LimbHitEvent;

        public bool IsMutilated;
        private AgentController _ownerAgent;
        private Collider _collider;
        [SerializeField] private AudioClipCompendium _bodyFall;

        private void Start()
        {
            GetComponent<Rigidbody>().isKinematic = true;
            _collider = GetComponent<Collider>();
            _ownerAgent = transform.root.GetComponent<AgentController>();
        }

        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            LimbHitEvent?.Invoke(CalculateDamage(payload.Damage, payload.Distance), this);

            _ownerAgent.NotifyHurt(CalculateDamage(payload.Damage, payload.Distance));

            ManageVisual(payload);

            if (gameObject.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForceAtPosition(-payload.RaycastHit.normal.normalized, payload.RaycastHit.point, ForceMode.VelocityChange);
            }
        }

        private void ManageVisual(HitWeaponEventPayload payload)
        {
            Bootstrap.Resolve<ImpactService>().System.BloodImpactAtPosition(payload.RaycastHit.point, payload.RaycastHit.normal, transform);

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

            if (gameObject.TryGetComponent(out Rigidbody rrb))
            {
                rrb.isKinematic = false;
                //enemies
                rrb.excludeLayers <<= 8;
                //trains
                rrb.excludeLayers <<= 10;
                //player
                rrb.excludeLayers <<= 3;
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
            _ownerAgent.NotifyHurt(damage);
        }

        internal void RunOver(Vector3 velocity, float damage)
        {
            _ownerAgent.RunOver(velocity);

            Debug.Log("Atropellado putooo");
        }

        internal void Impulse(Vector3 velocity)
        {
            if (gameObject.TryGetComponent(out Rigidbody rrb))
            {
                rrb.AddForce(velocity + Vector3.up, ForceMode.Impulse);
            }
        }

        private float _soundCooldown;

        private void OnCollisionEnter(Collision collision)
        {
            if (_type != LimbType.TORSO) return;
            if (collision.relativeVelocity.sqrMagnitude < 5) return;
            if (Time.time - _soundCooldown < 1) return;
            AudioToolService.PlayClipAtPoint(_bodyFall.GetRandom(), transform.position, 1, AudioChannels.ENVIRONMENT, 15);
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