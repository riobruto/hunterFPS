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

        void IDamageableFromExplosive.NotifyDamage(float damage, Vector3 position)
        {
            _ownerAgent.Damage(damage);
        }

        void IHittableFromWeapon.Hit(HitWeaponEventPayload payload)
        {
            _ownerAgent.Damage(payload.Damage);
        }
    }
}