using Core.Engine;
using Game.Hit;
using Game.Player.Weapon;
using Game.Service;
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
        public float Time { get; }
        public RaycastHit RaycastHit { get; }
        public Ray Ray { get; }
        public float Distance { get; }
        public float Damage { get; }

        public bool IsSenderPlayer { get; }

        public HitWeaponEventPayload(RaycastHit raycastHit, Ray ray, float damage, bool isSenderPlayer)
        {
            Time = UnityEngine.Time.time;
            RaycastHit = raycastHit;
            Distance = Vector3.Distance(ray.GetPoint(0), raycastHit.point);
            Damage = damage;
            Ray = ray;
            IsSenderPlayer = isSenderPlayer;
        }
    }
}

namespace Game.Hit
{
    public class HitSystem : MonoBehaviour
    {
        private void OnEnable()
        {
            Bootstrap.Resolve<HitScanService>().HitEvent += OnHitNotified;
        }

        private void OnHitNotified(HitWeaponEventPayload payload)
        {
            if (payload.RaycastHit.transform == null) return;

            //TODO: decidir si queremos emitir el hit solo al elemento colisionado, ergo, los hittables que sean componenets de ese transform solamente, o que pueda buscar verticalmente.
            //IMPÓRTANTE: seteando q busque en el collider previsionalmente, puede introducir ghosting de hits.
            //13-1
            foreach (IHittableFromWeapon hittable in payload.RaycastHit.collider.transform.GetComponents<IHittableFromWeapon>())
            {
                hittable.Hit(payload);
            }
        }

        private void OnDisable()
        {
            Bootstrap.Resolve<HitScanService>().HitEvent -= OnHitNotified;
        }
    }
}