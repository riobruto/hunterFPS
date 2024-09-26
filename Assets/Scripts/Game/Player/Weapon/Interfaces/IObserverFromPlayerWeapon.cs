using Game.Player.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Player.Weapon
{
    internal interface IObserverFromPlayerWeapon
    {
        void Initalize(PlayerWeapons controller);

        void Detach(PlayerWeapons controller);
    }
}