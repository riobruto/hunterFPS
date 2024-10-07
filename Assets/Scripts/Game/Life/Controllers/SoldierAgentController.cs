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
        private ObserverAttackState _attack;
        private ObserverDieState _die;
        private ObserverTakeCoverState _takeCover;

        private AgentCoverSensor _cover;
        private AgentWeapon _weapon;

        public AgentCoverSensor CoverSensor => _cover;

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

        public override void OnDeath()
        {
            Machine.ForceChangeToState(_die);
            Ragdoll();
            NavMesh.isStopped = true;

            Animator.SetTrigger("DIE");
            Animator.SetLayerWeight(2, 0);
            Animator.SetLayerWeight(3, 0);
            Animator.SetLayerWeight(4, 0);
        }

        public void Ragdoll()
        {
            Animator.enabled = false;
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

        private float _lastBurstTime = 0;
        private float _burstTime = 0;
        private bool _shooting;

        private void ManageWeapon()
        {
            if (IsDead) return;

            if (_weapon.HasNoAmmo)
            {
                _weapon.WeaponEngine.ReleaseFire();
                _weapon.WeaponEngine.Reload(_weapon.WeaponEngine.WeaponSettings.Ammo.Size);
            }

            if (!_allowFire) return;

            CoverType = _weapon.WeaponEngine.CurrentAmmo > 10 ? CoverSearchType.NEAREST_FROM_PLAYER : CoverSearchType.FARTEST_FROM_PLAYER;

            if (Time.time - _lastBurstTime > _burstTime)
            {
                _burstTime = Random.Range(0.5f, 2f);
                _lastBurstTime = Time.time;
                _shooting = !_shooting;
            }

            if (_shooting) _weapon.WeaponEngine.Fire();
            else _weapon.WeaponEngine.ReleaseFire();
        }

        private void CreateStates()
        {
            _wander = new(this);
            _report = new(this);
            _attack = new(this);
            _takeCover = new(this);
            _die = new(this);
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_wander, _report, new FuncPredicate(() => PlayerDetected));
            Machine.AddTransition(_wander, _report, new FuncPredicate(() => _combatReported));
            Machine.AddTransition(_report, _takeCover, new FuncPredicate(() => _combatReported));
            Machine.AddTransition(_takeCover, _attack, new FuncPredicate(() => _takeCover.Reached));
            Machine.AddTransition(_attack, _wander, new FuncPredicate(() => _lostPlayer));

            Machine.SetState(_wander);
        }

        private float _lastPlayerSawTime;
        private bool _lostPlayer => Time.realtimeSinceStartup - _lastPlayerSawTime > 20;

        public CoverSearchType CoverType;
        private float _hurtStopVelocityMultiplier;
        private bool _allowFire;

        public float HurtVelocityMultiplier => _hurtStopVelocityMultiplier;

        private float _reportRadius = 30;

        internal void ReportPlayer()
        {
            _lastPlayerSawTime = Time.realtimeSinceStartup;
            _reportedPlayer = true;

            foreach (SoldierAgentController controller in FindObjectsOfType<SoldierAgentController>())
            {
                if (controller == this) continue;
                if (Vector3.Distance(controller.transform.position, transform.position) > _reportRadius) continue;

                controller.ReportCombat();
            }
        }

        public void ReportCombat()
        {
            if (IsDead) return;
            if (!_combatReported)
            {
                _lastPlayerSawTime = Time.time;
                _combatReported = true;
            }
        }

        internal void ResetReport()
        {
            _reportedPlayer = false;
        }

        public override void OnHurt(float value)
        {
            if (IsDead) return;
            Machine.ForceChangeToState(_report);
            SetHealth(GetHealth() - value);
            Animator.SetTrigger("HURT");
            _hurtStopVelocityMultiplier = 0;
        }

        public void AllowFire(bool state)
        {
            _allowFire = state;
        }

        public override void OnHeardSteps()
        {
            if (IsDead) return;
            Machine.ForceChangeToState(_attack);
        }

        public override void OnHeardCombat()
        {
            if (IsDead) return;
            Machine.ForceChangeToState(_attack);
        }

        [SerializeField] private AudioClip[] _shouts;
        private bool _combatReported;

        internal void Shout()
        {
            AudioSource.PlayClipAtPoint(_shouts[Random.Range(0, _shouts.Length)], Head.transform.position);
        }

        internal void DropWeapon()
        {
            _weapon.DropWeapon();
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
            _observer.Animator.SetBool("WARNING", false);
            _observer.Animator.SetLayerWeight(2, 0);
            _observer.Animator.SetLayerWeight(3, 0);
            FindNewTarget();
            _observer.FaceTarget = true;
            _observer.NavMesh.speed = 1f;
        }

        public override void Update()
        {
            if (ReachedTarget())
            {
                FindNewTarget();
            }

            Vector3 pos = _targetPos;
            pos.y = _observer.transform.position.y;
            _observer.SetLookTarget(pos + Vector3.up * 1.75f);
        }

        private void FindNewTarget()
        {
            _targetPos = Random.insideUnitSphere * 10 + _observer.transform.position;
            _observer.SetTarget(_targetPos);
        }

        private bool ReachedTarget()
        {
            Vector3 pos = _targetPos;
            pos.y = _observer.transform.position.y;
            return Vector3.Distance(_observer.transform.position, pos) < 1.5f;
        }
    }

    public class ObserverTakeCoverState : BaseState
    {
        private SoldierAgentController _observer;
        private Vector3 _destination;
        private bool _reached => (_observer.transform.position - _destination).sqrMagnitude < 1;

        public bool Reached { get => _reached && _hasReached; }

        private bool _hasReached;

        public ObserverTakeCoverState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawWireSphere(_destination, 1f);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.Animator.SetBool("WARNING", true);
            _observer.FaceTarget = false;
            _observer.NavMesh.speed = 5;

            CoverData cover = _observer.CoverSensor.FindNearestCover(_observer.transform.position, _observer.PlayerHeadPosition);

            _destination = cover.Position;

            if (_destination != Vector3.zero)
            {
                _observer.SetTarget(_destination);
            }
        }

        public override void Update()
        {
            if (_reached && !_hasReached)
            {
                _hasReached = true;
                _observer.NavMesh.speed = 1;
                _observer.Animator.SetBool("CROUCH", true);
            }
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
            _observer.Shout();
            _observer.Animator.SetLayerWeight(2, 1);
            _observer.Animator.SetLayerWeight(3, 1);
            _observer.ResetReport();
            _observer.SetTarget(_observer.transform.position);
            //_observer.Animator.SetTrigger("SUSPECT");
            _time = Time.time;
        }

        public override void Update()
        {
            if (Time.time - _time > 1)
            {
                _observer.Animator.SetBool("WARNING", true);
                _observer.ReportPlayer();
            }
        }
    }

    public class ObserverAttackState : BaseState
    {
        public ObserverAttackState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        private SoldierAgentController _observer;
        private Vector3 _destination;
        private float _lastEvaluationTime;
        private CoverData cover;

        public override void DrawGizmos()
        {
            Gizmos.DrawWireSphere(_destination, 1f);

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
            Evaluate();
        }

        private void Evaluate()
        {
            cover = _observer.CoverSensor.FindNearestCover(_observer.PlayerGameObject.transform.position, _observer.PlayerHeadPosition);
            _destination = cover.Position;
            _observer.Animator.SetBool("CROUCH", cover.NeedCrouch);
        }

        public override void Update()
        {
            if (Time.time - _lastEvaluationTime > 3)
            {
                Evaluate();
                _lastEvaluationTime = Time.time;
            }
            float speed = 0;
            if (!cover.NeedCrouch)
            {
                speed = _observer.CoverType == CoverSearchType.FARTEST_FROM_PLAYER ? 5 : 3;
            }
            else speed = 1;

            _observer.NavMesh.speed = speed * _observer.HurtVelocityMultiplier;
            _destination = cover.Position;
            _observer.AllowFire(_observer.IsPlayerInRange(50) && _observer.IsPlayerVisible());
            _observer.SetLookTarget(_observer.PlayerHeadPosition);

            if (_destination != Vector3.zero)
            {
                _observer.SetTarget(_destination);
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
            _observer.AllowFire(false);
            _observer.DropWeapon();
        }

        public override void Update()
        {
        }
    }
}