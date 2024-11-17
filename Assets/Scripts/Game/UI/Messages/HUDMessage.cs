using Game.Service;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Messages
{
    internal class HUDMessage : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        private Image _bg;
        private Color _targetColor;
        private Color _targeBgColor;
        private float _duration;

        public void Set(MessageParameters parameters)
        {
            _bg = GetComponent<Image>();

            _text.text = parameters.Text;
            _targetColor = parameters.Color;
            _targeBgColor = parameters.BackgroundColor;
            _duration = parameters.Duration;
            StartCoroutine(Show());
        }

        private IEnumerator Show()
        {
            float t = 0;
            float d = 1;
            while (t < d)
            {
                _text.color = Color.Lerp(Color.clear, _targetColor, t / d);
                _bg.color = Color.Lerp(Color.clear, _targeBgColor, t/ d );
                t += 0.1f;
                yield return null;
            }
            _text.color = _targetColor;
            _bg.color = _targeBgColor;
            yield return new WaitForSeconds(_duration);
            StartCoroutine(Hide());
            yield return null;
        }

        private IEnumerator Hide()
        {
            float t = 0;
            float d = 5;

            while (t < d)
            {
                _text.color = Color.Lerp(_targetColor, Color.clear, t / d);
                _bg.color = Color.Lerp(_targeBgColor, Color.clear, t / d);
                t += 0.1f;
                yield return null;
            }
            Destroy(gameObject);
            yield return null;
        }
    }
}