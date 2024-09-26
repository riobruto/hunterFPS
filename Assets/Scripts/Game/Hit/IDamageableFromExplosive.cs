using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Hit
{
    public interface IDamageableFromExplosive
    {
        void NotifyDamage(float damage);
    }
}
