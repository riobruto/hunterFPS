using Core.Engine;
using Game.Service;
using Game.Weapon;
using System;
using System.Collections;
using UnityEngine;

namespace Game.Entities.Grenades
{
    public class PickableGrenadeEntity : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _amount = 3;
        [SerializeField] private GrenadeType _type = GrenadeType.HE;
        private bool _canBeTaken = true;
        private bool _begun;
        private float _time;
        private bool _completed;
        private float _takeTime = 1f;

        private void Start()
        {
            gameObject.layer = 30;
        }

        bool IInteractable.BeginInteraction(Vector3 position)
        {
            if (!_canBeTaken) return false;
            if (_begun) return false;

            _begun = true;
            return true;
        }

        private void Update()
        {
            if (!_begun) return;
            _time += Time.deltaTime;
            Debug.Log("Taking: " + _time);

            if (_time > _takeTime)
            {
                _completed = true;
                //_canBeTaken = false;
                _begun = false;
            }
        }

        bool IInteractable.IsDone(bool cancelRequest)
        {
            if (_completed)
            {
                GiveItem();
                //_timer.HideTimer();
                _begun = false;
                _time = 0f;
                //_canBeTaken = false;
                return true;
            }
            if (cancelRequest)
            {
                //_timer.HideTimer();
                _canBeTaken = true;
                _begun = false;
                _time = 0f;
                return true;
            }

            return false;
        }

        private void GiveItem()
        {
            InventoryService.Instance.GiveGrenades(_amount, _type);
        }

        bool IInteractable.CanInteract() => _canBeTaken;

        // Use this for initialization
    }
}