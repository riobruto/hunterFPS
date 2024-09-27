using Game.Life;
using Life.StateMachines;
using Life.StateMachines.Interfaces;
using System;
using UnityEngine;
using UnityEngine.Events;

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

        public IState CurrentState => _machine.CurrentState;
        public bool Initialized { get; private set; }

        private float _health;
        private float _maxHealth;
        private bool _isDead;

        public UnityAction<float> HealthChangedEvent;
        public UnityAction DeadEvent;

        public float GetHealth() => _health;

        public bool IsDead => _isDead;
        private bool _changedToDead = false;

        public void SetHealth(float value)
        {
            _health = Mathf.Clamp(value, 0, _maxHealth);
            HealthChangedEvent?.Invoke(_health);

            if (_health == 0 && !_changedToDead)
            {
                DeadEvent?.Invoke();
                OnDeath();
                _changedToDead = true;
            }
        }

        public virtual void OnDeath()
        {
        }

        public float GetMaxHealth() => _maxHealth;

        public void SetMaxHealth(float value) => _maxHealth = value;

        private void Start()
        {
            _machine = new StateMachine();
            Initialized = true;
            _move = gameObject.GetComponent<AgentMoveBehavior>();
            _player = gameObject.GetComponent<AgentPlayerBehavior>();

            OnStart();
        }

        private void Update()
        {
            _machine?.Update();
            OnUpdate();
        }

        public virtual void OnUpdate()
        { }

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