using Core.Engine;
using Core.Weapon;
using Game.Hit;
using Game.Impact;
using Game.Service;
using Nomnom.RaycastVisualization.Shapes;
using System;
using UnityEngine;

namespace Game.Entities
{
    public class PhysicalSurface : MonoBehaviour, IHittableFromWeapon
    {

        [SerializeField] SurfaceType _type;

        public SurfaceType Type { get => _type; }

        void IHittableFromWeapon.Hit(HitWeaponEventPayload payload)
        {

            //FIX: CUANDO LA ESCENA SE RECARGA, ALGUNOS IMPACTS SON DESTRUIDOS Y SUS REFERENCIAS SON NULAS

            Bootstrap.Resolve<ImpactService>().System.ImpactAtPosition(payload.RaycastHit.point, payload.RaycastHit.normal, transform, _type);

            switch (_type)
            {
               
                case SurfaceType.CARTBOARD:  
                case SurfaceType.PAPER:
                case SurfaceType.METAL_SOFT:
                case SurfaceType.WOOD:                   
                case SurfaceType.RUBBER:
                    ManagePenetration(payload);
                    break;      
            }


            if (gameObject.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForceAtPosition(-payload.RaycastHit.normal.normalized, payload.RaycastHit.point, ForceMode.Impulse);
            }
        }

        private void ManagePenetration(HitWeaponEventPayload payload)
        {
                    //todo: allow certain materials to continue the cast, or crear recast hit luego del impacto, copiando el owner payload. algo asi. qsy
            
        }
    }
}