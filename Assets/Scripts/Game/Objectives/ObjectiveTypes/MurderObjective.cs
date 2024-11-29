using Game.Service;
using Life.Controllers;
using System.Linq;

namespace Game.Objectives
{
    public class MurderObjective : Objective
    {
        public AgentController[] _targets;
        private string _description;
        private string _name;

        public override bool IsCompleted => _targets.All(x => x.IsDead);
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


        private void OnDie()
        {
            if (IsCompleted) { OnCompleted(); }
        }

        public override void Create<T>(string name,T target, string description)
        {
            _name = name;
            _description = description;
            _targets = target as AgentController[];
        }

        public override void Run()
        {
            foreach (AgentController controller in _targets) { controller.DeadEvent += OnDie; }
        }

    }
}