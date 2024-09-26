using Core.Engine;
using Game.Hit;
using Game.Service;
using System.Collections;
using UnityEngine;

namespace Game.Service
{
    public delegate void HitDelegate(HitWeaponEventPayload payload);
    public class HitScanService : SceneService
    {
        public HitDelegate HitEvent;
        public HitSystem HitSystem;
        internal override void Initialize()
        {
            HitSystem = new GameObject("HitSystem").AddComponent<HitSystem>();

        }
        public void Dispatch(HitWeaponEventPayload payload)
        {
            HitEvent?.Invoke(payload);
        }
        internal override void End()
        {
            GameObject.Destroy(HitSystem);
        }

    }
    public class HitWeaponEventPayload
    {
        // public WeaponEngine Sender { get; }
        public float Time { get; }
        public RaycastHit RaycastHit { get; }
        public Ray Ray { get; }
        public float Distance { get; }
        public float Damage { get; }


        public HitWeaponEventPayload(RaycastHit raycastHit, Ray ray, float damage)
        {

            Time = UnityEngine.Time.time;
            RaycastHit = raycastHit;
            Distance = Vector3.Distance(ray.GetPoint(0), raycastHit.point);
            Damage = damage;
            Ray = ray;
        }
    }
}
namespace Game.Hit
{
    
    public class HitSystem : MonoBehaviour
    {
        private void OnEnable()
        {
            Bootstrap.Resolve<HitScanService>().HitEvent += OnHit;
        }

        private void OnHit(HitWeaponEventPayload payload)
        {
            if (payload.RaycastHit.transform == null) return;

            foreach (IHittableFromWeapon hittable in payload.RaycastHit.transform.GetComponents<IHittableFromWeapon>())
            {
                hittable.OnHit(payload);
            }
        }
        private void OnDisable()
        {
            Bootstrap.Resolve<HitScanService>().HitEvent-= OnHit;
        }
    }
  
}