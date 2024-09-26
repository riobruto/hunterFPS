using Core.Weapon;
using System;
using UnityEngine;

namespace Game.Player.Weapon
{
    public class WeaponStateEventArgs : EventArgs
    {
        public WeaponState State;
        public IWeapon Sender;

        public WeaponStateEventArgs(WeaponState state, IWeapon sender)
        {
            State = state;
            Sender = sender;
        }
    }

    public interface IWeapon
    {
        event EventHandler<WeaponStateEventArgs> WeaponChangedState;

        event EventHandler<bool> WeaponActivatedState;
        bool IsOwnedByPlayer { get; set; }

        int CurrentAmmo { get; }
        bool Active { get; }
        bool Cocked { get; }
        bool Empty { get; }
        bool Initialized { get; }
        bool IsReloading { get; }
        bool IsShooting { get; }
        bool BoltOpen { get; }
        Vector2 RayNoise { get; set; }


        WeaponSettings WeaponSettings { get; }

        Ray Ray { get; }
        int MaxAmmo { get; }

        void Initialize(WeaponSettings settings, int currentAmmo, bool cocked,bool playerIsOwner);

        bool Fire();

        void Reload(int amount);

        void OpenBolt();

        void CloseBolt();

        bool Insert();

        void Activate();

        void Deactivate();

        void ReleaseFire();

        void SetMovementDelta(Vector2 v);

        void SetHitScanMask(LayerMask mask);
    }
}