using Core.Engine;
using Game.Inventory;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public class PickableItemEntity : MonoBehaviour, IInteractable
    {
        [SerializeField] private InventoryItem[] _inventoryItem;

        private bool _canBeTaken = true;
        private bool _begun;
        private float _takeTime = 1f;
        private float _time = 0;
        private bool _completed;
        private InteractionTimer _timer;

        private void Start()
        {
            _timer = Bootstrap.Resolve<InteractionTimerService>().Instance;
            gameObject.layer = 30;
        }

        bool IInteractable.BeginInteraction(Vector3 pos)
        {
            if (!_canBeTaken) return false;
            if (_begun) return false;

            _timer.SetTimer(pos);
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
                _canBeTaken = false;
                _begun = false;
            }
            _timer.UpdateTimer(_time, _takeTime, _begun);
        }

        bool IInteractable.IsDone(bool cancelRequest)
        {
            if (_completed)
            {
                _timer.HideTimer();
                GiveItem();
                _begun = false;
                _canBeTaken = false;
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

        private bool GiveItem()
        {
            foreach (InventoryItem item in _inventoryItem)
            {
                bool canGiveItem = Bootstrap.Resolve<InventoryService>().Instance.TryAddItem(item);
                if (canGiveItem) { Destroy(gameObject); }
            }
            return true;
        }

        bool IInteractable.CanInteract() => _canBeTaken;
    }
}