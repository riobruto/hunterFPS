using Core.Engine;
using Game.Objectives;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class HUDObjectiveReticle : MonoBehaviour
    {
        [SerializeField] private Image _target;
        private Camera _camera;
        private Canvas _canvas;
        private Vector3 _vectorSource;

        private ObjectiveService _objectives;

        private void Start()
        {
            _objectives = Bootstrap.Resolve<ObjectiveService>();
            _camera = Camera.main;
            _canvas = GetComponentInParent<Canvas>();
        }

        private void LateUpdate()
        {
            _vectorSource = _objectives.ObjectivePoint;
            _target.gameObject.SetActive(_vectorSource != Vector3.zero);

            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(_camera, _vectorSource);
            float dot = Vector3.Dot(_camera.transform.forward, _vectorSource - _camera.transform.position);
            if (dot > 0)
            {
                screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
                screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);
                _target.rectTransform.anchoredPosition = screenPos / _canvas.scaleFactor;
            }

            // SetVisibility(0);
            //else SetVisibility(1);
        }

        private void SetVisibility(float v)
        {
            _target.color = new Color(1, 1, 1, v);
        }
    }
}