using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Hit
{
    internal interface IDamagableForHurtbox
    {
        void NotifyDamage(int damage);

    }
}
