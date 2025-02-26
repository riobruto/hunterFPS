using Game.Service;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Entities.Buttons
{
    public class ButtonSimple : SimpleInteractable
    {
         
        [Header("Properties")]
        [SerializeField] private bool _remainPressed;

        [SerializeField] private float _pressCooldown = 1;
        [SerializeField] private float _duration = 1;

        [Header("Visual Element")]
        [SerializeField] private Vector3 _pressedPositionOffset;

        [SerializeField] private Transform _buttonVisual;

        [Header("Audio")]
        [SerializeField] private AudioClip _pressedClip;
        [SerializeField] private AudioClip _releasedClip;

        private bool _isPressed;
        private bool _isInteracting;
        private bool _canInteract => !_isInteracting && !_isPressed;
        public override bool CanInteract => _canInteract;
        public override bool Taken => _canInteract;

        public override event InteractableDelegate InteractEvent;

        public UnityEvent ButtonPressedEvent;
        public UnityEvent ButtonReleasedEvent;

        public override bool Interact()
        {
            if (_isInteracting) return false;
            StartCoroutine(Press());
            _isPressed = true;
            ButtonPressedEvent?.Invoke();
            return true;
        }

        private IEnumerator Press()
        {
            if(_pressedClip != null) AudioToolService.PlayClipAtPoint(_pressedClip, transform.position, 1, AudioChannels.ENVIRONMENT, 20);
            _isInteracting = true;
            float time = 0;
            Vector3 from = Vector3.zero;
            Vector3 to = _pressedPositionOffset;
            while (time < _duration)
            {
                time += 0.01f;
                _buttonVisual.transform.localPosition = Vector3.Lerp(from, to, time / _duration);
                yield return null;
            }
            _buttonVisual.transform.localPosition = to;

            if (!_remainPressed)
            {
                StartCoroutine(Release());
                yield break;
            }
            _isInteracting = false;
            yield return null;
        }

        private IEnumerator Release()
        {
            yield return new WaitForSeconds(_pressCooldown);

            float time = 0;
            Vector3 from =  _pressedPositionOffset;
            Vector3 to = Vector3.zero;
            while (time < _duration)
            {
                time += 0.01f;
                _buttonVisual.transform.localPosition = Vector3.Lerp(from, to, time / _duration);
                yield return null;
            }
            if(_releasedClip != null) AudioToolService.PlayClipAtPoint(_releasedClip, transform.position, .5f, AudioChannels.ENVIRONMENT, 20);
            ButtonReleasedEvent?.Invoke();
            _isInteracting = false;
            _isPressed = false;
            yield return null;
        }
    }
}