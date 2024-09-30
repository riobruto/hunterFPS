using Life.StateMachines;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Life.Controllers
{
    public class BlindAgentController : AgentController
    {
        private bool _isAttackingPlayer;

        private float _timeToForgetPlayer = 20;
        private float _lastReportTime;
        private bool _observersKnowPlayer => _observers.Any(x => x.PlayerDetected);
        private bool _forgotPlayer => !_observersKnowPlayer && Time.realtimeSinceStartup - _lastReportTime > _timeToForgetPlayer;

        public bool KnowsPlayerPosition { get => _observersKnowPlayer; }

        public override void OnStart()
        {
            CreateStates();
            CreateTransitions();

            SetMaxHealth(100000);
            SetHealth(100000);
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_rest, _goto, new FuncPredicate(() => _observersKnowPlayer));
            Machine.AddTransition(_rest, _goto, new FuncPredicate(() => PendingPlayerSoundChase && !_observersKnowPlayer));
            Machine.AddTransition(_goto, _attack, new FuncPredicate(() => IsPlayerInRange(4)));
            Machine.AddTransition(_attack, _goto, new FuncPredicate(() => !_isAttackingPlayer && !_forgotPlayer));
            Machine.AddTransition(_attack, _rest, new FuncPredicate(() => !_isAttackingPlayer && _forgotPlayer));
            Machine.AddTransition(_goto, _rest, new FuncPredicate(() => _forgotPlayer));

            Machine.SetState(_rest);
        }

        private BlindAttackState _attack;
        private BlindGoToPlayerState _goto;
        private BlindRestState _rest;
        private BlindDieState _die;

        private float _attackTime = 1;

        private IEnumerator AttackPlayer()
        {
            _isAttackingPlayer = true;
            Animator.SetTrigger("ATTACK");
            yield return new WaitForSeconds(_attackTime);
            {
                Debug.Log("NIGGER WAS ATTACKED, ALLEGEDLY");
            }
            _isAttackingPlayer = false;
            _lastReportTime = Time.realtimeSinceStartup;
            yield return null;
        }

        public override void OnDeath()
        {
            Machine.ForceChangeToState(_die);
        }

        private void CreateStates()
        {
            _attack = new(this);
            _goto = new(this);
            _rest = new(this);
            _die = new(this);
        }

        private List<ObserverAgentController> _observers = new List<ObserverAgentController>();

        public bool PendingPlayerSoundChase;

        public void AddObserverAgent(ObserverAgentController observerAgent)
        {
            _observers.Add(observerAgent);
        }

        public void RemoveObserverAgent(ObserverAgentController observerAgent)
        {
            _observers.Remove(observerAgent);
        }

        internal void BeginAttackPlayer()
        {
            StartCoroutine(AttackPlayer());
        }
    }

    internal class BlindDieState : BaseState
    {
        private BlindAgentController blind;

        public BlindDieState(AgentController context) : base(context)
        {
            blind = context as BlindAgentController;
        }

        public override void DrawGizmos()
        { }

        public override void End()
        { }

        public override void Start()
        {
        }

        public override void Update()
        {
        }
    }

    public class BlindRestState : BaseState
    {
        private BlindAgentController blind;

        public BlindRestState(AgentController context) : base(context)
        {
            blind = context as BlindAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
        }

        public override void Update()
        {
            blind.SetTarget(Vector3.zero);
        }
    }

    public class BlindGoToPlayerState : BaseState
    {
        private BlindAgentController blind;

        public BlindGoToPlayerState(AgentController context) : base(context)
        {
            blind = context as BlindAgentController;
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(blind.PlayerPosition + (blind.transform.position - blind.PlayerPosition).normalized * 2f, .35f);
        }

        public override void End()
        {
            blind.PendingPlayerSoundChase = false;
        }

        public override void Start()
        {
            blind.FaceTarget = true;
        }

        public override void Update()
        {
            blind.SetLookTarget(blind.PlayerHeadPosition);

            if (blind.KnowsPlayerPosition)
            {
                blind.SetTarget(blind.PlayerPosition + (blind.transform.position - blind.PlayerPosition).normalized * 2f);
            }
        }
    }

    public class BlindAttackState : BaseState
    {
        private BlindAgentController blind;

        public BlindAttackState(AgentController context) : base(context)
        {
            blind = context as BlindAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            blind.BeginAttackPlayer();
        }

        public override void Update()
        {
        }
    }
}