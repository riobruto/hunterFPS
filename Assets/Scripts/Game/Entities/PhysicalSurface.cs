using Core.Engine;
using Game.Hit;
using Game.Impact;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public class PhysicalSurface : MonoBehaviour, IHittableFromWeapon
    {

        [SerializeField] SurfaceType _type;

        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            Bootstrap.Resolve<ImpactService>().System.ImpactAtPosition(payload.RaycastHit.point, payload.RaycastHit.normal, transform, _type);           

            if (gameObject.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForceAtPosition(-payload.RaycastHit.normal.normalized, payload.RaycastHit.point, ForceMode.Impulse);
            }
        }
    }
}