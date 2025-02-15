using Core.Engine;
using Core.Save;
using Core.Weapon;
using Game.Inventory;
using Game.Weapon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

namespace Game.Service
{
    public class InventoryService : SceneService
    {
        public static InventorySystem Instance;
        private static string _currentProfileName = "juan";

        internal override void Initialize() => PlayerService.PlayerSpawnEvent += OnPlayerSpawn;

        private void OnPlayerSpawn(GameObject player)
        {
            Instance = new GameObject("InventorySystem").AddComponent<InventorySystem>();
            Instance.Initialize();
            GameObject.DontDestroyOnLoad(Instance);
            //TODO: LOAD SAVED INVENTORY;
            LoadInventory();
        }

        public static void SaveInventory()
        {
            if (!PlayerService.Active) return;


            InventorySaveData data = new(GetLocationsForSaveables(Instance.Consumables));
            string json = JsonConvert.SerializeObject(data);
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Refaulter/SaveData";
            Directory.CreateDirectory(directory);

            string SaveName = "data";
            SaveName = directory + "/" + SaveName + ".json";

            Debug.Log(json);
            Debug.Break();
            try
            {
                File.AppendAllText(SaveName, json + "\n");
            }
            catch
            {
                Debug.LogError("Wont Save");
            }
        }

        private static string[] GetLocationsForSaveables(SaveableScriptableObject[] saveables)
        {
            string[] result = new string[saveables.Length];

            for (int i = 0; i < saveables.Length; i++)
            {
                if (saveables == null) continue;
                result[i] = saveables[i].name;
            }

            return result;
        }

        public static void LoadInventory()
        {
            //load json
        }

        public class InventorySaveData
        {
            public string[] ConsumbleAddresses;

            public InventorySaveData(string[] consumbleAddresses)
            {
                ConsumbleAddresses = consumbleAddresses;
            }
        }
    }

    public delegate void InventoryControllerDelegate(bool state);

    public delegate void InventoryConsumableDelegate(ConsumableItem item);

    public delegate void InventoryEquippableDelegate(EquipableItem item);

    public delegate void InventoryDropDelegate(InventoryItem item, GameObject gameObject);

    public delegate void InventoryAttachmentDelegate(AttachmentSettings item);

    public class InventorySystem : MonoBehaviour
    {
        public void SetInventoryFromSave()
        {
        }

        public void Initialize()
        {
            Ammunitions = new Dictionary<AmmunitionItem, int>();
            Grenades = new Dictionary<GrenadeType, int>();
            foreach (GrenadeType grenade in Enum.GetValues(typeof(GrenadeType)))
            {
                Grenades.Add(grenade, 3);
            }

            //GetSavedData
        }

        //save weapons, ammo, equipables, equipped, consumables
        //TODO: IMPLEMENTAR ADDRESABLES
        public event InventoryControllerDelegate ToggleInventoryEvent;

        public ConsumableItem[] Consumables = new ConsumableItem[8];
        public EquipableItem[] Equipables = new EquipableItem[8];
        public EquipableItem[] Equipped = new EquipableItem[3];
        public Dictionary<AmmunitionItem, int> Ammunitions;
        public Dictionary<GrenadeType, int> Grenades;
        public List<AttachmentSettings> CurrentAttachments => _attachments;
        [SerializeField] private AttachmentSettings _testAttach;

        [ContextMenu("ItemTest")]
        public void GiveItem()
        {
            TryGiveAttachment(_testAttach);
        }

        public int GrenadeLimitPerType = 3;

        public event UnityAction<InventoryItem> InventoryItemGiven;

        public event UnityAction<InventoryItem> InventoryItemRemoved;

        public event InventoryConsumableDelegate UseConsumableEvent;

        public event InventoryEquippableDelegate UseEquippableEvent;

        public event UnityAction<AmmunitionItem> GiveAmmoEvent;

        public event InventoryDropDelegate DropItem;

        public event InventoryAttachmentDelegate AttachmentAddedEvent;

        private List<AttachmentSettings> _attachments = new List<AttachmentSettings>();

        public bool TryAddItem(InventoryItem item)
        {
            if (!PlayerService.Active) return false;
            if (item is ConsumableItem)
            {
                for (int i = 0; i < Consumables.Length; i++)
                {
                    if (Consumables[i] == null)
                    {
                        Consumables[i] = item as ConsumableItem;
                        InventoryItemGiven?.Invoke(item);
                        UIService.CreateMessage($"Picked up {item.Name}");
                        return true;
                    }
                    continue;
                }
                UIService.CreateMessage("Your inventory is full");
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
                        UIService.CreateMessage($"Picked up {item.Name}");
                        return true;
                    }
                    continue;
                }
                UIService.CreateMessage("Your inventory is full");
                return false;
            }
            return false;
        }

        public bool TryRemoveItem(InventoryItem item)
        {
            if (!PlayerService.Active) return false;
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
        {
            Debug.Log("Tried Removed item");
            if (TryRemoveItem(item))
            {
                Debug.Log("Removed Item");

                Transform instancePoint = Bootstrap.Resolve<PlayerService>().PlayerCamera.transform;
                Ray ray = new(instancePoint.transform.position, instancePoint.forward);
                Physics.Raycast(ray, out RaycastHit hit, 2, 1, QueryTriggerInteraction.Ignore);
                GameObject droppedItem = Instantiate(item.Prefab, hit.point != Vector3.zero ? hit.point + hit.normal : instancePoint.position + instancePoint.forward, Quaternion.identity);
                DropItem?.Invoke(item, droppedItem);
            }
            //todo: add drop logic
        }

        internal void GiveGrenades(int amount, GrenadeType type)
        {
            if (!PlayerService.Active) return;
            Grenades[type] = amount - Grenades[type];
        }

        internal bool TryGiveAmmo(AmmunitionItem type, int amount)
        {
            if (!PlayerService.Active) return false;
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

        /// <summary>
        /// Takes a given amount of ammunition type, and returns the maximun amount possible clamped to the desired amount
        /// </summary>
        /// <param name="type">the type of ammunition</param>
        /// <param name="desiredAmount">The max amount desired</param>
        /// <returns></returns>
        internal int TakeAvaliableAmmo(AmmunitionItem type, int desiredAmount)
        {
            if (!PlayerService.Active) return 0;
            if (desiredAmount <= 0) return 0;
            if (!Ammunitions.ContainsKey(type)) { return 0; }
            int resultant = Mathf.Clamp(Ammunitions[type], 0, desiredAmount);
            Ammunitions[type] -= resultant;
            Debug.Log("TakingAmmo");
            return resultant;
        }

        public bool HasAmmoOfType(AmmunitionItem type) => Ammunitions.ContainsKey(type) && Ammunitions[type] > 0;

        public bool TryGiveAttachment(AttachmentSettings attachment)
        {
            if (!PlayerService.Active) return false;
            if (_attachments.Contains(attachment)) return false;
            _attachments.Add(attachment);
            AttachmentAddedEvent?.Invoke(attachment);
            return true;
        }

        public bool HasAttachment(AttachmentSettings attachment) => _attachments.Contains(attachment);
    }
}