using UnityEngine;

namespace Player.Weapon.Interfaces
{
    internal interface IGrenade
    {
        void Trigger(int secondsRemaining);
        public Rigidbody Rigidbody { get; }
    }
}