using Game.Service;
using Nomnom.RaycastVisualization;
using System.Collections;
using UnityEngine;

namespace Life.Controllers
{
  
    public class DroneAgentController : AgentController
    {
      
       

        public override void OnStart()
        {           
           
            NavMeshAgent.SetDestination(Vector3.zero);
        }

        public override void OnUpdate()
        {
            if (PlayerService.Active) { NavMeshAgent.SetDestination(PlayerHeadPosition); }
           
        }

        public override void UpdateMovement()
        {
        }

        private void FixedUpdate()
        {
           
        }

       
    }
}