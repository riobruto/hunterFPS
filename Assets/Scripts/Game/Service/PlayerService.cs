using Core.Engine;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Service
{
    public class PlayerService : SceneService
    {
        private GameObject _player;
        private SpawnController _controller;
        private Camera _playerCamera;
        public GameObject Player => _player;
        public Camera PlayerCamera => _playerCamera;

        internal override void Initialize()
        {//Deativate.
            _controller = GameObject.FindObjectOfType<SpawnController>();
            if (_controller == null)
            {
                Debug.LogError("Theres no Spawn Controller in the currentScene");
                return;
            }

            SpawnPlayer();
        }

        private void SpawnPlayer()
        {
            Transform spawnTransform = _controller.SpawnPoints[Random.Range(0, _controller.SpawnPoints.Length)];
            _player = GameObject.Instantiate(Resources.Load<GameObject>("Player"));
            _player.GetComponent<Rigidbody>().position = spawnTransform.position;
            _player.GetComponent<Rigidbody>().rotation = spawnTransform.rotation;
            _playerCamera = _player.GetComponentInChildren<Camera>();
            Debug.Log("Player Spawned Succesfully");
            GameObject.DontDestroyOnLoad(_player);
        }
    }
}