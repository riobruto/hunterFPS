using Game.Entities;

using UnityEngine;

namespace Game.Objectives
{
    public class BreakableObjective : Objective
    {
        [SerializeField] private BreakableEntity[] _entitiesToBreak;  
        private Vector3 _centroid;
        public override Vector3 TargetPosition { get => _centroid; }

        public override void Run()
        {
            Status = ObjectiveStatus.ACTIVE;
            foreach (BreakableEntity entity in _entitiesToBreak)
            {
                _centroid += entity.transform.position;
                _centroid = _centroid / _entitiesToBreak.Length;
                entity.BreakEvent += Evaluate;
            }
        }

        private void Evaluate(BreakableEntity entity)
        {
            foreach (MonoBehaviour breakable in _entitiesToBreak)
            {
                if (!(breakable as BreakableEntity).Broken)
                {
                    Status = ObjectiveStatus.ACTIVE;
                    return;
                }
            }
            Status = ObjectiveStatus.COMPLETED;
        }

        public override void Create<T>(string name, T target, string description)
        {
         
            _entitiesToBreak = target as BreakableEntity[];
        }
    }
}