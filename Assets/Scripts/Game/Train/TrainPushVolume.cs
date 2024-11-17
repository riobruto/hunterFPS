using Core.Engine;
using Game.Player;
using Game.Player.Controllers;
using Game.Service;
using UnityEngine;

namespace Game.Train
{
    public class TrainPushVolume : MonoBehaviour
    {
        private GameObject _player;
        [SerializeField] private Collider _boxCollider;
        private Rigidbody _rb;

        private void Start()
        {
            _player = Bootstrap.Resolve<PlayerService>().Player;
            _rb = GetComponentInParent<Rigidbody>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject != _player) return;
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject != _player) return;

            if (_rb.velocity.magnitude > 2.5f)
            {
                _player.GetComponent<PlayerHealth>().Hurt(_rb.velocity.magnitude);
            }
            Vector3 pushPos = _boxCollider.ClosestPointOnBounds(_player.transform.position) - transform.position;
            Debug.DrawRay(transform.position, pushPos);
            _player.GetComponent<PlayerMovementController>().Teletransport(_player.transform.position + pushPos.normalized * Time.deltaTime*2f);
        }
    }
}