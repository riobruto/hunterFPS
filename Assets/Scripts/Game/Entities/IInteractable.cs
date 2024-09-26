namespace Game.Entities
{
    public interface IInteractable
    {
        bool BeginInteraction();

        bool IsDone(bool cancelRequest);
    }
}