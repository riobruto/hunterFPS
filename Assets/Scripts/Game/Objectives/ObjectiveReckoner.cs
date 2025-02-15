using Core.Engine;
using Game.Service;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Objectives
{
    public delegate void ReckonerDelegate(ObjectiveReckoner reckoner);

    public class ObjectiveReckoner : MonoBehaviour
    {
        private List<Objective> _objectives => _sceneObjectives.ToList();
        private Objective _current;
        public Objective CurrentObjective => _current;

        [Header("Scene Objectives(Sorted in the order of the array)")]
        [SerializeField] private Objective[] _sceneObjectives;

        public event ReckonerDelegate ReckonerAdvancedEvent;

        public event ReckonerDelegate ReckonerCompletedEvent;

        private ObjectiveService _service => Bootstrap.Resolve<ObjectiveService>();
        public bool Completed => _completed;

        private int _index = 0;
        private bool _completed;

        private void Start()
        {
            _service.SetReckoner(this);
            CreateObjectives();
            _current = _objectives[_index];
            _current.StatusChanged += Advance;
            _current.Run();
        }

        private void Advance(Objective objective, ObjectiveStatus status)
        {
            switch (status)
            {
                case ObjectiveStatus.PENDING:

                case ObjectiveStatus.ACTIVE:
                    break;

                case ObjectiveStatus.UPDATED:
                    UIService.CreateMessage("<sprite=0> Objective Updated");
                    break;

                case ObjectiveStatus.COMPLETED:
                    StartCoroutine(EvaluateNextObjective());
                    break;

                case ObjectiveStatus.FAILED:
                    UIService.CreateMessage("<sprite=0> Objective Failed");
                    break;
            }
        }

        private IEnumerator EvaluateNextObjective()
        {
            _current.StatusChanged -= Advance;
            UIService.CreateMessage("<sprite=0> Objective Completed");
            if (_objectives.Count == _index + 1)
            {
                _completed = true;
                ReckonerCompletedEvent?.Invoke(this);
                yield break;
            }
            yield return new WaitForSeconds(_current.ExitDelayInSeconds);
            _index++;
            _current = _objectives[_index];
            _current.StatusChanged += Advance;
            _current.Run();
            ReckonerAdvancedEvent?.Invoke(this);
            UIService.CreateMessage($"<sprite=0> New Objective: {_current.TaskName}");
            yield break;
        }

        private void CreateObjectives()
        {
        }
    }
}