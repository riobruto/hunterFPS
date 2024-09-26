using Game.Inventory;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Inventory
{
    public class InventoryGrid : MonoBehaviour
    {
        [SerializeField] private int _gridSizeX = 2;
        [SerializeField] private int _gridSizeY = 4;

        [SerializeField] private Sprite _slotBackground;     

        private InventorySlot[,] _inventorySlots;
        public Sprite BackgroundSprite { get => _slotBackground; }

        public UnityAction<InventorySlot> GridSlotClicked;

        private void Start()
        {
            _inventorySlots = new InventorySlot[_gridSizeX, _gridSizeY];
            GenerateSlots();
        }

        private void GenerateSlots()
        {
            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    InventorySlot slot = new GameObject("Slot", typeof(RectTransform)).AddComponent<InventorySlot>();
                    slot.transform.SetParent(transform);
                    slot.Initialize(x, y, this);
                    _inventorySlots[x, y] = slot;
                }
            }
        }

        public void ReportItemClicked(InventorySlot inventorySlot)
        {

            GridSlotClicked?.Invoke(inventorySlot);

        }

        public void Remove(InventoryItem item)
        {
            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    if (_inventorySlots[x, y].Item == item)
                    {
                        _inventorySlots[x, y].Empty();
                        return;
                    }
                }
            }
        }

        public void Add(InventoryItem item)
        {
            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    if (_inventorySlots[x, y].Item == null)
                    {
                        _inventorySlots[x, y].Set(item);
                        return;
                    }
                }
            }
        }
    }
}