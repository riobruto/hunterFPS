

namespace Game.Player.Movement
{
    internal interface IObserverFromPlayerMovement
    {
        void Initalize(PlayerRigidbodyMovement controller);

        void Detach(PlayerRigidbodyMovement controller);
    }
}