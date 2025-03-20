using Core.Engine;
using Game.Service;
using UnityEngine;
using UnityEngine.Events;

namespace Entities
{
    [RequireComponent(typeof(BoxCollider))]
    public class PlayerTriggerEvent : MonoBehaviour
    {
        private GameObject _player;
        private BoxCollider _collider;
        public UnityEvent EnterEvent;
        public UnityEvent ExitEvent;

        private void Start()
        {
            if (PlayerService.Active) { _player = Bootstrap.Resolve<PlayerService>().Player; }
            else PlayerService.PlayerSpawnEvent += OnPlayerSpawn;
            _collider = GetComponent<BoxCollider>();
        }

        private void OnValidate()
        {  
            if(_collider ==  null) _collider = GetComponent<BoxCollider>();
            _collider.isTrigger = true;
        }

        private void OnPlayerSpawn(GameObject player)
        {
            _player = player;
            PlayerService.PlayerSpawnEvent -= OnPlayerSpawn;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_player == null) return;
            if (other.transform.root == _player.transform.root) { EnterEvent?.Invoke(); }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_player == null) return;
            if (other.transform.root == _player.transform.root) { ExitEvent?.Invoke(); }
        }

        private void OnDrawGizmos()
        {
            if (_collider == null) { _collider = GetComponent<BoxCollider>(); }
            Gizmos.color = new Color(1, .5f, 0, .5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(_collider.center, _collider.size);
        }

        private void OnDestroy()
        {
            PlayerService.PlayerSpawnEvent -= OnPlayerSpawn;

        }
    }
}