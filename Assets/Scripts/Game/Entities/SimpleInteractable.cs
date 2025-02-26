using UnityEngine;

namespace Game.Entities
{
    public delegate void InteractableDelegate();

    public abstract class SimpleInteractable : MonoBehaviour, IInteractable
    {
        public abstract event InteractableDelegate InteractEvent;

        public abstract bool CanInteract { get; }
        public abstract bool Taken { get; }
        public abstract bool Interact();
        private void Start()
        {
            gameObject.layer = 30;
        }
        bool IInteractable.BeginInteraction(Vector3 position)
        {
            return Interact();
        }
        bool IInteractable.CanInteract() => CanInteract;

        bool IInteractable.IsDone(bool cancelRequest)
        {
            return true;
        }
    }
}