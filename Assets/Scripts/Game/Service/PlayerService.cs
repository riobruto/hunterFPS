using Core.Engine;
using Game.Player;
using Game.Player.Controllers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Service
{
    public delegate void PlayerSpawnDelegate(GameObject player);

    public class PlayerService : SceneService
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
                return;
            }

            //check if input is needed;
            //SpawnPlayer();
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        public void SpawnPlayer()
        {
            Transform spawnTransform = _controller.SpawnPoints[Random.Range(0, _controller.SpawnPoints.Length)];
            _player = GameObject.Instantiate(Resources.Load<GameObject>("Player"));
            _player.GetComponent<Rigidbody>().position = spawnTransform.position;
            _player.GetComponent<Rigidbody>().rotation = spawnTransform.rotation;
            _playerCamera = _player.GetComponentInChildren<Camera>();
            Debug.Log("Player Spawned Succesfully");
            GameObject.DontDestroyOnLoad(_player);
            PlayerSpawnEvent(_player);
            Active = true;
        }

        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            //we unset the spawn
            _lastRespawn = null;
            _controller = GameObject.FindObjectOfType<PlayerSpawnEntity>();
            GetPlayerComponent<PlayerRigidbodyMovement>().Teletransport(_controller.SpawnPoints[Random.Range(0, _controller.SpawnPoints.Length)].position);
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
            SceneManager.LoadScene(SceneManager.GetSceneAt(0).name);      

            if (_lastRespawn != null)
            {
                //respawn Sequence?
                //todo: ojito con este bastante bastante
                //reset Components
                GetPlayerComponent<PlayerManager>().RestorePlayer();                //teleport
                GetPlayerComponent<PlayerRigidbodyMovement>().Teletransport(_lastRespawn.RespawnPosition);
                return;
            }

            GetPlayerComponent<PlayerManager>().RestorePlayer();
            GetPlayerComponent<PlayerRigidbodyMovement>().Teletransport(_controller.SpawnPoints[Random.Range(0, _controller.SpawnPoints.Length)].position);
        }
    }
}