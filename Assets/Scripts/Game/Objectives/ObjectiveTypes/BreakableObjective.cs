using Game.Entities;
using Game.Service;
using UnityEngine;

namespace Game.Objectives
{
    public class BreakableObjective : Objective
    {
        [SerializeField] private BreakableEntity[] _entitiesToBreak;
        private bool _completed;
        private string _name;
        private string _description;

        public override event ObjectiveCompleted CompletedEvent;

        public override event ObjectiveFailed FailedEvent;

        public override bool IsCompleted { get => _completed; }

        public override string TaskName => _name;

        public override string TaskDescription => _description;

        public override void Run()
        {
            foreach (BreakableEntity entity in _entitiesToBreak)
            {
                entity.BreakEvent += Evaluate;
            }

            _completed = false;
        }

        private void Evaluate(BreakableEntity entity)
        {
            foreach (MonoBehaviour breakable in _entitiesToBreak)
            {
                if (!(breakable as BreakableEntity).Broken)
                {
                    _completed = false;
                    return;
                }
            }
            OnCompleted();
            _completed = true;
        }

        public override void OnCompleted()
        {
            CompletedEvent?.Invoke(this);

            
        }

        public override void OnFailed()
        {
            
        }


        public override void Create<T>(string name, T target, string description)
        {
            _name = name;
            _description = description;
            _entitiesToBreak = target as BreakableEntity[];
        }
    }
}