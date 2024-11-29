using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Hit
{
    public interface IDamageableFromExplosive
    {
        //TODO: CREAR PAYLOAD PARA ESTA GARCHA(DIRECCION, TIPO, OWNER, ETC)
        void NotifyDamage(float damage);
    }
}
