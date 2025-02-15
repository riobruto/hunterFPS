using Game.UI;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Level
{
    public class SceneChangeTrigger : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private Scene _scene;

        private float _fadeTime = 1.5f;

        private void OnTriggerEnter(Collider other) => StartCoroutine(ChangeScene());      
        private IEnumerator ChangeScene(){

            //desea continuar?
            yield return new WaitForSeconds(_fadeTime);
            SceneManager.LoadScene(_scene.name);
            yield return null;
        }
    }
}