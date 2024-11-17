using Game.UI;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Level
{
    public class SceneChangeTrigger : MonoBehaviour
    {
        //[SerializeField] private SceneAsset scene;

        private float _fadeTime = 1.5f;

        private void OnTriggerEnter(Collider other)
        {
            StartCoroutine(FindObjectOfType<HUDLoadingScreen>().FadeIn());
            StartCoroutine(ChangeScene());
        }

        private IEnumerator ChangeScene()
        {
            yield return new WaitForSeconds(_fadeTime);
            //SceneManager.LoadScene(scene.name);
            SceneManager.LoadScene("UI", LoadSceneMode.Additive);
            yield return null;
        }
    }
}