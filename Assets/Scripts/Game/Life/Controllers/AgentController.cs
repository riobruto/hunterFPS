using Game.Life;
using Life.StateMachines;
using System;
using UnityEngine;

namespace Life.Controllers
{
    [RequireComponent(typeof(AgentMoveBehavior), typeof(AgentPlayerBehavior))]
    public class AgentController : MonoBehaviour
    {
        private StateMachine _machine;

        public StateMachine Machine
        { get { return _machine; } }

        public AgentMoveBehavior MoveBehavior { get => _move; }
        public AgentPlayerBehavior PlayerBehavior { get => _player; }

        private AgentMoveBehavior _move;
        private AgentPlayerBehavior _player;

        private void Start()
        {
            _machine = new StateMachine();
            _move = gameObject.GetComponent<AgentMoveBehavior>();
            _player = gameObject.GetComponent<AgentPlayerBehavior>();

            OnStart();
        }

        private void Update()
        {
            _machine?.Update();
            OnUpdate();
        }


        public virtual void OnUpdate() { }
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && _machine != null)
            {
                _machine.DrawGizmos();
            }
        }

        public virtual void OnStart()
        {
        }
    }
}