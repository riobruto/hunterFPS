using Core.Engine;
using Game.Service;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Entities
{
    internal class TimedInteractable : MonoBehaviour, IInteractable
    {
        public UnityAction InteractEvent;
        private bool _canInteract = true;
        private bool _begun;

        private float _timeToInteract = 2;
        private float _time = 0;
        private bool _interacted;
        private InteractionTimer _timer;

        private void Start()
        {
            _timer = Bootstrap.Resolve<InteractionTimerService>().Instance;
            gameObject.layer = 30;

            OnStart();
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnInteracting()
        { }

        public virtual void OnBeginInteract()
        { }

        public virtual void OnEndInteract(bool value)
        { }

        public virtual void OnReleaseInteract()
        { }

        bool IInteractable.BeginInteraction(Vector3 position)
        {
            if (!_canInteract) return false;
            if (_begun) return false;
            _timer.SetTimer(position);
            _begun = true;
            OnBeginInteract();
            return true;
        }

        private void Update()
        {
            if (!_begun) return;
            _time += Time.deltaTime;
            //Debug.Log("Entering: " + _time);
            OnInteracting();
            if (_time > _timeToInteract)
            {
                _interacted = true;
                _canInteract = false;
                _begun = false;
            }
            _timer.UpdateTimer(_time, _timeToInteract, _begun);
        }

        bool IInteractable.IsDone(bool cancelRequest)
        {
            if (_interacted)
            {
                _timer.HideTimer();
                InteractEvent?.Invoke();
                _begun = false;
                _canInteract = false;
                OnEndInteract(true);
                return true;
            }
            if (cancelRequest)
            {
                _timer.HideTimer();
                _canInteract = true;
                _begun = false;
                _time = 0;
                OnEndInteract(false);
                return true;
            }
            OnReleaseInteract();
            return false;
        }

        internal virtual void ResetInteraction()
        {
            _canInteract = true;
            _interacted = false;
            _time = 0;
        }

        bool IInteractable.CanInteract() => _canInteract;
    }
}