using Core.Engine;
using Game.Entities;
using Game.Service;
using Life.Controllers;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Objectives
{
    public delegate void ReckonerDelegate(ObjectiveReckoner reckoner);

    public class ObjectiveReckoner : MonoBehaviour
    {
        private List<Objective> _objectives = new List<Objective>();
        private Objective _current;
        public Objective CurrentObjective => _current;

        [SerializeField] private BreakableEntity[] _bottles;
        [SerializeField] private SimpleInteractable[] _targetWeapons;
        [SerializeField] private AgentController[] _targetEnemies;

        public event ReckonerDelegate ReckonerAdvancedEvent;

        public event ReckonerDelegate ReckonerCompletedEvent;

        private ObjectiveService service => Bootstrap.Resolve<ObjectiveService>();
        public bool Completed => _completed;

        private int _index = 0;
        private bool _completed;

        private void Start()
        {
            service.SetReckoner(this);

            CreateObjectives();
            _current = _objectives[_index];
            _current.Run();
            _current.CompletedEvent += Advance;
        }

        private void Advance(Objective objective)
        {
            _current.CompletedEvent -= Advance;
            UIService.CreateMessage("Objective Completed");

            if (_objectives.Count == _index + 1)
            {
                _completed = true;
                ReckonerCompletedEvent?.Invoke(this);
                return;
            }

            _index++;
            _current = _objectives[_index];
            _current.CompletedEvent += Advance;
            _current.Run();
            ReckonerAdvancedEvent?.Invoke(this);
            UIService.CreateMessage($"Objective Updated: {_current.TaskName}");         
        }

        private void CreateObjectives()
        {
            MurderObjective murder = new MurderObjective();
            murder.Create("Kill the soldiers", _targetEnemies, "This mfs are hiddem in the town");
            //_objectives.Add(murder);

            PickUpObjective weapon = new PickUpObjective();
            weapon.Create("Pick up the revolver", _targetWeapons, "Is in the ground");
            _objectives.Add(weapon);

            BreakableObjective bottles = new BreakableObjective();
            bottles.Create("Destroy the bottles", _bottles, "The bottles has to be destroyed");
            _objectives.Add(bottles);
        }
    }
}