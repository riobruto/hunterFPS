using Core.Engine;
using Game.Player;
using Game.Service;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Train
{
    public class HUDTrain : MonoBehaviour
    {
        [SerializeField] private Sprite[] _panelBackgroundSprites;
        [SerializeField] private Sprite[] _throttleSprites;
        [SerializeField] private Sprite[] _reverserSprites;
        [SerializeField] private Image _throttle;
        [SerializeField] private Image _reverser;
        [SerializeField] private Image _background;
        [SerializeField] private Image _needle;
        [SerializeField] private Slider _fuel;

        [SerializeField] private RectTransform _rect;

        private Vector3 _openPose;
        private Vector3 _closedPose;
        private PlayerTrainController _controller;
        private bool _active;
        private bool _lastActive;

        [SerializeField] private Slider _stabilityRight;
        [SerializeField] private Slider _stabilityLeft;
        [SerializeField] private Image _stabilityRightFill;
        [SerializeField] private Image _stabilityLeftFill;

        // Use this for initialization
        private void Start()
        {
            _openPose = _rect.localPosition;
            _closedPose = _openPose - Vector3.up * 480f;
            _controller = Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerTrainController>();
            StartCoroutine(ShowHud(false));
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            _active = _controller.CurrentTrain;

            if (_active != _lastActive)
            {
                StartCoroutine(ShowHud(_active));
                _lastActive = _active;
            }
            if (_active)
            {
                _stabilityRight.value = _controller.CurrentTrain.SidewayStress / 2f;
                _stabilityLeft.value = _controller.CurrentTrain.SidewayStress * -1f / 2f;
                _stabilityRightFill.color = new Color(1, 1, 1, _controller.CurrentTrain.SidewayStress / 2f);
                _stabilityLeftFill.color = new Color(1, 1, 1, _controller.CurrentTrain.SidewayStress * -1f / 2f);

                _throttle.sprite = _throttleSprites[_controller.CurrentTrain.CurrentAccelerationLevel];
                _reverser.sprite = _reverserSprites[_controller.CurrentTrain.CurrentReverser + 1];
                _background.sprite = _panelBackgroundSprites[_controller.CurrentTrain.CurrentBrakeLevel];
                float needleValue = Mathf.Lerp(-45, -315, Mathf.InverseLerp(0, 60, _controller.CurrentTrain.SpeedInKmh));
                _needle.rectTransform.localRotation = Quaternion.Euler(0, 0, needleValue);

                _fuel.value = _controller.CurrentTrain.Fuel;
                _fuel.maxValue = _controller.CurrentTrain.MaxFuel;
            }
        }

        private IEnumerator ShowHud(bool active)
        {
            if (active) _rect.gameObject.SetActive(true);
            float time = 0;
            float duration = .5f;
            Vector3 from = active ? _closedPose : _openPose;
            Vector3 to = active ? _openPose : _closedPose;
            while (time < duration)
            {
                _rect.localPosition = Vector3.Lerp(from, to, time / duration);
                time += 0.01f;
                yield return null;
            }
            _rect.localPosition = to;
            if (!active) _rect.gameObject.SetActive(false);
            yield return null;
        }
    }
}