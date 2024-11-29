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

#if UNITY_EDITOR
namespace Core.Console
{

    public class ConsoleService : SceneService
    {
        public ConsoleInput Input;

        internal override void Initialize()

        {
            GameObject go = new GameObject("Console");
            Input = go.AddComponent<ConsoleInput>();
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
            if (Keyboard.current.f10Key.isPressed)
            {
                _consoleOpen = !_consoleOpen;
            }
        }

        private bool _giveItemMenu;
        private bool _giveWeaponMenu;
        private List<InventoryItem> _cachedSearch;
        private List<WeaponSettings> _cachedWeaponSearch;

        private void OnGUI()
        {

            if (!_consoleOpen) return;
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Give Item"))
                {
                    _giveItemMenu = !_giveItemMenu;
                    _cachedSearch = FindAll<InventoryItem>("Assets/Resources");
                }

                if (GUILayout.Button("Give Weapon"))
                {
                    _giveWeaponMenu = !_giveWeaponMenu;
                    _cachedWeaponSearch = FindAll<WeaponSettings>("Assets/Resources");
                }

                if (GUILayout.Button("Restore Player"))
                {
                    Bootstrap.Resolve<PlayerService>().Player.GetComponent<PlayerHealth>().Heal(100f);
                    Bootstrap.Resolve<PlayerService>().Player.GetComponent<PlayerRigidbodyMovement>().Stamina = 100;
                }
                if (GUILayout.Button("Give Current Ammo"))
                {
                    Bootstrap.Resolve<InventoryService>().Instance.Ammunitions[FindObjectOfType<PlayerWeapons>().WeaponEngine.WeaponSettings.Ammo.Type] = 9999;
                }
                if (_giveWeaponMenu)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        foreach (WeaponSettings weapon in _cachedWeaponSearch)
                        {
                            if (GUILayout.Button(weapon.name))
                            {
                                FindObjectOfType<PlayerWeapons>().TryGiveWeapon(weapon, weapon.Ammo.Size);
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
                                Bootstrap.Resolve<InventoryService>().Instance.TryAddItem(item);
                            }
                        }
                    }
                }
            }
        }

        public static List<T> FindAll<T>(string path = "") where T : ScriptableObject
        {
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