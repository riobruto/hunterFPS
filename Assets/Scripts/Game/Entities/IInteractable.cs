using UnityEngine;

namespace Game.Entities
{
    public interface IInteractable
    {
        bool BeginInteraction(Vector3 position);

        bool IsDone(bool cancelRequest);

        bool CanInteract();
    }
}