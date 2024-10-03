using Game.Entities;
using Game.Life;
using Game.Player.Weapon;
using Life.Controllers;
using System.Collections;
using UnityEngine;

namespace Life.StateMachines
{
    public class SoldierAgentController : AgentController
    {
        private bool _reportedPlayer;

        private bool _playerMadeNoise;

        private ObserverWanderState _wander;
        private ObserverReportState _report;
        private ObserverEscapeState _escape;
        private ObserverDieState _die;

        private AgentCoverSensor _cover;
        private AgentWeapon _weapon;

        public override void OnStart()
        {
            _cover = gameObject.AddComponent<AgentCoverSensor>();
            _weapon = gameObject.GetComponent<AgentWeapon>();

            CreateStates();
            CreateTransitions();
            SetMaxHealth(100);
            SetHealth(100);

            StartCoroutine(BindWeapon());
        }

        private IEnumerator BindWeapon()
        {
            yield return new WaitForEndOfFrame();
            _weapon.WeaponEngine.WeaponChangedState += OnWeaponChangeState;
        }

        private void OnWeaponChangeState(object sender, WeaponStateEventArgs e)
        {
            if (e.State == Core.Weapon.WeaponState.BEGIN_SHOOTING)
            {
                Animator.SetTrigger("FIRE");
            }
            if (e.State == Core.Weapon.WeaponState.BEGIN_RELOADING)
            {
                Animator.SetTrigger("RELOAD");
            }
            if (e.State == Core.Weapon.WeaponState.BEGIN_RELOADING_EMPTY)
            {
                Animator.SetTrigger("RELOAD");
            }
        }

        public CoverData GetCover(CoverSearchType type)
        {
            return _cover.FindCover(type);
        }

        public override void OnDeath()
        {
            Machine.ForceChangeToState(_die);
            Animator.enabled = false;
            NavMesh.isStopped = true;

            foreach (CharacterLimbHitbox body in GetComponentsInChildren<CharacterLimbHitbox>(true))
            {
                body.Ragdoll();
            }
        }

        public override void OnUpdate()
        {
            if (_playerMadeNoise)
            {
                _playerMadeNoise = false;
            }
            ManageWeapon();
            _hurtStopVelocityMultiplier = Mathf.Clamp(_hurtStopVelocityMultiplier + Time.deltaTime, 0, 1);
        }

        private void ManageWeapon()
        {
            if (!_allowFire || IsDead) return;

            CoverType = _weapon.WeaponEngine.CurrentAmmo > 10 ? CoverSearchType.NEAREST_FROM_PLAYER : CoverSearchType.FARTEST_FROM_PLAYER;

            if (_weapon.HasNoAmmo)
            {
                _weapon.WeaponEngine.ReleaseFire();
                _weapon.WeaponEngine.Reload(_weapon.WeaponEngine.WeaponSettings.Ammo.Size);
            }

            if (_weapon.WeaponEngine.CurrentRecoil > Random.Range(0f, .33f))
            {
                return;
            }

            _weapon.WeaponEngine.Fire();
        }

        private void CreateStates()
        {
            _wander = new(this);
            _report = new(this);
            _escape = new(this);

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
        private float _hurtStopVelocityMultiplier;
        private bool _allowFire;

        public float HurtVelocityMultiplier => _hurtStopVelocityMultiplier;

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
            Machine.ForceChangeToState(_die);
        }

        public override void OnHurt(float value)
        {
            SetHealth(GetHealth() - value);
            _hurtStopVelocityMultiplier = 0;
        }

        public void AllowFire(bool state)
        {
            _allowFire = state;
        }
    }

    public class ObserverWanderState : BaseState
    {
        public ObserverWanderState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        private SoldierAgentController _observer;

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
            _observer = context as SoldierAgentController;
        }

        private SoldierAgentController _observer;
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
            _observer = context as SoldierAgentController;
        }

        private SoldierAgentController _observer;
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
            _observer.NavMesh.speed = 5 * _observer.HurtVelocityMultiplier;

            CoverData cover = _observer.GetCover(_observer.CoverType);

            _observer.AllowFire(_observer.IsPlayerInRange(10) && _observer.IsPlayerVisible());

            _observer.SetLookTarget(_observer.PlayerHeadPosition);

            if (cover.Position != Vector3.zero)
            {
                _observer.SetTarget(cover.Position);
            }

            _observer.ReportPlayer();
        }
    }

    public class ObserverDieState : BaseState
    {
        public ObserverDieState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        private SoldierAgentController _observer;

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