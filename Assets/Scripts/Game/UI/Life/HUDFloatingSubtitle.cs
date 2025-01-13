using Core.Engine;
using Game.Service;
using TMPro;
using UnityEngine;

namespace Game.UI.Life
{
    public class HUDFloatingSubtitle : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Transform _source;

        private Vector3 _vectorSource;
        private Canvas _canvas;
        private Camera _camera;
        private bool _followSource;
        private float _duration;
        private float _time;

        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _text;

        internal void Create(SubtitleParameters parameters, Canvas canvas)
        {
            _name.text = parameters.Name + ":";
            _text.text = parameters.Content;
            _source = parameters.Transform;
            _vectorSource = parameters.Transform.position;
            _canvas = canvas;
            _camera = Bootstrap.Resolve<PlayerService>().PlayerCamera;
            _rectTransform = GetComponent<RectTransform>();
            _time = 0;
            _duration = parameters.Duration;
        }

        private void LateUpdate()
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(_camera, _source.position);

            //DONDE QUIERO IR MENOS DONDE ESTOY
            //DONDE QUIERO IR MENOS DONDE ESTOY
            //DONDE QUIERO IR MENOS DONDE ESTOY
            //DONDE QUIERO IR MENOS DONDE ESTOY
            //DONDE QUIERO IR MENOS DONDE ESTOY
            //DONDE QUIERO IR MENOS DONDE ESTOY
            //DONDE QUIERO IR MENOS DONDE ESTOY
            //DONDE QUIERO IR MENOS DONDE ESTOY

            //DIR = B - A
            //DIR = B - A
            //DIR = B - A
            //DIR = B - A
            //DIR = B - A
            //DIR = B - A
            //DIR = B - A

            float dot = Vector3.Dot(_camera.transform.forward, _vectorSource - _camera.transform.position);
            float distance = Vector3.Distance(_camera.transform.position, _vectorSource);

            if (dot < 0 || distance > 15f) SetVisibility(0);
            else SetVisibility(1);

            _rectTransform.anchoredPosition = screenPos / _canvas.scaleFactor;

            if (_duration != 0)
            {
                _time += Time.deltaTime;
                if (_time > _duration)
                {
                    Terminate();
                }
            }
        }

        private void SetVisibility(float v)
        {
            _name.color = new Color(1, 1, 1, v);
            _text.color = new Color(1, 1, 1, v);
        }

        private void Terminate()
        {
            Destroy(gameObject);
        }
    }
}