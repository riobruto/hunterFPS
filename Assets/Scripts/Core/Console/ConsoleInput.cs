using Core.Engine;
using Core.Weapon;
using Game.Inventory;
using Game.Player;
using Game.Player.Controllers;
using Game.Service;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using UnityEngine.ResourceManagement.ResourceLocations;
#if UNITY_EDITOR
namespace Core.Console { 


    public class ConsoleService : SceneService
    {
        public ConsoleInput Input;

        internal override void Initialize()

        {
            GameObject go = new GameObject("Console");
            Input = go.AddComponent<ConsoleInput>();
            GameObject.DontDestroyOnLoad(Input);
        }
    }

    public class ConsoleInput : MonoBehaviour
    {

            


        private bool _consoleOpen;

        private void Start()
        {
            Debug.Log("HI, CONSOLE STARTED!");
        }


        private void Update()
        {
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                _consoleOpen = !_consoleOpen;
            }
        }

        private bool _giveItemMenu;
        private bool _giveWeaponMenu;
        private bool _giveAttachmentMenu;
        private List<AttachmentSettings> _catchedAttachmentSettings = new List<AttachmentSettings>();
        private List<InventoryItem> _cachedSearch = new List<InventoryItem>();
        private List<WeaponSettings> _cachedWeaponSearch = new List<WeaponSettings>();
        private bool _inmune;
        private bool _disabledAI;
        private bool _ignorePlayer;

        private bool _playerSpawned => PlayerService.Active;
        private void OnGUI()
        {

            if (!_consoleOpen) return;



            using (new GUILayout.VerticalScope())
            {

                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope())
                    {

                        if (!_playerSpawned && GUILayout.Button("Spawn Player"))
                        {
                            Bootstrap.Resolve<PlayerService>().SpawnPlayer();
                        }
                        if (_playerSpawned)
                        {
                            if (GUILayout.Button("Save Player"))
                            {
                                InventoryService.SaveInventory();

                            }
                            if (GUILayout.Button("Load Player"))
                            {
                                InventoryService.LoadInventory();


                            }
                        }
                    }
                    if (_playerSpawned)
                    {
                        if (GUILayout.Button("Give Item"))
                        {
                            //TODO: IMPLEMENTAR ADDRESABLES 
                            _giveItemMenu = !_giveItemMenu;
                            if (_cachedSearch.Count == 0) _cachedSearch = LoadItemsOfTypeByLabel<InventoryItem>("Consumables");

                        }

                        if (GUILayout.Button("Give Weapon"))
                        {
                            _giveWeaponMenu = !_giveWeaponMenu;
                            if (_cachedWeaponSearch.Count == 0) _cachedWeaponSearch = LoadItemsOfTypeByLabel<WeaponSettings>("Weapons");
                        }
                        if (GUILayout.Button("Give Attachment"))
                        {
                            _giveAttachmentMenu = !_giveAttachmentMenu;
                            _catchedAttachmentSettings = FindAll<AttachmentSettings>("Assets/Resources/WeaponSettings");
                        }

                        if (GUILayout.Button("Restore Player"))
                        {
                            Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerHealth>().Heal(100f);
                            Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerRigidbodyMovement>().Stamina = 100;
                        }
                        if (GUILayout.Button("Give Current Ammo"))
                        {
                            InventoryService.Instance.Ammunitions[FindObjectOfType<PlayerWeapons>().WeaponEngine.WeaponSettings.Ammo.Type] = 9999;
                        }

                        if (GUILayout.Button("Toggle Player Inmunnity"))
                        {
                            _inmune = !_inmune;
                            Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerHealth>().SetInmunity(_inmune);

                        }
                        if (GUILayout.Button(_disabledAI ? "Enable AI" : "Disable AI"))
                        {
                            _disabledAI = !_disabledAI;
                            AgentGlobalService.SetDisableAI(_disabledAI);

                        }

                        if (GUILayout.Button($"Ignore Player: {_ignorePlayer}"))
                        {
                            _ignorePlayer = !_ignorePlayer;
                            AgentGlobalService.SetIgnorePlayer(_ignorePlayer);

                        }

                        if (_giveWeaponMenu)
                        {
                            using (new GUILayout.VerticalScope())
                            {
                                foreach (WeaponSettings weapon in _cachedWeaponSearch)
                                {
                                    if (GUILayout.Button(weapon.name))
                                    {
                                        InventoryService.Instance.TryGiveAmmo(weapon.Ammo.Type, weapon.Ammo.Type.PlayerLimit);
                                        FindObjectOfType<PlayerWeapons>().TryGiveWeapon(weapon, weapon.Ammo.Size);
                                    }
                                }
                            }
                        }
                        if (_giveAttachmentMenu)
                        {
                            using (new GUILayout.VerticalScope())
                            {
                                foreach (AttachmentSettings attachment in _catchedAttachmentSettings)
                                {
                                    if (GUILayout.Button(attachment.name))
                                    {
                                        InventoryService.Instance.TryGiveAttachment(attachment);
                                    }
                                }
                            }
                        }
                        if (_giveItemMenu)
                        {
                            using (new GUILayout.VerticalScope())
                            {
                                foreach (InventoryItem item in _cachedSearch)
                                {
                                    if (GUILayout.Button(item.name))
                                    {
                                        InventoryService.Instance.TryAddItem(item);
                                    }
                                }
                            }
                        }
                    }
                    
                   
                }
            }
        }

        private void OnLoad(AsyncOperationHandle<IList<IResourceLocation>> handle)
        {
           

        }

        private List<T> LoadItemsOfTypeByLabel<T>(string label)
        {
            List<T> list = new List<T>();
            Addressables.LoadResourceLocationsAsync(new AssetLabelReference().labelString = label).Completed += (AsyncOperationHandle<IList<IResourceLocation>> handle) => 
            {
                foreach (IResourceLocation loc in handle.Result)
                {
                    Addressables.LoadAssetAsync<T>(loc).Completed += (type) =>
                    {
                        list.Add(type.Result);
                    };
                }
            };
            return list;
        }



        public static List<T> FindAll<T>(string path = "") where T : ScriptableObject
        {
            // use adressables
            //TODO: IMPLEMENTAR ADDRESABLES 


            var scripts = new List<T>();
            var searchFilter = $"t:{typeof(T).Name}"; 
            var soNames = path == ""
            ? AssetDatabase.FindAssets(searchFilter) :
            AssetDatabase.FindAssets(searchFilter, new[] { path });
            foreach (var soName in soNames)
            {
                var soPath = AssetDatabase.GUIDToAssetPath(soName);
                var script =
                AssetDatabase.LoadAssetAtPath<T>(soPath);
                if (script == null)
                    continue;
                scripts.Add(script);
            }
            return scripts;
        }

    }    

}
#endif