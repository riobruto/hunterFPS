using Game.Inventory;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Inventory
{
    public class UI_ItemGrid : MonoBehaviour
    {
        public const float TileSizePxWidth = 48;
        public const float TileSizePxHeight = 48;

        private UI_InventoryItem[,] _inventoryItemSlot;
        private RectTransform _rectTransform;
        [SerializeField] private Vector2Int _gridSize;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            Init(Mathf.Abs(_gridSize.x), Mathf.Abs(_gridSize.y));
        }

        private void Init(int width, int height)
        {
            _inventoryItemSlot = new UI_InventoryItem[width, height];
            Vector2 size = new Vector2(width * TileSizePxWidth, height * TileSizePxHeight);
            _rectTransform.sizeDelta = size;
        }

        private Vector2 _positionOnTheGrid;
        private Vector2Int _tileGridPosition;

        public Vector2Int GetTileGridPosition(Vector2 mousePosition)
        {
            _positionOnTheGrid.x = mousePosition.x - _rectTransform.position.x;
            _positionOnTheGrid.y = _rectTransform.position.y - mousePosition.y;

            _tileGridPosition.x = (int)(_positionOnTheGrid.x / TileSizePxWidth);
            _tileGridPosition.y = (int)(_positionOnTheGrid.y / TileSizePxHeight);

            return _tileGridPosition;
        }

        public bool PlaceItem(UI_InventoryItem inventoryItem, int posX, int posY)
        {
            if (BoundaryCheck(posX, posY, inventoryItem.ItemData.Width, inventoryItem.ItemData.Height) == false)
            {
                return false;
            }

            if (OverlapCheck(posX, posY, inventoryItem.ItemData.Width, inventoryItem.ItemData.Height))
            {
                return false;
            }

            RectTransform rectTransform = inventoryItem.GetComponent<RectTransform>();
            rectTransform.SetParent(_rectTransform);

            rectTransform.localScale = Vector3.one;
            for (int x = 0; x < inventoryItem.ItemData.Width; x++)
            {
                for (int y = 0; y < inventoryItem.ItemData.Height; y++)
                {
                    _inventoryItemSlot[posX + x, posY + y] = inventoryItem;
                }
            }

            Vector2 position = new Vector2();
            position.x = posX * TileSizePxWidth;
            position.y = -(posY * TileSizePxHeight);

            rectTransform.localPosition = position;
            inventoryItem.Position.x = posX;
            inventoryItem.Position.y = posY;
            inventoryItem.Place();
            return true;
        }

        internal UI_InventoryItem PickUpItem(int x, int y)
        {
            UI_InventoryItem toReturn = _inventoryItemSlot[x, y];
            if (toReturn == null) return null;

            for (int ix = 0; ix < toReturn.ItemData.Width; ix++)
            {
                for (int iy = 0; iy < toReturn.ItemData.Height; iy++)
                {
                    _inventoryItemSlot[toReturn.Position.x + ix, toReturn.Position.y + iy] = null;
                }
            }
            toReturn.Pick();

            return toReturn;
        }

        private bool PositionCheck(int x, int y)
        {
            if (x < 0 || y < 0)
            {
                return false;
            }
            if (x >= _gridSize.x || y >= _gridSize.y)
            {
                return false;
            }
            return true;
        }

        internal UI_InventoryItem GetItemInPosition(Vector2Int position)
        {
            return _inventoryItemSlot[position.x, position.y];
        }

        private bool BoundaryCheck(int x, int y, int width, int height)
        {
            if (PositionCheck(x, y) == false) return false;
            x += width - 1;
            y += height - 1;
            if (PositionCheck(x, y) == false) return false;

            return true;
        }

        private bool OverlapCheck(int x, int y, int width, int height)
        {
            for (int ix = 0; ix < width; ix++)
            {
                for (int iy = 0; iy < height; iy++)
                {
                    if (_inventoryItemSlot[x + ix, y + iy] != null)
                        return true;
                }
            }
            return false;
        }

        internal Vector2Int? FindSpaceForItem(UI_InventoryItem item)
        {
            int width = _gridSize.x - item.ItemData.Width + 1;
            int height = _gridSize.y - item.ItemData.Height + 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!OverlapCheck(x, y, item.ItemData.Width, item.ItemData.Height))
                    {
                        Debug.Log("Found Position");
                        return new Vector2Int?(new Vector2Int(x, y));
                    }
                }
            }
            Debug.Log("Inventory Full");
            return null;
        }

        internal bool HasItem(InventoryItem item)
        {
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    if (_inventoryItemSlot[x, y] != null)
                    {
                        if (_inventoryItemSlot[x, y].ItemData == item) return true;
                    }
                }
            }
            return false;
        }

        internal UI_InventoryItem GetItemByType(InventoryItem item)
        {
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    if (_inventoryItemSlot[x, y] != null)
                    {
                        if (_inventoryItemSlot[x, y].ItemData == item) return _inventoryItemSlot[x, y];
                    }
                }
            }
            return null;
        }

        internal List<UI_InventoryItem> GetAllItemsByType(InventoryItem item)
        {
            List<UI_InventoryItem> list = new List<UI_InventoryItem>();

            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    if (_inventoryItemSlot[x, y] != null)
                    {
                        if (_inventoryItemSlot[x, y].ItemData == item)
                        {
                            if (!list.Contains(_inventoryItemSlot[x, y]))
                            {
                                list.Add(_inventoryItemSlot[x, y]);
                            }
                        }
                    }
                }
            }
            return list;
        }

        internal void RemoveItemByType(InventoryItem item)
        {
            UI_InventoryItem uiItem = GetItemByType(item);

            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    if (_inventoryItemSlot[x, y] == uiItem)
                    {
                        _inventoryItemSlot[x, y] = null;
                    }
                }
            }

            Destroy(uiItem.gameObject);
        }

        internal void RemoveItem(UI_InventoryItem selectedItem)
        {
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    if (_inventoryItemSlot[x, y] == selectedItem)
                    {
                        _inventoryItemSlot[x, y] = null;
                    }
                }
            }

            Destroy(selectedItem.gameObject);
        }
    }
}