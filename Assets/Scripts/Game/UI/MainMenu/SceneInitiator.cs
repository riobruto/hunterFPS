using Core.Engine;
using Game.Service;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.UI.MainMenu
{
    public delegate void SceneLoaded();

    public class SceneInitiator : GameGlobalService
    {
        private GameObject _loadScreen;
        public MainLoadSceneScreen LoadScreenComponent { get; private set; }
        private MainMenuElements _elements;

        public static event SceneLoaded OnSceneLoaded;
        public static event SceneLoaded OnSceneReloaded;

        internal override void Initialize()
        {
            LoadLoadingScreen();
            LoadMainMenu();
        }

        private void LoadLoadingScreen()
        {
            Addressables.InstantiateAsync("Assets/Prefabs/UI/LoadingScreen.prefab").Completed += (x) =>
            {
                _loadScreen = x.Result;
                LoadScreenComponent = _loadScreen.GetComponent<MainLoadSceneScreen>();
                GameObject.DontDestroyOnLoad(_loadScreen);
            };
        }

        private void LoadMainMenu()
        {
            Addressables.InstantiateAsync("Assets/Prefabs/UI/MainMenu.prefab").Completed += (x) =>
            {
                _elements = x.Result.GetComponent<MainMenuElements>();
            };
        }

        public void LoadScene(string scene)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scene);
            //set loadbar
            LoadScreenComponent.Load(operation);

            operation.completed += (x) =>
            {
                OnSceneLoaded?.Invoke();
                UIService.CreateMessage("Scene Loaded");
            };
        }

        public void ReloadCurrentScene()
        {
           
            AsyncOperation operation = SceneManager.LoadSceneAsync(SceneManager.GetSceneAt(0).name);
            //set loadbar
            LoadScreenComponent.Load(operation);


            operation.completed += (x) =>
            {
                OnSceneReloaded?.Invoke();
                UIService.CreateMessage("Scene Reloaded");
            };
        }
    }
}