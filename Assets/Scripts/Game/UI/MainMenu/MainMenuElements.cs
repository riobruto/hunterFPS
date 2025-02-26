using Core.Engine;
using Game.Service;
using System;
using System.Collections;
using UnityEngine;

namespace 
    Game.UI.MainMenu
{
    public class MainMenuElements : MonoBehaviour
    {
        private string _currentSceneSelected;

        [SerializeField] private AudioClip _playGameClip;
        [SerializeField] private AudioClip _sceneSelectedClip;
        internal void SetSelectedScene(string _sceneName)
        {
            AudioToolService.PlayUISound(_sceneSelectedClip);
            _currentSceneSelected = _sceneName;
        }

        public void LoadScene() {

            StartCoroutine(ITransitionLoadScene());
            
        }
        private IEnumerator ITransitionLoadScene()
        {

            AudioToolService.PlayUISound(_playGameClip);
            yield return new WaitForSeconds(5);
            //fadeout o algo
            Bootstrap.Resolve<SceneInitiator>().LoadScene(_currentSceneSelected);
            yield return null;
        }
    }
}