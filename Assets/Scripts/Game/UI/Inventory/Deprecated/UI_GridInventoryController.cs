using Game.Inventory;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

namespace Game.UI.Inventory
{
    public class UI_GridInventoryController : MonoBehaviour
    {
        private UI_ItemGrid _currentGrid;
        private UI_ItemGrid _lastGrid;
        private UI_InventoryItem _currentItem;
        private Vector2Int _lastItemPositionBeforePick;
        private RectTransform _currentItemRectTransform;

        [SerializeField] private UI_ItemGrid _mainGrid;
        public UI_ItemGrid MainGrid => _mainGrid;
        public UI_ItemGrid CurrentGrid { get => _currentGrid; internal set => _currentGrid = value; }

        [SerializeField] private List<InventoryItem> _spawnItems;

        [Header("UI References")]
        [SerializeField] private UI_Cursor _cursor;

        [SerializeField] private UI_InventoryContextMenuController _contextMenu;

        private void Start()
        {
            _contextMenu.DropButtonAction += OnContextMenuDrop;
            _contextMenu.FunctionButtonAction += OnContextMenuFunction;
        }

        private void OnContextMenuFunction()
        {
            _mainGrid.RemoveItem(_contextMenu.SelectedItem);
        }

        private void OnContextMenuDrop()
        {
            _mainGrid.RemoveItem(_contextMenu.SelectedItem);
        }

        private bool _active;

        public void ShowInventory()
        {
            _active = true;
            _cursor.SetVisibility(true);
            gameObject.SetActive(true);
        }

        public void HideInventory()
        {
            if (_currentItem != null)
            {
                RestoreItem();
            }

            if (_contextMenu.Active)
            {
                _contextMenu.Close();
            }
            _cursor.SetVisibility(false);
            _active = false;
            gameObject.SetActive(false);

            //TODO: Soltar item donde estaba
        }

        private void Update()
        {
            if (!_active) return;

            DragIcon();

            if (_currentGrid == null) return;

            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                CreateRandomItem();
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)//(Input.GetMouseButtonDown(1))
            {
                UI_InventoryItem item = _currentGrid.GetItemInPosition(_currentGrid.GetTileGridPosition(Mouse.current.position.value));
                if (item)
                {
                    _contextMenu.Open(item, Mouse.current.position.value);
                    return;
                }
                else
                {
                    if (_contextMenu.Active)
                    {
                        _contextMenu.Close();
                    }
                }
            }
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (_contextMenu.Active)
                {
                    _contextMenu.Close();
                    return;
                }

                Vector2 pos = Mouse.current.position.value;

                if (_currentItem != null)
                {
                    pos.x -= (_currentItem.ItemData.Width - 1) * UI_ItemGrid.TileSizePxWidth / 2;
                    pos.y += (_currentItem.ItemData.Height - 1) * UI_ItemGrid.TileSizePxWidth / 2;
                }

                Vector2Int posOnGrid = _currentGrid.GetTileGridPosition(pos);

                if (_currentItem == null)
                {
                    PickUpItem(posOnGrid);
                }
                else
                {
                    PlaceItem(posOnGrid);
                }
            }
        }

        private void CreateRandomItem()
        {
            GiveItem(_spawnItems[Random.Range(0, _spawnItems.Count)]);
        }

        private bool GiveItem(InventoryItem item)
        {
            if (item.Stackeable)
            {
                return AddStackeableItem(item);
            }
            return AddItem(item);
        }

        private bool AddStackeableItem(InventoryItem item)
        {
            UI_InventoryItem visualItem = _mainGrid.GetItemByType(item);
            if (visualItem != null)
            {
                if (visualItem.StackAmount + item.AmountPerObject < item.StackSize)
                {
                    visualItem.SetStackSize(visualItem.StackAmount + item.AmountPerObject);
                    return true;
                }

                int stack = visualItem.StackAmount;
                visualItem.SetStackSize(item.StackSize);

                return AddItem(item, item.AmountPerObject - stack);
            }

            //Creates new object
            return AddItem(item, item.AmountPerObject);
        }

        private bool AddItem(InventoryItem item)
        {
            UI_InventoryItem uiItem = Instantiate(Resources.Load("UI/UI_Item") as GameObject).GetComponent<UI_InventoryItem>();
            uiItem.Set(item);
            Vector2Int? posOnGrid = _mainGrid.FindSpaceForItem(uiItem);

            if (!posOnGrid.HasValue)
            {
                Destroy(uiItem.gameObject);
                return false;
            }
            if (!_mainGrid.PlaceItem(uiItem, posOnGrid.Value.x, posOnGrid.Value.y))
            {
                Destroy(uiItem.gameObject);
                return false;
            }

            return true;
        }

        private bool AddItem(InventoryItem item, int stackSize)
        {
            UI_InventoryItem uiItem = Instantiate(Resources.Load("UI/UI_Item") as GameObject).GetComponent<UI_InventoryItem>();
            uiItem.Set(item);
            Vector2Int? posOnGrid = _mainGrid.FindSpaceForItem(uiItem);

            if (!posOnGrid.HasValue)
            {
                Destroy(uiItem.gameObject);
                return false;
            }
            if (!_mainGrid.PlaceItem(uiItem, posOnGrid.Value.x, posOnGrid.Value.y))
            {
                Destroy(uiItem.gameObject);
                return false;
            }
            uiItem.SetStackSize(item.AmountPerObject);
            return true;
        }

        public UI_InventoryItem FindInventoryItemOfType(InventoryItem item)
        {
            return _mainGrid.GetItemByType(item);
        }

        private void PlaceItem(Vector2Int posOnGrid)
        {
            bool canPlace = _currentGrid.PlaceItem(_currentItem, posOnGrid.x, posOnGrid.y);

            if (canPlace)
            {
                _currentItem = null;
            }

            //Add some error sound or something.
        }

        private void PickUpItem(Vector2Int posOnGrid)
        {
            _currentItem = _currentGrid.PickUpItem(posOnGrid.x, posOnGrid.y);

            if (_currentItem != null)
            {
                SavePickInfo(_currentItem.Position);
                _currentItemRectTransform = _currentItem.GetComponent<RectTransform>();
                _currentItemRectTransform.SetAsLastSibling();
            }
        }

        private void SavePickInfo(Vector2Int posOnGrid)
        {
            _lastItemPositionBeforePick = posOnGrid;
            _lastGrid = _currentGrid;
        }

        private void RestoreItem()
        {
            _lastGrid.PlaceItem(_currentItem, _lastItemPositionBeforePick.x, _lastItemPositionBeforePick.y);
            _currentItem = null;
        }

        private void DragIcon()
        {
            if (_currentItem != null)
            {
                Vector2 pos = Mouse.current.position.value;

                pos.x -= (_currentItem.ItemData.Width - 1) * UI_ItemGrid.TileSizePxWidth / 2;
                pos.y += (_currentItem.ItemData.Height - 1) * UI_ItemGrid.TileSizePxWidth / 2;

                _currentItemRectTransform.position = pos;
            }
        }

        public bool TryGiveItem(InventoryItem item)
        {
            return GiveItem(item);
        }

        public bool TryRemoveItem(InventoryItem item)
        {
            //Add logic for stackables
            if (_mainGrid.HasItem(item))
            {
                _mainGrid.RemoveItemByType(item);
                return true;
            }
            return false;
        }
    }
}