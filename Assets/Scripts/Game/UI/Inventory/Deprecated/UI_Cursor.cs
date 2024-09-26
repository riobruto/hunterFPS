using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI.Inventory
{
    public class UI_Cursor : MonoBehaviour
    {
        private RectTransform _transform;
        private bool _visible = false;

        private void Start()
        {
            _transform = GetComponent<RectTransform>();
        }

        public void SetVisibility(bool visible)
        {
            _visible = visible;
            gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            if (!_visible) { return; }

            _transform.position = Mouse.current.position.value;
        }
    }
}