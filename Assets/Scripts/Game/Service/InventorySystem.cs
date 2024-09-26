using Core.Engine;
using Game.Inventory;
using Game.Weapon;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Service
{
    public class InventoryService : SceneService
    {
        public InventorySystem Instance;

        internal override void Initialize()
        {
            Instance = new GameObject("InventorySystem").AddComponent<InventorySystem>();
            GameObject.DontDestroyOnLoad(Instance);
            Instance.Initialize();
        }

        public static void SaveInventory()

        {   //TODO: Logica de guardado y cargado creada por el service.
            //PlayerPrefs.Save();
        }

        public static void LoadInventory()
        {
        }
    }

    public delegate void InventoryControllerDelegate(bool state);

    public delegate void InventoryConsumableDelegate(ConsumableItem item);

    public delegate void InventoryEquippableDelegate(EquipableItem item);

    public class InventorySystem : MonoBehaviour

    {
        public event InventoryControllerDelegate ToggleInventoryEvent;

        public ConsumableItem[] Consumables = new ConsumableItem[8];
        public EquipableItem[] Equipables = new EquipableItem[8];

        public EquipableItem[] Equipped = new EquipableItem[3];

        public Dictionary<AmmunitionItem, int> Ammunitions;

        public Dictionary<GrenadeType, int> Grenades;

        public int GrenadeLimitPerType = 3;

        public event UnityAction<InventoryItem> InventoryItemGiven;

        public event UnityAction<InventoryItem> InventoryItemRemoved;

        public event InventoryConsumableDelegate UseConsumableEvent;

        public event InventoryEquippableDelegate UseEquippableEvent;

        public bool TryAddItem(InventoryItem item)
        {
            if (item is ConsumableItem)
            {
                for (int i = 0; i < Consumables.Length; i++)
                {
                    if (Consumables[i] == null)
                    {
                        Consumables[i] = item as ConsumableItem;
                        InventoryItemGiven?.Invoke(item);
                        return true;
                    }
                    continue;
                }
                return false;
            }

            if (item is EquipableItem)
            {
                for (int i = 0; i < Equipables.Length; i++)
                {
                    if (Equipables[i] == null)
                    {
                        Equipables[i] = item as EquipableItem;
                        InventoryItemGiven?.Invoke(item);
                        return true;
                    }
                    continue;
                }
                return false;
            }
            return false;
        }

        public bool TryRemoveItem(InventoryItem item)
        {
            if (item is ConsumableItem)
            {
                for (int i = 0; i < Consumables.Length; i++)
                {
                    if (Consumables[i] == item)
                    {
                        Consumables[i] = null;
                        InventoryItemRemoved?.Invoke(item);
                        return true;
                    }
                    continue;
                }
                return false;
            }

            if (item is EquipableItem)
            {
                for (int i = 0; i < Equipables.Length; i++)
                {
                    if (Equipables[i] == item)
                    {
                        Equipables[i] = null;
                        InventoryItemRemoved?.Invoke(item);
                        return true;
                    }
                    continue;
                }
                return false;
            }
            return false;
        }

        public void Initialize()
        {
            Ammunitions = new Dictionary<AmmunitionItem, int>();

            AmmunitionItem[] _types = Resources.FindObjectsOfTypeAll<AmmunitionItem>();
            foreach (AmmunitionItem ammo in _types)
            {
                Ammunitions.Add(ammo, 150);
            }

            Grenades = new Dictionary<GrenadeType, int>();

            foreach (GrenadeType grenade in Enum.GetValues(typeof(GrenadeType)))
            {
                //Grenades.Add(grenade, 3);
            }
            //GetSavedData
        }

        internal void ShowInventoryUI()
        {
            ToggleInventoryEvent?.Invoke(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        internal void HideInventoryUI()
        {
            ToggleInventoryEvent?.Invoke(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void UseItemFromUI(InventoryItem item)
        {
            if (item is ConsumableItem)
            {
                UseConsumableEvent?.Invoke(item as ConsumableItem);
                //Este evento se escucha desde PlayerInventorySystem y se ordena la eliminacion de items alli.
                return;
            }
            UseEquippableEvent?.Invoke(item as EquipableItem);
        }

        internal void DropItemFromUI(InventoryItem item)
        {//todo: add drop logic
        }

        internal void GiveGrenades(int amount, GrenadeType type)
        {
            Grenades[type] = amount - Grenades[type];
        }
    }

    //INVENTARIO VIEJO!!

    /*public class InventorySystem : MonoBehaviour
    {
        public event InventoryControllerDelegate ShowInventoryEvent;

        private UI_GridInventoryController _UIController;
        private Vector2Int _size;

        public void Initialize()
        {
            _itemGrid = new InventoryItem[_size.x, _size.y];

            CreateUI();
        }

        //TODO: Control this array from the ui
        private void CreateUI()
        {
            GameObject inventory = Resources.Load("UI/Inventory") as GameObject;
            inventory = Instantiate(inventory);
            inventory.transform.parent = FindObjectOfType<Canvas>().transform;
            inventory.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
            inventory.SetActive(false);
            _UIController = inventory.GetComponent<UI_GridInventoryController>();
        }

        public void ShowInventory()
        {
            _UIController.ShowInventory();
            ShowInventoryEvent?.Invoke(true);
        }

        public void HideInventory()
        {
            _UIController.HideInventory();
            ShowInventoryEvent?.Invoke(false);
        }

        private InventoryItem[,] _itemGrid;

        private bool HasItem(InventoryItem item)
        {
            for (int x = 0; x < _size.x; x++)
            {
                for (int y = 0; y < _size.y; y++)
                {
                    if (_itemGrid[x, y] != null)
                    {
                        if (_itemGrid[x, y] == item) return true;
                    }
                }
            }
            return false;
        }

        private bool AddItem(InventoryItem item)
        {
            return false;
        }

        public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY)
        {
            if (BoundaryCheck(posX, posY, inventoryItem.Width, inventoryItem.Height) == false)
            {
                return false;
            }

            if (OverlapCheck(posX, posY, inventoryItem.Width, inventoryItem.Height))
            {
                return false;
            }

            for (int x = 0; x < inventoryItem.Width; x++)
            {
                for (int y = 0; y < inventoryItem.Height; y++)
                {
                    _itemGrid[posX + x, posY + y] = inventoryItem;
                }
            }

            return true;
        }

        private Vector2Int? HasSpaceForItem(InventoryItem item)
        {
            int width = _size.x - item.Width + 1;
            int height = _size.y - item.Height + 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!OverlapCheck(x, y, item.Width, item.Height))
                    {
                        Debug.Log("Found Position");
                        return new Vector2Int?(new Vector2Int(x, y));
                    }
                }
            }
            Debug.Log("Inventory Full");
            return null;
        }

        private bool TakeItem(InventoryItem item)
        {
            return false;
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
                    if (_itemGrid[x + ix, y + iy] != null)
                        return true;
                }
            }
            return false;
        }

        private bool PositionCheck(int x, int y)
        {
            if (x < 0 || y < 0)
            {
                return false;
            }
            if (x >= _size.x || y >= _size.y)
            {
                return false;
            }
            return true;
        }
    }*/
}