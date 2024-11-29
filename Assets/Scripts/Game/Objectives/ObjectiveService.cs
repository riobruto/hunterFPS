using Core.Engine;

namespace Game.Objectives
{
    public delegate void ObjectiveDelegate(ObjectiveReckoner reckoner, Objective current);

    internal class ObjectiveService : SceneService
    {
        private ObjectiveReckoner _currentReckoner;
        public event ObjectiveDelegate CompleteEvent;
        public event ObjectiveDelegate AdvanceEvent;
        public event ObjectiveDelegate ReckonerChanged;

        public Objective GetCurrentObjective()
        {
            return _currentReckoner.CurrentObjective;
        }
        public void SetReckoner(ObjectiveReckoner reckoner)
        {
            if (_currentReckoner != null)
            {
                _currentReckoner.ReckonerAdvancedEvent -= OnAdvance;
                _currentReckoner.ReckonerCompletedEvent -= OnComplete;
            }
            _currentReckoner = reckoner;
            _currentReckoner.ReckonerAdvancedEvent += OnAdvance;
            _currentReckoner.ReckonerCompletedEvent += OnComplete;
            ReckonerChanged?.Invoke(reckoner, reckoner.CurrentObjective);
        }
        private void OnAdvance(ObjectiveReckoner reckoner) => AdvanceEvent?.Invoke(reckoner, reckoner.CurrentObjective);
        private void OnComplete(ObjectiveReckoner reckoner) => CompleteEvent?.Invoke(reckoner, reckoner.CurrentObjective);
    }
}