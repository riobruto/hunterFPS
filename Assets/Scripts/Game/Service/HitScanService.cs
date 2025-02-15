using Core.Engine;
using Game.Hit;
using UnityEngine;

namespace Game.Service
{
    public class HitScanService : SceneService
    {
        public HitDelegate HitEvent;
        public HitSystem HitSystem;

        internal override void Initialize()
        {
            HitSystem = new GameObject("HitSystem").AddComponent<HitSystem>();
            GameObject.DontDestroyOnLoad(HitSystem);

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
}
