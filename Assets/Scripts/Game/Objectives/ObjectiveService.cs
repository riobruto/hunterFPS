using Core.Engine;
using UnityEngine;

namespace Game.Objectives
{
    public delegate void ObjectiveDelegate(ObjectiveReckoner reckoner, Objective current);

    internal class ObjectiveService : SceneService
    {
        private ObjectiveReckoner _currentReckoner;
        private Vector3 _objectivePoint;

        public event ObjectiveDelegate CompleteEvent;

        public event ObjectiveDelegate AdvanceEvent;

        public event ObjectiveDelegate ReckonerChanged;

        public Vector3 ObjectivePoint
        {
            get
            {
                if (_currentReckoner == null) return Vector3.zero;
                if (_currentReckoner.CurrentObjective == null) return Vector3.zero;
                return _currentReckoner.CurrentObjective.TargetPosition;
            }
        }

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

        private void UpdatePosition()
        {
            _objectivePoint = _currentReckoner.CurrentObjective.TargetPosition;
        }

        private void OnAdvance(ObjectiveReckoner reckoner)
        { AdvanceEvent?.Invoke(reckoner, reckoner.CurrentObjective); UpdatePosition(); }

        private void OnComplete(ObjectiveReckoner reckoner)
        { CompleteEvent?.Invoke(reckoner, reckoner.CurrentObjective); UpdatePosition(); }
    }
}