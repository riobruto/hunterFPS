using Core.Engine;
using Game.Player.Controllers;
using Game.Service;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.UI
{
    public class HUDAbilitiesRadial : MonoBehaviour
    {
        private bool _active;
        private const int SECTORS = 6;
        private int _currentSector = 0;
        private int _lastSector = 0;

        [SerializeField] private RectTransform[] _arrow;
        public UnityAction<int> ValueChangedEvent;

        // Use this for initialization

        private void Start()
        {
            _active = true;
            Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerAbilitiesController>().OpenRadialEvent += OnRadialInput;
        }

        private void OnRadialInput(bool state)
        {
            gameObject.SetActive(state);
            Cursor.lockState = CursorLockMode.Confined;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!_active) return;

            _currentSector = CalculateSector();
            if (_lastSector != _currentSector)
            {
                ValueChangedEvent?.Invoke(_currentSector);
                UpdateVisual();
                _lastSector = _currentSector;
            }
        }

        private void UpdateVisual()
        {
            for (int i = 0; i < _arrow.Length; i++)
            {
                if (i == CalculateSector())
                {
                    _arrow[i].localScale = Vector3.one + Vector3.up * .25f;
                    continue;
                }
                _arrow[i].localScale = Vector3.one;
            }
        }

        private int CalculateSector()
        {
            Vector2 mousePos;
            mousePos.x = Mouse.current.position.x.value;
            mousePos.y = Mouse.current.position.y.value;
            Vector2 mousePosCentered;
            mousePosCentered.x = mousePos.x - Screen.width / 2f;
            mousePosCentered.y = mousePos.y - Screen.height / 2f;

            Vector2 mouseDot;

            mousePosCentered = mousePosCentered.normalized;

            mouseDot.x = Vector2.Dot(Vector2.right, mousePosCentered);
            mouseDot.y = Vector2.Dot(Vector2.up, mousePosCentered);
            //Debug.Log(mouseDot);

            if (Vector2.Dot(Vector2.right, mouseDot) > 0)
            {
                //estamos en la derecha

                if (Mathf.Abs(Vector2.Dot(Vector2.up, mouseDot)) < 0.6666f)
                {
                    return 1;
                }
                else if (Vector2.Dot(Vector2.up, mouseDot) > 0.6666666f)
                {
                    return 0;
                }
                else return 2;
            }
            else
            {
                //estamos en la izquierda

                if (Mathf.Abs(Vector2.Dot(Vector2.up, mouseDot)) < 0.66666f)
                {
                    return 4;
                }
                else if (Vector2.Dot(Vector2.up, mouseDot) > 0.6666666f)
                {
                    return 5;
                }
                else return 3;
            }
        }
    }
}