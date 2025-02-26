using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    public class MainLoadSceneScreen : MonoBehaviour
    {
        [SerializeField] private Slider _loadBar;
        [SerializeField] private Image _background;

        private float _fadeTime = .45f;
        private Color _targetColor;

        private void LateUpdate()
        {
            _background.color = Color.Lerp(_background.color, _targetColor, Time.deltaTime / _fadeTime);
            _background.gameObject.SetActive(_background.color.a > 0 ? true : false);
        }

        public void FadeOut() => _targetColor = new Color(1, 1, 1, 0);

        public void FadeIn() => _targetColor = new Color(1, 1, 1, 1);

        /// <summary>
        /// de 0 a 1 mierda
        /// </summary>
        /// <param name="progress"></param>
        public void SetProgress(float progress) => _loadBar.value = progress;

        internal void Load(AsyncOperation operation)
        {
            gameObject.SetActive(true);
            _loadBar.gameObject.SetActive(true);
            FadeIn();
            StartCoroutine(ILoad(operation));
        }

        private IEnumerator ILoad(AsyncOperation operation)
        {
            while (!operation.isDone)
            {
                SetProgress(operation.progress);
                yield return null;
            }
            _loadBar.gameObject.SetActive(false);
            FadeOut();
            yield return new WaitForSeconds(5);
            gameObject.SetActive(false);
        }
    }
}