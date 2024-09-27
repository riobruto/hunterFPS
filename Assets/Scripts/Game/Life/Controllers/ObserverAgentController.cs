using Game.Hit;
using Game.Life;
using Game.Service;
using Life.Controllers;

using UnityEngine;
using UnityEngine.Events;

namespace Life.StateMachines
{
    [RequireComponent(typeof(AgentPlayerBehavior))]
    public class ObserverAgentController : AgentController, IHittableFromWeapon
    {
        private float _speed;

        private float _viewRange;
        private Vector3 _lastKnowPosition;
        private bool _reportedPlayer;
        private bool _isPlayerVisible => PlayerBehavior.PlayerDetected;
        private bool _lastPlayerVisible;

        [field: SerializeField] public UnityEvent<bool> ReportPlayerEvent;
        [field: SerializeField] public BlindAgentController Attacker;

        public bool PlayerInSight => _isPlayerVisible;
        private bool _playerMadeNoise;

        private ObserverWanderState _wander;
        private ObserverReportState _report;
        private ObserverEscapeState _escape;
        private ObserverBlindState _blind;
        private ObserverDieState _die;

        public override void OnStart()
        {
            CreateStates();
            CreateTransitions();
            PlayerBehavior.HeardPlayerEvent.AddListener(OnHeardPlayer);
            Attacker.AddObserverAgent(this);

            SetMaxHealth(100);
            SetHealth(100);
        }

        public override void OnDeath()
        {
            Machine.ForceChangeToState(_die);
        }

        private void OnHeardPlayer(float arg0, Vector3 arg1)
        {
            _playerMadeNoise = true;
        }

        public override void OnUpdate()
        {
            if (_lastPlayerVisible != _isPlayerVisible)
            {
                ReportPlayerEvent?.Invoke(_isPlayerVisible);
                _lastPlayerVisible = _isPlayerVisible;
            }

            if (_playerMadeNoise)
            {
                _playerMadeNoise = false;
            }
        }

        private void CreateStates()
        {
            _wander = new(this);
            _report = new(this);
            _escape = new(this);
            _blind = new(this);
            _die = new(this);
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_wander, _report, new FuncPredicate(() => _isPlayerVisible));
            Machine.AddTransition(_report, _escape, new FuncPredicate(() => _reportedPlayer));
            Machine.AddTransition(_escape, _wander, new FuncPredicate(() => _lostPlayer));

            Machine.SetState(_wander);
        }

        private float _lastPlayerSawTime;

        private bool _lostPlayer => Time.realtimeSinceStartup - _lastPlayerSawTime > 20;

        internal void ReportPlayer()
        {
            _lastPlayerSawTime = Time.realtimeSinceStartup;
            _reportedPlayer = true;
        }

        internal void ResetReport()
        {
            _reportedPlayer = false;
        }

        internal void Die()
        {
            Attacker.RemoveObserverAgent(this);
            Machine.ForceChangeToState(_die);
        }

        void IHittableFromWeapon.OnHit(HitWeaponEventPayload payload)
        {
            if (IsDead) return;
            SetHealth(GetHealth() - 50);
        }
    }

    public class ObserverPayload
    {
        public Vector3 Position { get; private set; }
        public float Time { get; private set; }

        public bool InSight { get; private set; }

        public ObserverPayload(Vector3 position, float time, bool inSight)
        {
            Position = position;
            Time = time;
            InSight = inSight;
        }
    }

    public class ObserverWanderState : BaseState
    {
        public ObserverWanderState(AgentController context) : base(context)
        {
            _observer = context as ObserverAgentController;
        }

        private ObserverAgentController _observer;

        private Vector3 _targetPos;

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(_targetPos, 0.33f);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            Context.MoveBehavior.StartPatrol();
            FindNewTarget();
            _observer.MoveBehavior.FaceTarget = false;
        }

        public override void Update()
        {
            if (ReachedTarget())
            {
                FindNewTarget();
            }

            //_observer.MoveBehavior.SetLookTarget(_targetPos + Vector3.up * 1f);
        }

        private void FindNewTarget()
        {
            _targetPos = _observer.Attacker.transform.position + Random.insideUnitSphere * 15;
            _observer.MoveBehavior.SetTarget(_targetPos);
        }

        private bool ReachedTarget()
        {
            Vector3 pos = _targetPos;
            pos.y = _observer.transform.position.y;
            return Vector3.Distance(_observer.transform.position, pos) < 1.5f;
        }
    }

    public class ObserverReportState : BaseState
    {
        public ObserverReportState(AgentController context) : base(context)
        {
            _observer = context as ObserverAgentController;
        }

        private ObserverAgentController _observer;
        private float _time;

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            Context.MoveBehavior.StartWarning();
            _observer.ResetReport();
            _observer.MoveBehavior.Crouch(true);
            _observer.MoveBehavior.SetTarget(_observer.transform.position);
            _time = Time.time;
        }

        public override void Update()
        {
            if (Time.time - _time > 3)
            {
                _observer.ReportPlayer();
                _observer.MoveBehavior.Crouch(false);
            }
        }
    }

    public class ObserverEscapeState : BaseState
    {
        public ObserverEscapeState(AgentController context) : base(context)
        {
            _observer = context as ObserverAgentController;
        }

        private ObserverAgentController _observer;
        private bool _playerNear => Vector3.Distance(_observer.PlayerBehavior.PlayerGameObject.transform.position, _observer.transform.position) < 6;

        public override void DrawGizmos()
        {
            /* Gizmos.DrawWireSphere(_observer.Attacker.position, .4f);
             Gizmos.color = Color.red;
             Gizmos.DrawWireSphere(_observer.Player.PlayerGameObject.transform.position, .4f);*/
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.MoveBehavior.FaceTarget = true;
        }

        public override void Update()
        {
            _observer.MoveBehavior.SetLookTarget(_observer.PlayerBehavior.PlayerGameObject.transform.position);

            if (_playerNear)
            {
                _observer.MoveBehavior.SetTarget(_observer.transform.position - (_observer.PlayerBehavior.PlayerGameObject.transform.position - _observer.transform.position).normalized * 6f);
                return;
            }

            _observer.MoveBehavior.SetTarget(_observer.Attacker.transform.position - (_observer.Attacker.transform.position - _observer.transform.position).normalized * 6f);
        }
    }

    public class ObserverBlindState : BaseState
    {
        public ObserverBlindState(AgentController context) : base(context)
        {
            _observer = context as ObserverAgentController;
        }

        private ObserverAgentController _observer;

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

    public class ObserverDieState : BaseState
    {
        public ObserverDieState(AgentController context) : base(context)
        {
            _observer = context as ObserverAgentController;
        }

        private ObserverAgentController _observer;

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