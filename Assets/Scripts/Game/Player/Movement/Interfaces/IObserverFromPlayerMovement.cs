

namespace Game.Player.Movement
{
    internal interface IObserverFromPlayerMovement
    {
        void Initalize(PlayerMovementController controller);

        void Detach(PlayerMovementController controller);
    }
}