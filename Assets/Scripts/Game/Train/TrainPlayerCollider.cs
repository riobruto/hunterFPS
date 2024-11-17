using Core.Engine;
using Game.Service;
using System.Collections;
using UnityEngine;

namespace Game.Train
{
    public class TrainPlayerCollider : MonoBehaviour
    {
        private GameObject _player;
        private TrainBase _base;

        private void Start()
        {
            _base = GetComponent<TrainBase>();
            _player = Bootstrap.Resolve<PlayerService>().Player;
        }

        private void OnTriggerEnter(Collider other)
        {
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject == _player)
            {
                Debug.Log("PlayerInCollider");
            }
        }

        private void OnTriggerExit(Collider other)
        {
        }
    }
}