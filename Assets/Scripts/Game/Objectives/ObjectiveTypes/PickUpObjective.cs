using Game.Entities;
using Game.Service;
using System.Linq;

namespace Game.Objectives
{
    public class PickUpObjective : Objective
    {
        private SimpleInteractable[] _targets;
        private string _name;
        private string _description;

        public override bool IsCompleted => _targets.All(x => x.Taken);

        public override string TaskName => _name;
        public override string TaskDescription => _description;

        public override event ObjectiveCompleted CompletedEvent;

        public override event ObjectiveFailed FailedEvent;

        public override void OnCompleted()
        {
          
            CompletedEvent?.Invoke(this);
        }

        public override void OnFailed()
        {
        }

        public override void Run()
        {
            foreach (var target in _targets)
            {
                target.InteractEvent += Evaluate;
            }
        }

        private void Evaluate()
        {
            if (IsCompleted) { OnCompleted(); }
        }

        public override void Create<T>(string name, T target, string description)
        {
            _name = name;
            _description = description;
            _targets = target as SimpleInteractable[];
        }
    }
}