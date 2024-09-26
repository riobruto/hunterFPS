using Core.Engine;
using Game.Player.Controllers;
using Game.Service;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUDWeaponSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;

        [SerializeField] private Image _image;
        private RectTransform _transform;

        private Vector2 _from;
        private Vector2 _to;

        private Vector2 _hiddenPosition;
        private Vector2 _selectedPosition;

        private float _moveTime = .5f;
        private float _time;

        [SerializeField] private AnimationCurve _curves;

        private void Start()
        {
            _transform = GetComponent<RectTransform>();

            _hiddenPosition = _transform.anchoredPosition;
            _hiddenPosition.y = -100;
            _selectedPosition = _transform.anchoredPosition;

            Hide();
        }

        [ContextMenu("Show")]
        public void Show()
        {
            _from = _hiddenPosition;
            _to = _selectedPosition;
            StartCoroutine(Move());
        }

        public void Show(float duration)
        {
            _from = _hiddenPosition;
            _to = _selectedPosition;
            StartCoroutine(Move());
            Invoke("Hide", duration);
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            _to = _hiddenPosition;
            _from = _transform.anchoredPosition;

            StartCoroutine(Move());
        }

        private IEnumerator Move()
        {
            _time = 0;

            while (_time < _moveTime)
            {
                _transform.anchoredPosition = Vector2.LerpUnclamped(_from, _to, _curves.Evaluate(_time / _moveTime));
                _time += Time.deltaTime;
                yield return null;
            }
            _transform.anchoredPosition = _to;
            yield return null;
        }

        internal void Set(PlayerWeaponSlot playerWeaponSlot)
        {
            _name.text = playerWeaponSlot.WeaponInstances[0].Settings.name;
            //_image.sprite = playerWeaponSlot.WeaponInstances[0].Settings.HUDSprite;
        }
    

    }
}