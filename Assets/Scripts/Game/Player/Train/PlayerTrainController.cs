using Game.Player.Train;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerTrainController : MonoBehaviour
    {
        private bool _inInTrain;
        private bool _isLookFree;
        public UnityAction PlayerEnterEvent;
        public UnityAction PlayerExitEvent;
        private Transform _defaultCameraParent;

        private PlayerRigidbodyMovement _movement;

        private TrainControlPossesable _currentTrain = null;

        public TrainControlPossesable CurrentTrain { get => _currentTrain; }

        private void Start()
        {
            _movement = GetComponent<PlayerRigidbodyMovement>();
            _defaultCameraParent = Camera.main.transform.parent;
        }

        internal void Exit(TrainControlPossesable trainControlPossesable)
        {
            PlayerExitEvent?.Invoke();
            transform.SetParent(null, false);
            _movement.Teletransport(trainControlPossesable.PlayerExitPosition.position);
            _movement.Simulate(true);
            _inInTrain = false;
            _currentTrain = null;
        }

        internal void Enter(TrainControlPossesable trainControlPossesable)
        {
            PlayerEnterEvent?.Invoke();
            _movement.Simulate(false);
            transform.SetParent(trainControlPossesable.PlayerSeatPosition, false);
            transform.localPosition = (Vector3.zero);
            //_movement.Teletransport(trainControlPossesable.PlayerSeatPosition.position);

            _inInTrain = true;
            _currentTrain = trainControlPossesable;
        }

        private void OnInteract(InputValue value)
        {
            if (!_inInTrain) { return; }

            if (value.isPressed)
            {
                _currentTrain.ExitRequest();
            }
        }

        private void OnCouple(InputValue value)
        {
            if (!_inInTrain) { return; }

            if (value.isPressed)
            {
                _currentTrain.CoupleRequest();
            }
        }
    }
}