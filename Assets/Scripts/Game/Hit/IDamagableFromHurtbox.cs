
using UnityEngine;

namespace Game.Hit
{
    public interface IDamagableFromHurtbox
    {
        void NotifyDamage(float damage, Vector3 position, Vector3 direction);
    }
}