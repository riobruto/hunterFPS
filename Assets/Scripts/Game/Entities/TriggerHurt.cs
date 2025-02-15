using Core.Engine;
using Game.Player.Controllers;
using Game.Service;
using System.Collections;
using UnityEngine;

namespace Game.Entities
{
    public class TriggerHurt : MonoBehaviour
    {
        private GameObject player;
        private float _lastTimeHurt;

        private void OnTriggerEnter(Collider other)
        {
            if (player == null)
            {
                player = Bootstrap.Resolve<PlayerService>().Player;
            }
            if (player == null)
            {
                return;
            }

            if (other.gameObject == player)
            {
                player.GetComponent<PlayerHealth>().Hurt(10f, transform.position);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject == player)
            {
                if (Time.time - _lastTimeHurt > 1)
                {
                    _lastTimeHurt = Time.time;
                    player.GetComponent<PlayerHealth>().Hurt(10f, transform.position);
                }
            }
        }
    }
}