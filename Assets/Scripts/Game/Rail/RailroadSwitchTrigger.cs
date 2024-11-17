using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Rail
{
    public class RailroadSwitchTrigger : MonoBehaviour
    {
         public UnityEvent<bool> TrainEnteredEvent;
        [SerializeField] bool _switchState; 
        private void OnTriggerEnter(Collider other)
        {
            TrainEnteredEvent?.Invoke(_switchState);
        }
    }
}