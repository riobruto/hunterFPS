using Life.Controllers;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Objectives
{
    public class MurderObjective : Objective
    {
        [SerializeField]
        private AgentController[] _targets;


        private Vector3 _centroid;


        public override Vector3 TargetPosition => _centroid;

        public override void OnCompleted()
        {
        }

        public override void OnFailed()
        {
        }

        public override void Create<T>(string name, T target, string description)
        {           
            _targets = target as AgentController[];
        }

        public override void Run()
        {
            foreach (AgentController controller in _targets)
            {
                controller.DeadEvent += OnDead; 
                _centroid += controller.transform.position;
                _centroid /= _targets.Length;
            }
        }

        private void OnDead(AgentController arg0)
        {
            throw new NotImplementedException();
        }

        public override void OnUpdated()
        {
        }
    }
}