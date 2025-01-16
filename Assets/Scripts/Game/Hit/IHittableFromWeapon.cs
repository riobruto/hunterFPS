using Game.Service;

namespace Game.Hit
{
    internal interface IHittableFromWeapon
    {
        void Hit(HitWeaponEventPayload payload);
    }
}