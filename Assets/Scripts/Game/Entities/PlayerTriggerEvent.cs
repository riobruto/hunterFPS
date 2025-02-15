using Core.Engine;
using Game.Service;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts
{
    [RequireComponent(typeof(BoxCollider))]
    public class PlayerTriggerEvent : MonoBehaviour
    {
        private GameObject player;
        private BoxCollider _collider;
        public UnityEvent EnterEvent;
        public UnityEvent ExitEvent;

        private void Start()
        {
            if (PlayerService.Active) { player = Bootstrap.Resolve<PlayerService>().Player; }
            else PlayerService.PlayerSpawnEvent += (p) => player = p;
            _collider = GetComponent<BoxCollider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root == player.transform.root) { EnterEvent?.Invoke(); }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.root == player.transform.root) { ExitEvent?.Invoke(); }
        }

        private void OnDrawGizmos()
        {
            if (_collider == null) { _collider = GetComponent<BoxCollider>(); }
            Gizmos.color = new Color(1, .5f, 0, .5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(_collider.center, _collider.size);
        }
    }
}