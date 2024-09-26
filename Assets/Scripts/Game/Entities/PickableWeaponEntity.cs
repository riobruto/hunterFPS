using Core.Engine;
using Core.Weapon;
using Game.Entities;
using Game.Player;
using Game.Player.Controllers;
using Game.Service;
using System;
using System.Collections;
using UnityEngine;

namespace Game.Entities
{
    public class PickableWeaponEntity : MonoBehaviour, IInteractable
    {
        private bool _canBeTaken = true;
        private bool _begun;
        private float _takeTime = 1f;
        private float _time = 0;
        private bool _completed;
        private InteractionTimer _timer;
        [SerializeField] private WeaponSettings _weapon;
        [SerializeField] private int _currentAmmo = 0;

        bool IInteractable.BeginInteraction()
        {
            if (!_canBeTaken) return false;
            if (_begun) return false;
            _timer.SetTimer(transform.position);
            _begun = true;
            return true;
        }

        private void Start()
        {
            _timer = Bootstrap.Resolve<InteractionTimerService>().Instance;
            gameObject.layer = 30;
            SetLayerAllChildren(this.transform, 30);
            _currentAmmo = _weapon.Ammo.Size;
        }

        private void SetLayerAllChildren(Transform root, int layer)
        {
            var children = root.GetComponentsInChildren<Transform>(includeInactive: true);

            foreach (var child in children)
            {
                child.gameObject.layer = layer;
            }
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
            _timer.UpdateTimer(_time, _takeTime, _begun);
        }

        bool IInteractable.IsDone(bool cancelRequest)
        {
            if (_completed)
            {
                GiveItem();
                _timer.HideTimer();
                _begun = false;
                _time = 0;
                //_canBeTaken = false;
                return true;
            }
            if (cancelRequest)
            {
                _timer.HideTimer();
                _canBeTaken = true;
                _begun = false;
                _time = 0;
                return true;
            }

            return false;
        }

        private void GiveItem()
        {
            if (FindObjectOfType<PlayerWeapons>().TryGiveWeapon(_weapon, _currentAmmo))
            {
                //{ Destroy(gameObject); }
            }
        }
    }
}