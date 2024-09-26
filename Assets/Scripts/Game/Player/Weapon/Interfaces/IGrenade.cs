using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Player.Weapon.Interfaces
{
    internal interface IGrenade
    {
        void Trigger(int secondsRemaining);
    }
}
