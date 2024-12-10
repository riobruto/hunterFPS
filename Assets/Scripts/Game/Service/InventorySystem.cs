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

        public event UnityAction<AmmunitionItem> GiveAmmoEvent;

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
            //LOAD AMMO FROM SAVE

            Grenades = new Dictionary<GrenadeType, int>();

            foreach (GrenadeType grenade in Enum.GetValues(typeof(GrenadeType)))
            {
                Grenades.Add(grenade, 3);
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

        internal bool TryGiveAmmo(AmmunitionItem type, int amount)
        {
            if (amount <= 0) return false;

            if (!Ammunitions.ContainsKey(type))
            {
                GiveAmmoEvent?.Invoke(type);
                UIService.CreateMessage($"Picked: <b>{amount}</b> rounds of <b>{type.Name}</b>");
                Ammunitions.Add(type, amount);
                return true;
            }

            int resultant = Mathf.Clamp(Ammunitions[type] + amount, 0, type.PlayerLimit);
            if (Mathf.Abs(Ammunitions[type] - resultant) <= 0)
            {
                UIService.CreateMessage($"<b>{type.Name}</b> full ");
                return false;
            }
            UIService.CreateMessage($"Picked: <b>{Mathf.Abs(Ammunitions[type] - resultant)}</b> rounds of <b>{type.Name}</b>");
            Ammunitions[type] = resultant;
            GiveAmmoEvent?.Invoke(type);
            return true;
        }

        internal int TryTakeAmmo(AmmunitionItem type, int desiredAmount)
        {
            if (desiredAmount <= 0) return 0;
            if (!Ammunitions.ContainsKey(type)) { return 0; }
            int resultant = Mathf.Clamp(Ammunitions[type], 0, desiredAmount);
            Ammunitions[type] -= resultant;
            return resultant;
        }
    }
}