using Game.Hit;
using Game.Service;
using Life.Controllers;
using UnityEngine;

namespace Game.Entities
{
    public class Hitbox : MonoBehaviour, IHittableFromWeapon, IDamageableFromExplosive
    {
        private AgentController _ownerAgent;

        private void Start()
        {
            _ownerAgent = transform.root.GetComponent<AgentController>();
        }

        void IDamageableFromExplosive.NotifyDamage(float damage)
        {
            _ownerAgent.NotifyHurt(damage);
        }

        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            _ownerAgent.NotifyHurt(payload.Damage);
        }
    }
}