using Game.Life;
using Life.Controllers;

using UnityEngine;
using UnityEngine.Events;

namespace Life.StateMachines
{
    public class ObserverAgentController : AgentController
    {
        private bool _reportedPlayer;

        [field: SerializeField] public UnityEvent<bool> ReportPlayerEvent;
        [field: SerializeField] public BlindAgentController Attacker;

        private bool _playerMadeNoise;

        private ObserverWanderState _wander;
        private ObserverReportState _report;
        private ObserverEscapeState _escape;
        private ObserverBlindState _blind;
        private ObserverDieState _die;

        private AgentCoverSensor _cover;

        public override void OnStart()
        {
            _cover = gameObject.AddComponent<AgentCoverSensor>();

            CreateStates();
            CreateTransitions();
            Attacker.AddObserverAgent(this);
            SetMaxHealth(100);
            SetHealth(100);
        }

        public CoverData GetCover(CoverSearchType type)
        {
            return _cover.FindCover(type);
        }

        public override void OnDeath()
        {
            Machine.ForceChangeToState(_die);
        }

        public override void OnUpdate()
        {
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
            Machine.AddTransition(_wander, _report, new FuncPredicate(() => PlayerDetected));
            Machine.AddTransition(_report, _escape, new FuncPredicate(() => _reportedPlayer));
            Machine.AddTransition(_escape, _wander, new FuncPredicate(() => _lostPlayer));

            Machine.SetState(_wander);
        }

        private float _lastPlayerSawTime;

        private bool _lostPlayer => Time.realtimeSinceStartup - _lastPlayerSawTime > 20;

        public CoverSearchType CoverType;

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

        public override void OnHurt(float value)
        {
            SetHealth(GetHealth() - value);
        }

        internal CoverData GetCover(object coverType)
        {
            throw new System.NotImplementedException();
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
            FindNewTarget();
            _observer.FaceTarget = false;
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
            _observer.SetTarget(_targetPos);
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
            _observer.ResetReport();

            _observer.SetTarget(_observer.transform.position);
            _time = Time.time;
        }

        public override void Update()
        {
            if (Time.time - _time > 3)
            {
                _observer.ReportPlayer();
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
        private bool _playerNear => Vector3.Distance(_observer.PlayerGameObject.transform.position, _observer.transform.position) < 6;

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
            _observer.FaceTarget = true;
        }

        public override void Update()
        {
            CoverData cover = _observer.GetCover(_observer.CoverType);
            _observer.SetLookTarget(_observer.PlayerHeadPosition);
            if (cover.Position != Vector3.zero)
            {
                _observer.SetTarget(cover.Position);
            }
            _observer.ReportPlayer();
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