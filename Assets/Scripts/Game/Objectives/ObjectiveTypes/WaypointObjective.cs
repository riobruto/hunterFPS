using Core.Engine;
using Game.Service;
using UnityEngine;

namespace Game.Objectives
{
    public class WaypointObjective : Objective
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _radius;

        private GameObject _player;
        private bool _active;

        public override void Create<T>(string name, T target, string description = "")
        {
            _target = target as Transform;
        }

        private bool Reached => Vector3.Distance(_player.transform.position, _target.position) < _radius;

        public override Vector3 TargetPosition => _target.position;

        private void Update()
        {
            if (!_active) return;
            if (Reached) { Status = ObjectiveStatus.COMPLETED; _active = false; }
        }

        public override void Run()
        {
            _player = Bootstrap.Resolve<PlayerService>().Player;
            _active = true;
        }
    }
}