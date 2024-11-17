using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class HUDLoadingScreen : MonoBehaviour
    {
        [SerializeField] private Image _background;
        private float _time;
        private float _fadeTime = 5f;

        // [SerializeField] private TMP_Text _text;
        private void Start()
        {
            StartCoroutine(FadeOut());
        }

        public IEnumerator FadeOut()
        {
            _time = 0;

            while (_time < _fadeTime)
            {
                _background.color = Color.Lerp(_background.color, new Color(1, 1, 1, 0), _time / _fadeTime);
                _time += Time.deltaTime;
                yield return null;
            }
            yield return null;
            _background.enabled = false;
        }

        public IEnumerator FadeIn()
        {
            _time = 0;
            _background.enabled = true;

            while (_time < _fadeTime)
            {
                _background.color = Color.Lerp(_background.color, new Color(1, 1, 1, 1), _time / _fadeTime);
                _time += Time.deltaTime;
                yield return null;
            }

            yield return null;
        }
    }
}