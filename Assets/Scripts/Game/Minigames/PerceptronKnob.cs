using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Minigames
{
    public class PerceptronKnob : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _value;
        [SerializeField] private float _sensibility = .1f;
        [SerializeField] private int _range = 10;

        [SerializeField] private Vector2 _startPos = Vector2.zero;
        [SerializeField] private Vector2 _currentPos;
        [SerializeField] private RectTransform _knob;
        [SerializeField] private Image _color;

        private bool _capturing;
        private float _startingValue;

        public float Value { get => _value; }

        private void Start()
        {
            _color = GetComponent<Image>();
        }
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            _capturing = true;
            _startPos = Mouse.current.position.value;
            _startingValue = _value;

        }

        private void Update()
        {
            if (_capturing)
            {
                _currentPos = Mouse.current.position.value;

                _value = Mathf.Clamp((_startPos - _currentPos).y * _sensibility + _startingValue, -_range, _range);
                _value = Mathf.FloorToInt(_value);
                _color.color = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(-_range, _range, _value));
                _knob.eulerAngles = new Vector3(0, 0, Mathf.Lerp(145, -145, Mathf.InverseLerp(-_range, _range, _value)));
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            _capturing = false;
            
        }
    }
}