using Core.Engine;
using Game.Entities;
using Game.Service;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player.Train
{
    internal class TrainEnterEntity : MonoBehaviour, IInteractable
    {
        public UnityAction EnterEvent;

        private bool _canEnter = true;
        private bool _begun;
        private float _timeToEnter = 2;
        private float _time = 0;
        private bool _entered;

        private InteractionTimer _timer;

        private void Start()
        {
            _timer = Bootstrap.Resolve<InteractionTimerService>().Instance;
            gameObject.layer = 30;
        }

        private Vector3 _timerpos;

        bool IInteractable.BeginInteraction(Vector3 p)
        {
            if (!_canEnter) return false;
            if (_begun) return false;
            _timerpos = p;
            _timer.SetTimer(p);
            _begun = true;
            return true;
        }

        private void Update()
        {
            if (!_begun) return;
            _time += Time.deltaTime;
            //Debug.Log("Entering: " + _time);

            if (_time > _timeToEnter)
            {
                _entered = true;
                _canEnter = false;
                _begun = false;
            }
            _timer.UpdateTimer(_time, _timeToEnter, _begun, _timerpos);
        }

        bool IInteractable.IsDone(bool cancelRequest)
        {
            if (_entered)
            {
                _timer.HideTimer();
                EnterEvent?.Invoke();
                _begun = false;
                _canEnter = false;
                return true;
            }
            if (cancelRequest)
            {
                _timer.HideTimer();
                _canEnter = true;
                _begun = false;
                _time = 0;
                return true;
            }

            return false;
        }

        internal void Reset()
        {
            _canEnter = true;
            _entered = false;
            _time = 0;
        }

        bool IInteractable.CanInteract() => _canEnter;
    }
}