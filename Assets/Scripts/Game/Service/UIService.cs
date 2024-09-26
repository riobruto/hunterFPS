using Core.Engine;
using UnityEngine.SceneManagement;

namespace Game.Service
{
    public class UIService : SceneService
    {
        internal override void Initialize()
        {
            SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
        }

        internal override void End()
        {
            SceneManager.UnloadSceneAsync(1);
        }
    }
}