using Game.Inventory;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public class PickableItemEntity : SimpleInteractable
    {
        [SerializeField] private InventoryItem[] _inventoryItem;

        private bool _canBeTaken = true;
        public override bool CanInteract => _canBeTaken;

        public override bool Taken => false;

        public override event InteractableDelegate InteractEvent;

        private void Start()
        {
            gameObject.layer = 30;
        }

        private bool GiveItem()
        {
            foreach (InventoryItem item in _inventoryItem)
            {
                bool canGiveItem = InventoryService.Instance.TryAddItem(item);
                if (canGiveItem) { Destroy(gameObject); }
            }
            return true;
        }

        public override bool Interact()
        {
            if (GiveItem()) { Destroy(gameObject); return true; }
            else return false;
        }
    }
}