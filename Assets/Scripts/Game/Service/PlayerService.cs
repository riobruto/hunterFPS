using Core.Engine;
using Game.Player;
using Game.Player.Controllers;
using Game.UI.MainMenu;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using static UnityEditor.Experimental.GraphView.GraphView;
using Random = UnityEngine.Random;

namespace Game.Service
{
    public delegate void PlayerSpawnDelegate(GameObject player);

    public class PlayerService : GameGlobalService
    {
        private GameObject _player;
        private PlayerSpawnEntity _controller;
        private Camera _playerCamera;
        public GameObject Player => _player;
        public Camera PlayerCamera => _playerCamera;

        //profile stuff

        public T GetPlayerComponent<T>() => Player.GetComponent<T>();

        public static event PlayerSpawnDelegate PlayerSpawnEvent;

        internal override void Initialize()
        {
            //Deativate.
            _controller = GameObject.FindObjectOfType<PlayerSpawnEntity>();
            if (_controller == null)
            {
                Debug.LogError("Theres no Spawn Controller in the currentScene");
            }

            //check if input is needed;
            //SpawnPlayer();
         
            //Spawn the player if it does not exist
            SceneInitiator.OnSceneLoaded += OnSceneLoaded;
            SceneInitiator.OnSceneReloaded += OnSceneReloaded;
        }

        private void OnSceneReloaded()
        {
            _controller = GameObject.FindObjectOfType<PlayerSpawnEntity>();

            if (_lastRespawn != null)
            {
                //respawn Sequence?
                //todo: ojito con este bastante bastante
                //reset Components
                GetPlayerComponent<PlayerManager>().RestorePlayer();                //teleport
                GetPlayerComponent<PlayerRigidbodyMovement>().Teleport(_lastRespawn.RespawnPosition);
                return;
            }

            GetPlayerComponent<PlayerManager>().RestorePlayer();
            GetPlayerComponent<PlayerRigidbodyMovement>().Teleport(_controller.SpawnPoints[Random.Range(0, _controller.SpawnPoints.Length)].position);
        }

        private void OnSceneLoaded()
        {
            if (!Active)
            {
                SpawnPlayer();
                Debug.Log("Spawn Player for the firts scene!");
            }            

            //_lastRespawn = null;
            _controller = GameObject.FindObjectOfType<PlayerSpawnEntity>(true);

            if (Active) GetPlayerComponent<PlayerRigidbodyMovement>().Teleport(_controller.SpawnPoints[Random.Range(0, _controller.SpawnPoints.Length)].position);
        }

        public void SpawnPlayer()
        {
            _controller = GameObject.FindObjectOfType<PlayerSpawnEntity>(true);
            Transform spawnTransform = _controller.SpawnPoints[Random.Range(0, _controller.SpawnPoints.Length)];
            //TODO: GET PLAYER FROM ADDRESSABLES!          

            Addressables.InstantiateAsync("Assets/Prefabs/Player.prefab").Completed += (x) =>
            {
                _player = x.Result;
                _player.GetComponent<Rigidbody>().position = spawnTransform.position;
                _player.GetComponent<Rigidbody>().rotation = spawnTransform.rotation;
                _playerCamera = _player.GetComponentInChildren<Camera>();
                Debug.Log("Player Spawned Succesfully");
                GameObject.DontDestroyOnLoad(_player);
                PlayerSpawnEvent(_player);
                Active = true;
            };


          
        }

      

        private static RespawnEntity _lastRespawn;
        public static RespawnEntity LastRespawn => _lastRespawn;

        public static bool Active { get; private set; }

        internal static void SetLastRespawn(RespawnEntity respawnEntity)
        {
            _lastRespawn = respawnEntity;
        }

        public void Respawn()
        {
            Bootstrap.Resolve<SceneInitiator>().ReloadCurrentScene();
        }

        public void SavePlayerWeaponState()
        {
            WeaponSaveData data = WeaponSaveData.CreateWeaponSaveData(GetPlayerComponent<PlayerWeapons>().WeaponSlots.Values.ToArray());

            string json = JsonConvert.SerializeObject(data);
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Refaulter/SaveData";
            Directory.CreateDirectory(directory);

            string SaveName = "weapondata";
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
    }

    public class WeaponSaveSlot
    {
        public WeaponSaveInstance[] WeaponSaveInstances;

        public WeaponSaveSlot(WeaponSaveInstance[] weaponSaveInstances)
        {
            WeaponSaveInstances = weaponSaveInstances;
        }
    }

    public class WeaponSaveInstance
    {
        public int currentAmmo;
        public string name;

        public WeaponSaveInstance(int currentAmmo, string name)
        {
            this.currentAmmo = currentAmmo;
            this.name = name;
        }
    }

    public class WeaponSaveData
    {
        public WeaponSaveSlot[] WeaponSlots;

        public WeaponSaveData(WeaponSaveSlot[] slots)
        {
            WeaponSlots = slots;
        }

        public static WeaponSaveData CreateWeaponSaveData(PlayerWeaponSlot[] slots)
        {
            WeaponSaveSlot[] data = new WeaponSaveSlot[slots.Length];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = CreateSaveSlotFromInstance(slots[i]);
            }

            return new WeaponSaveData(data);
        }

        private static WeaponSaveSlot CreateSaveSlotFromInstance(PlayerWeaponSlot slot)
        {
            List<WeaponSaveInstance> saveInstances = new List<WeaponSaveInstance>();

            foreach (PlayerWeaponInstance instance in slot.WeaponInstances)
            {
                WeaponSaveInstance saveInstance = new WeaponSaveInstance(instance.CurrentAmmo, instance.Settings.name);
                saveInstances.Add(saveInstance);
            }
            WeaponSaveSlot saveSlot = new WeaponSaveSlot(saveInstances.ToArray());
            return saveSlot;
        }
    }
}