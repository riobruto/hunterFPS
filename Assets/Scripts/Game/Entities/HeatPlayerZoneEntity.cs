using Game.Player.Controllers;
using System.Collections;
using UnityEngine;

namespace Game.Entities
{
    public class HeatPlayerZoneEntity : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("eNTER");
            if (other.gameObject.layer == 3)
            {
                other.transform.root.GetComponent<PlayerCold>().SetFrostState(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log("eXIT");
            if (other.gameObject.layer == 3)
            {
                other.transform.root.GetComponent<PlayerCold>().SetFrostState(true);
            }
        }
    }
}