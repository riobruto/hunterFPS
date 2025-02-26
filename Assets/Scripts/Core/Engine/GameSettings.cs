using Core.Configuration;
using UnityEngine;

namespace Core.Engine
{
    public class GameSettings : GameGlobalService
    {
        public RaycastConfiguration RaycastConfiguration { get; private set; }
        public PlayerConfiguration PlayerConfiguration { get; private set; }

        internal override void Initialize()
        {
            RaycastConfiguration = Resources.Load<RaycastConfiguration>("Config/" + nameof(RaycastConfiguration));
            PlayerConfiguration = Resources.Load<PlayerConfiguration>("Config/" + nameof(PlayerConfiguration));
        }
    }
}