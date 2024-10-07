using Core.Engine;
using Game.Hit;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public class ImpactSurface : MonoBehaviour, IHittableFromWeapon
    {
        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            Bootstrap.Resolve<ImpactService>().System.ImpactAtPosition(payload.RaycastHit.point, payload.RaycastHit.normal, transform);
           

            if (gameObject.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForceAtPosition(-payload.RaycastHit.normal.normalized, payload.RaycastHit.point, ForceMode.VelocityChange);
            }
        }
    }
}