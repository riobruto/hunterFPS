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

        private TrainControlPossesable _currentTrain = null;

        public TrainControlPossesable CurrentTrain { get => _currentTrain; }

        private void Start()
        {
            _defaultCameraParent = Camera.main.transform.parent;
        }

        internal void Exit(TrainControlPossesable trainControlPossesable)
        {
            PlayerExitEvent?.Invoke();
            transform.parent = null;
            GetComponent<PlayerMovementController>().Teletransport(trainControlPossesable.PlayerExitPosition.position);          
            transform.rotation = Quaternion.identity;
            _inInTrain = false;
            _currentTrain = null;
        }

        internal void Enter(TrainControlPossesable trainControlPossesable)
        {
            _currentTrain = trainControlPossesable;
            PlayerEnterEvent?.Invoke();
            transform.parent = trainControlPossesable.PlayerSeatPosition;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            _inInTrain = true;
        }

        private void OnInteract(InputValue value)
        {
            if (!_inInTrain) { return; }

            if (value.isPressed)
            {
                _currentTrain.ExitRequest();
            }
        }
    }
}