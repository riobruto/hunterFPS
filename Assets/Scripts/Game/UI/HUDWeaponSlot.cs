using Core.Engine;
using Core.Weapon;
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
        [SerializeField] private WeaponSlotType _slotType;

        public WeaponSlotType Type => _slotType;

        private void Start()
        {
            _transform = GetComponent<RectTransform>();
            _selectedPosition = _transform.anchoredPosition;
            _hiddenPosition = _selectedPosition + Vector2.right * 150;

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
            _from = _transform.anchoredPosition;
            _to = _hiddenPosition; 

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
            _slotType = playerWeaponSlot.SlotType;
            _name.text = playerWeaponSlot.WeaponInstances[0].Settings.name;
            Sprite sprite = playerWeaponSlot.WeaponInstances[0].Settings.HUDSprite;
            if (sprite)
            {
                _image.sprite = sprite;
            }
        }
    }
}