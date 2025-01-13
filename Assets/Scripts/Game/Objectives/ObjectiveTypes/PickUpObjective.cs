using Game.Entities;
using UnityEngine;

namespace Game.Objectives
{
    public class PickUpObjective : Objective
    {
        [SerializeField] private SimpleInteractable[] _targets;

        private Vector3 _centroid;

        public override Vector3 TargetPosition => _centroid;

        private void Evaluate()
        {
        }

        public override void Run()
        {
            Status = ObjectiveStatus.ACTIVE;
            foreach (var target in _targets)
            {
                _centroid += target.transform.position;
                _centroid /= _targets.Length;
                target.InteractEvent += Evaluate;
            }
        }

        public override void Create<T>(string name, T target, string description)
        {
           
            _targets = target as SimpleInteractable[];
        }
    }
}