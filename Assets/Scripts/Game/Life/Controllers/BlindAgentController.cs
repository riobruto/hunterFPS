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
        private bool _knowsPlayerPosition => _observers.Any(x => x.PlayerInSight);
        private bool _forgotPlayer => !_knowsPlayerPosition && Time.realtimeSinceStartup - _lastReportTime > _timeToForgetPlayer;
        private bool _playerInAttackRange => PlayerBehavior.IsPlayerInRange(5) && PlayerBehavior.IsPlayerInViewAngle(.8f);
        private bool _confused;

        public override void OnStart()
        {
            CreateStates();
            CreateTransitions();
            PlayerBehavior.HeardPlayerEvent.AddListener(OnHeardPlayer);

            SetMaxHealth(100000);
            SetHealth(100000);
        }

        private void OnHeardPlayer(float arg0, Vector3 arg1)
        {
            PendingPlayerSoundChase = true;
            _lastReportTime = Time.realtimeSinceStartup;
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_rest, _goto, new FuncPredicate(() => _knowsPlayerPosition));
            Machine.AddTransition(_rest, _goto, new FuncPredicate(() => PendingPlayerSoundChase && !_knowsPlayerPosition));

            Machine.AddTransition(_goto, _attack, new FuncPredicate(() => _playerInAttackRange));
            Machine.AddTransition(_goto, _confuse, new FuncPredicate(() => !_playerInAttackRange));

            Machine.AddTransition(_confuse, _rest, new FuncPredicate(() => _forgotPlayer));

            Machine.AddTransition(_attack, _goto, new FuncPredicate(() => !_isAttackingPlayer && !_forgotPlayer));
            Machine.AddTransition(_attack, _rest, new FuncPredicate(() => !_isAttackingPlayer && _forgotPlayer));

            Machine.AddTransition(_goto, _rest, new FuncPredicate(() => _forgotPlayer));

            Machine.AddAnyTransition(_die, new FuncPredicate(() => IsDead));
            Machine.SetState(_rest);
        }

        private BlindAttackState _attack;
        private BlindGoToPlayerState _goto;
        private BlindRestState _rest;
        private BlindDieState _die;
        private BlindConfusedState _confuse;

        private float _attackTime = 2;

        private IEnumerator AttackPlayer()
        {
            _isAttackingPlayer = true;

            yield return new WaitForSeconds(_attackTime);
            //Attack Logic
            {
                Debug.Log("NIGGER WAS ATTACKED, ALLEGEDLY");
            }
            _isAttackingPlayer = false;
            _lastReportTime = Time.realtimeSinceStartup;
            yield return null;
        }

        private void CreateStates()
        {
            _attack = new(this);
            _goto = new(this);
            _rest = new(this);
            _die = new(this);
            _confuse = new(this);
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
            blind.MoveBehavior.SetTarget(Vector3.zero);
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
            Gizmos.DrawSphere(blind.PlayerBehavior.PlayerPosition + (blind.PlayerBehavior.PlayerPosition - blind.transform.position).normalized * 2f, .35f);
        }

        public override void End()
        {
            blind.PendingPlayerSoundChase = false;
        }

        public override void Start()
        {
            blind.MoveBehavior.FaceTarget = true;
            blind.MoveBehavior.SetTarget(blind.PlayerBehavior.PlayerPosition + (blind.PlayerBehavior.PlayerPosition - blind.transform.position).normalized * 2f);
        }

        public override void Update()
        {
            blind.MoveBehavior.SetLookTarget(blind.PlayerBehavior.PlayerHeadPosition);
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

    public class BlindConfusedState : BaseState
    {
        private BlindAgentController blind;

        public BlindConfusedState(AgentController context) : base(context)
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
        }
    }
}