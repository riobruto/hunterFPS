using Game.Entities;
using Game.Service;
using System.Collections;
using UnityEngine;

namespace Game.Player
{
    public class RespawnEntity : SimpleInteractable
    {

        public override event InteractableDelegate InteractEvent;

        public Vector3 RespawnPosition => _spawnLocalPosition + transform.position;

        public override bool CanInteract => PlayerService.LastRespawn != this;

        public override bool Taken => false;
        
        [SerializeField] private Vector3 _spawnLocalPosition;

        private void Start()
        {
        }

        private void LateUpdate()
        {
            //ANIMATION
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(_spawnLocalPosition, Vector3.one);
        }

        public override bool Interact()
        {
            if (!CanInteract) return false;
            UIService.CreateMessage("You will now respawn here");
            PlayerService.SetLastRespawn(this);
            return true;
        }
    }
}