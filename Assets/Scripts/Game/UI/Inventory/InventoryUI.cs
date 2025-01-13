using Core.Engine;
using Game.Inventory;
using Game.Service;
using UnityEngine;

namespace UI.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private InventoryContextMenu _contextMenu;
        private InventorySystem _inventorySystem;

        [SerializeField] private InventoryGrid _equippableGrid;
        [SerializeField] private InventoryGrid _consumableGrid;

        private InventorySlot _lastSlotPressed;

        private void Start()
        {
            _equippableGrid.GridSlotClicked += (x) => OnGridClicked(x);
            _consumableGrid.GridSlotClicked += (x) => OnGridClicked(x);

            _inventorySystem = InventoryService.Instance;

            _inventorySystem.InventoryItemGiven += OnItemGiven;
            _inventorySystem.InventoryItemRemoved += OnItemRemoved;
            _inventorySystem.ToggleInventoryEvent += OnInventoryToggle;

            _contextMenu.Use += Use;
            _contextMenu.Drop += Drop;

            Initialize();
        }

        private void OnInventoryToggle(bool state)
        {
            if (state)
            {
                Open();
                return;
            }
            Close();
        }

        private void OnItemRemoved(InventoryItem item)
        {
            if (item is ConsumableItem)
            {
                _consumableGrid.Remove(item);
                return;
            }

            if (item is EquipableItem)
            {
                _equippableGrid.Remove(item);
                return;
            }
        }

        private void OnItemGiven(InventoryItem item)
        {
            Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAA");

            if (item is ConsumableItem)
            {
                _consumableGrid.Add(item);
                return;
            }
            if (item is EquipableItem)
            {
                _equippableGrid.Add(item);
                return;
            }
        }

        private void OnGridClicked(InventorySlot slot)
        {
            Debug.Log("Item Presionado");

            _lastSlotPressed = slot;

            _contextMenu.Show(slot.Item);
        }

        private void Use()
        {
            _inventorySystem.UseItemFromUI(_lastSlotPressed.Item);
            _contextMenu.Hide();

            Debug.Log("Use");
        }

        private void Drop()
        {
            _contextMenu.Hide();
            _inventorySystem.DropItemFromUI(_lastSlotPressed.Item);     
            Debug.Log("Drop");
        }

        public void Initialize()
        {
            foreach (EquipableItem item in _inventorySystem.Equipables)
            {
                if (item != null) _equippableGrid.Add(item);
            }

            foreach (ConsumableItem item in _inventorySystem.Consumables)
            {
                if (item != null) _consumableGrid.Add(item);
            }

            gameObject.SetActive(false);
        }

        public void Open()
        {
            _lastSlotPressed = null;
            _contextMenu.Hide();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            _contextMenu.Hide();
            _lastSlotPressed = null;

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _equippableGrid.GridSlotClicked -= (x) => OnGridClicked(x);
            _consumableGrid.GridSlotClicked -= (x) => OnGridClicked(x);

            _inventorySystem.InventoryItemGiven -= OnItemGiven;
            _inventorySystem.InventoryItemRemoved -= OnItemRemoved;
            _inventorySystem.ToggleInventoryEvent -= OnInventoryToggle;

            _contextMenu.Use -= Use;
            _contextMenu.Drop -= Drop;
        }
    }
}