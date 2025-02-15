using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class HUDLoadingScreen : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _text;

        private float _time;
        private float _fadeTime = .45f;
        private Color _targetColor;

        // [SerializeField] private TMP_Text _text;
        private void Start() => FadeOut();

        private void LateUpdate()
        {
            _background.color = Color.Lerp(_background.color, _targetColor, Time.deltaTime / _fadeTime);
            _background.gameObject.SetActive(_background.color.a > 0 ? true : false);
        }

        public void FadeOut() => _targetColor = new Color(1, 1, 1, 0);

        public void FadeIn() => _targetColor = new Color(1, 1, 1, 1);

        internal void ShowRespawnText(bool v) => _text.gameObject.SetActive(v);
    }
}