using System.Collections;
using UnityEngine;

namespace Game.Entities.Doors
{
    public class DoorEntity : MonoBehaviour, IInteractable
    {
        private float _time = 1;

        private bool _state = false;
        private bool _isRotating = false;
        private Vector3 _rotationTarget;

        private Vector3 _closedRotation = new Vector3(0, 0, 0);
        private Vector3 _openRotation = new Vector3(0, 95, 0);

        bool IInteractable.BeginInteraction()
        {
            if (_isRotating) return false;
            _state = !_state;
            _rotationTarget = _state ? _openRotation : _closedRotation;
            StartCoroutine(Rotate());
            return true;
        }

        bool IInteractable.IsDone(bool cancelRequest) => true;

        private IEnumerator Rotate()
        {
            float timeElapsed = 0;

            while (timeElapsed < _time)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(_rotationTarget), timeElapsed / _time);
                timeElapsed += Time.deltaTime;
                _isRotating = true;
                yield return null;
            }
            _isRotating = false;
        }
    }
}