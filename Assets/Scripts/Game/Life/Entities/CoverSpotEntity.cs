using Core.Engine;
using Game.Service;
using Life.Controllers;

using UnityEngine;

namespace Game.Life.Entities
{
    public class CoverSpotEntity : MonoBehaviour
    {
        public bool Taken => CurrentSoldier != null;
        public SoldierAgentController CurrentSoldier { get; private set; }

        public bool TryTake(SoldierAgentController controller, Vector3 threat)
        {
            bool dot = Vector3.Dot(transform.forward, threat - transform.position) > .8f;
            bool maxThreatDistance = Vector3.Distance(transform.position, threat) > 20f;
            bool minThreatDistance = Vector3.Distance(transform.position, threat) < 1;

            if (maxThreatDistance) return false;
            if (minThreatDistance) return false;
            if (!dot) return false;
            if (Taken) return false;

            CurrentSoldier = controller;
            return true;
        }

        public bool IsValid(SoldierAgentController controller, Vector3 threat)
        {
            bool dot = Vector3.Dot(transform.forward, threat - transform.position) > .8f;
            bool maxThreatDistance = Vector3.Distance(transform.position, threat) > 20f;
            bool minThreatDistance = Vector3.Distance(transform.position, threat) < 1;

            if (maxThreatDistance) return false;
            if (minThreatDistance) return false;
            if (!dot) return false;
            if (Taken) return false;
            return true;
        }

        public bool HasVisibility(Vector3 threat)
        {
            return !Physics.Linecast(threat, transform.position + Vector3.up * 1.75f, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers);
        }

        private void Start()
        {
            AgentGlobalService.Instance.RegisterCoverSpot(this);
            //REGISTER
        }

        public void Release(SoldierAgentController controller)
        {
            CurrentSoldier = null;
        }

        //la gracia de esto es poder planear los enfrentamientos desde el editor.
        //si alguno de estos puntos se compromete tendran la opcion de romper filas

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + transform.forward + transform.up, .25f);
            Gizmos.color = Taken ? new Color(1, 1, 1, .33f) : new Color(1, 0, 1, .33f);

            Gizmos.DrawCube(transform.position + transform.up, Vector3.one + Vector3.up);
        }

        private void OnDestroy()
        {
            AgentGlobalService.Instance.UnregisterCoverSpot(this);
            //UNREGISTER
        }
    }
}