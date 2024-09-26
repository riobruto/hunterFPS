using Game.Service;

namespace Game.Hit
{
    internal interface IHittableFromWeapon
    {
        void OnHit(HitWeaponEventPayload payload);
    }
}