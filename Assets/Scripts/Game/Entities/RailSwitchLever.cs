using Game.Service;
using Rail;
using UnityEngine;

namespace Game.Entities
{
    internal class RailSwitchLever : TimedInteractable
    {
        [SerializeField] private RailroadJunction _junction;
        [SerializeField] private bool _currentState = false;
        [SerializeField] private Transform _lever;

        [SerializeField] private bool _flipAnimation;

        public override void OnStart()
        {
            _junction.JunctionChangeSwitch += OnRailChanged;
            _currentState = _junction.GetSwitchState();
        }

        private void OnRailChanged(bool state)
        {
            if (_currentState != state)
            {
                ChangeState(state);
            }
        }

        private void ChangeState(bool state)
        {
            _currentState = state;
            _lever.rotation = Quaternion.Euler(0, 0, (state ? 40 : -40) * (_flipAnimation ? -1 : 1));
        }

        public override void OnBeginInteract()
        {
        }

        public override void OnEndInteract(bool value)
        {
            if (value)
            {
                UIService.CreateMessage(new("Railswitch changed", 5, Color.white, new Color(0, 0, 0, .5f)));
                _junction.SetSwitchState(!_currentState);

                ResetInteraction();
            }
        }

        public override void OnInteracting()
        {
        }

        public override void OnReleaseInteract()
        {
        }
    }
}