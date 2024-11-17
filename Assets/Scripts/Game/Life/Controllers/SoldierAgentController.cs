using Game.Entities;
using Game.Life;
using Game.Player.Weapon;
using Life.StateMachines;

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Life.Controllers
{
    public enum SoldierMovementType
    {
        RUN,
        WALK,
        PATROL,
        CROUCH
    }

    public class SoldierAgentController : AgentController
    {
        private AgentCoverSensor _cover;
        private AgentFireWeapon _weapon;

        public AgentCoverSensor CoverSensor => _cover;
        public AgentFireWeapon Weapon => _weapon;

        //StealthData
        public override void OnStart()
        {
            _cover = gameObject.AddComponent<AgentCoverSensor>();
            _weapon = gameObject.GetComponent<AgentFireWeapon>();

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

        private float _runSpeed = 5f;
        private float _walkSpeed = 3f;
        private float _patrolSpeed = 1f;
        private float _crouchSpeed = 1f;
        private float _desiredSpeed;

        private SoldierMovementType _current;

        public void SetMovementType(SoldierMovementType type)
        {
            switch (type)
            {
                case SoldierMovementType.RUN:
                    if (_current == SoldierMovementType.CROUCH)
                    {
                        Animator.SetBool("WARNING", true);
                        StartCoroutine(SetCrouch(false, _runSpeed));
                        break;
                    }
                    _desiredSpeed = _runSpeed;
                    break;

                case SoldierMovementType.WALK:
                    if (_current == SoldierMovementType.CROUCH)
                    {
                        Animator.SetBool("WARNING", true);
                        StartCoroutine(SetCrouch(false, _walkSpeed));
                        break;
                    }
                    _desiredSpeed = _walkSpeed;
                    break;

                case SoldierMovementType.PATROL:
                    if (_current == SoldierMovementType.CROUCH)
                    {
                        Animator.SetBool("WARNING", false);
                        StartCoroutine(SetCrouch(false, _patrolSpeed));
                        break;
                    }
                    _desiredSpeed = _patrolSpeed;
                    break;

                case SoldierMovementType.CROUCH:
                    Animator.SetBool("WARNING", true);
                    StartCoroutine(SetCrouch(true, _crouchSpeed));
                    break;
            }
            _current = type;
        }

        private IEnumerator SetCrouch(bool state, float target)
        {
            _desiredSpeed = 0;
            Animator.SetBool("CROUCH", state);
            yield return new WaitForSeconds(1);
            _desiredSpeed = target;
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
            ManageStealth();
            ManageWeapon();

            _hurtStopVelocityMultiplier = Mathf.Clamp(_hurtStopVelocityMultiplier + Time.deltaTime, 0, 1);
            NavMesh.speed = _desiredSpeed * _hurtStopVelocityMultiplier;
        }

        private void ManageStealth()
        {
            if (PlayerVisualDetected) { _engagedTime = 40; }

            _suspisiusTime = Mathf.Clamp(_suspisiusTime - Time.deltaTime, 0, float.MaxValue);
            _engagedTime = Mathf.Clamp(_engagedTime - Time.deltaTime, 0, float.MaxValue);
        }

        private Vector3 _interestPoint;

        private bool _isSuspicius => _suspisiusTime > 0;
        private bool _isEngaged => _engagedTime > 0;

        private float _engagedTime;
        private float _suspisiusTime;

        public Vector3 InterestPoint { get => _interestPoint; }

        private float _lastBurstTime = 0;
        private float _burstTime = 0;
        private bool _shooting;

        [SerializeField] private AudioClip[] _shouts;

        private SoldierWanderState _wander;
        private SoldierReportState _report;
        private SoldierAttackState _attack;
        private SoldierDieState _die;
        private SoldierRetreatCoverState _retreatCover;

        private SoldierSearchAlertState _investigate;

        private void CreateStates()
        {
            _wander = new(this);
            _report = new(this);
            _attack = new(this);
            _retreatCover = new(this);
            _die = new(this);

            _investigate = new(this);
        }

        private void CreateTransitions()
        {
            //TODO: MEJORAR FLAGS DE DETECCION, ESTAN MUY INCONSISTENTES
            //Definir mejor las transiciones ya que crean bucles
            //report, detect, suspect, asuntos separados)como la iglesia y el estado))))

            Machine.AddTransition(_wander, _investigate, new FuncPredicate(() => !_isEngaged && _isSuspicius));

            Machine.AddTransition(_wander, _report, new FuncPredicate(() => _isEngaged));
            Machine.AddTransition(_report, _attack, new FuncPredicate(() => _isEngaged && _report.Done));

            Machine.AddTransition(_attack, _retreatCover, new FuncPredicate(() => _weapon.WeaponEngine.CurrentAmmo < _weapon.WeaponEngine.MaxAmmo / 3));
            Machine.AddTransition(_retreatCover, _attack, new FuncPredicate(() => _retreatCover.Ready && _isEngaged));

            Machine.AddTransition(_attack, _wander, new FuncPredicate(() => !_isEngaged && !_isSuspicius));

            Machine.AddTransition(_investigate, _attack, new FuncPredicate(() => _isEngaged));

            Machine.AddTransition(_retreatCover, _investigate, new FuncPredicate(() => _retreatCover.Ready && _isSuspicius && !_isEngaged));

            Machine.SetState(_wander);
        }

        private float _hurtStopVelocityMultiplier;
        private bool _allowFire;

        public override void OnHurt(float value)
        {
            if (IsDead) return;

            if (!_isEngaged) Machine.ForceChangeToState(_retreatCover);

            SetHealth(GetHealth() - value);
            Animator.SetTrigger("HURT");
            _hurtStopVelocityMultiplier = 0;
            _engagedTime = 30;
        }

        public void AllowFire(bool state)
        {
            _allowFire = state;
        }

        public override void OnHeardSteps()
        {
            if (IsDead) return;

            if (!PlayerVisualDetected)
            {
                _suspisiusTime = 60;

                _interestPoint = PlayerGameObject.transform.position;
                return;
            }
            _engagedTime = 30;
        }

        private void ManageWeapon()
        {
            if (IsDead) return;

            if (_weapon.HasNoAmmo)
            {
                _weapon.WeaponEngine.ReleaseFire();
                _shooting = false;
                return;
            }

            if (!_allowFire)
            {
                _weapon.WeaponEngine.ReleaseFire();
                _shooting = false;
                return;
            }

            if (Time.time - _lastBurstTime > _burstTime)
            {
                _burstTime = Random.Range(0.5f, 2f);
                _lastBurstTime = Time.time;
                _shooting = !_shooting;
            }

            if (_shooting) _weapon.WeaponEngine.Fire();
            else _weapon.WeaponEngine.ReleaseFire();
        }

        public override void OnHeardCombat()
        {
            if (IsDead) return;
            _engagedTime = 30;
        }

        internal void Shout()
        {
            AudioSource.PlayClipAtPoint(_shouts[Random.Range(0, _shouts.Length)], Head.transform.position);
        }

        public void ResetInvestigate()
        {
            _interestPoint = Vector3.zero;
        }

        internal void DropWeapon()
        {
            _weapon.DropWeapon();
        }
    }

    public class SoldierWanderState : BaseState
    {
        public SoldierWanderState(AgentController context) : base(context)
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
            _observer.SetMovementType(SoldierMovementType.PATROL);
            _observer.Animator.SetBool("WARNING", false);
            _observer.Animator.SetLayerWeight(3, 0);
            if (FindNewTarget()) _observer.SetTarget(_targetPos);

            _observer.FaceTarget = false;
        }

        public override void Update()
        {
            if (ReachedTarget())
            {
                if (FindNewTarget())
                {
                    _observer.SetTarget(_targetPos);
                }
            }
        }

        private bool FindNewTarget()
        {
            int tries = 5;
            for (int i = 0; i < tries; i++)
            {
                if (NavMesh.SamplePosition(Random.insideUnitSphere * 10 + _observer.transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas))
                {
                    _targetPos = hit.position;

                    return true;
                }
            }
            return false;
        }

        private bool ReachedTarget()
        {
            Vector3 pos = _targetPos;
            pos.y = _observer.transform.position.y;
            return Vector3.Distance(_observer.transform.position, pos) < 1.5f;
        }
    }

    public class SoldierSearchAlertState : BaseState
    {
        public SoldierSearchAlertState(AgentController context) : base(context)
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
            _observer.SetMovementType(SoldierMovementType.RUN);
            _observer.Animator.SetBool("WARNING", true);
            _observer.Animator.SetLayerWeight(3, 0);
            _observer.SetTarget(_observer.InterestPoint);
            _observer.FaceTarget = false;
        }

        public override void Update()
        {
            if (ReachedTarget())
            {
                _observer.SetMovementType(SoldierMovementType.WALK);
                _observer.ResetInvestigate();
            }
        }

        private bool ReachedTarget()
        {
            Vector3 pos = _observer.InterestPoint;
            pos.y = _observer.transform.position.y;
            return Vector3.Distance(_observer.transform.position, pos) < 1.5f;
        }
    }

    public class SoldierAdvanceCoverState : BaseState
    {
        private SoldierAgentController _observer;
        private Vector3 _destination;
        private bool _reached => (_observer.transform.position - _destination).sqrMagnitude < 2;
        public bool Reached { get => _reached && _hasReached; }
        public bool HasValidCover;
        private bool _hasReached;

        public SoldierAdvanceCoverState(AgentController context) : base(context)
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
            _observer.FaceTarget = true;
            _observer.SetMovementType(SoldierMovementType.RUN);
            FindMovementPoint();
        }

        private void FindMovementPoint()
        {
            SpatialDataPoint[] points = _observer.CoverSensor.GetCombatSpatialData(_observer.PlayerPosition, _observer.PlayerHeadPosition).ToArray();

            points = points.OrderBy(x => x.DistanceFromThreat).ThenBy(x => !x.SafeFromStanding && x.SafeFromCrouch).ToArray();

            _destination = points[0].Position;

            _observer.Animator.SetLayerWeight(3, 0);
            _hasReached = false;

            if (_destination != Vector3.zero)
            {
                _observer.SetTarget(_destination);
            }

            _observer.SetLookTarget(_observer.PlayerHeadPosition);
        }

        public override void Update()
        {
            if (_reached && !_hasReached)
            {
                _hasReached = true;

                if (!_observer.PlayerVisualDetected)
                {
                    FindMovementPoint();

                    return;
                }

                //_observer.Weapon.WeaponEngine.Reload(_observer.Weapon.WeaponEngine.WeaponSettings.Ammo.Size);
                _observer.SetMovementType(SoldierMovementType.CROUCH);
            }
        }
    }

    public class SoldierRetreatCoverState : BaseState
    {
        private SoldierAgentController _observer;

        private Vector3 _destination;
        private bool _reached => Vector3.Distance(_observer.transform.position, _destination) < 2f;

        private bool _hasReached;
        private float _relaxTime;
        private float _timeToExit;

        public bool Ready => _hasReached && _reloaded && _timeToExit > _relaxTime;

        public SoldierRetreatCoverState(AgentController context) : base(context)
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
            _observer.SetMovementType(SoldierMovementType.RUN);
            _observer.Animator.SetLayerWeight(3, 0);
            _observer.FaceTarget = false;
            _hasReached = false;
            _reloaded = false;
            _observer.AllowFire(false);
            _relaxTime = Random.Range(3, 10);
            _timeToExit = 0f;

            FindCover();
            if (_destination != Vector3.zero)
            {
            }
        }

        private void FindCover()
        {
            _destination = Vector3.zero;

            SpatialDataPoint[] points = _observer.CoverSensor.GetCombatSpatialData(_observer.transform.position, _observer.PlayerHeadPosition).OrderBy(x => x.DistanceFromThreat).Reverse().ToArray();

            foreach (SpatialDataPoint point in points)
            {
                if (point.SafeFromCrouch && point.SafeFromStanding)
                {
                    _destination = point.Position;
                    _observer.SetTarget(_destination);
                    return;
                }
                else if (point.SafeFromCrouch && point.SafeFromStanding)
                {
                    _destination = point.Position;
                    _observer.SetTarget(_destination);
                    return;
                }

                _destination = point.Position;
                _observer.SetTarget(_destination);
                return;
            }
        }

        public bool _reloaded;

        public override void Update()
        {
            if (_reached && !_hasReached)
            {
                _hasReached = true;
                _observer.SetMovementType(SoldierMovementType.CROUCH);
            }

            if (_hasReached)
            {
                if (!_reloaded)
                {
                    _observer.Weapon.WeaponEngine.Reload(_observer.Weapon.WeaponEngine.WeaponSettings.Ammo.Size);
                    _reloaded = true;
                }
                _timeToExit += Time.deltaTime;
            }
        }
    }

    public class SoldierReportState : BaseState
    {
        public SoldierReportState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        private SoldierAgentController _observer;
        private float _time;
        private bool _hasReported;

        public bool Done { get => _hasReported; }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.Shout();
            _observer.SetTarget(_observer.transform.position);
            _time = Time.time;
            _observer.Animator.SetBool("WARNING", true);
            _observer.Animator.SetTrigger("SURPRISE");
            _observer.FaceTarget = true;
            _observer.SetTarget(_observer.transform.position + -_observer.transform.forward);
            _hasReported = false;
        }

        public override void Update()
        {
            _observer.SetLookTarget(_observer.PlayerHeadPosition);
            if (Time.time - _time > 1)
            {
                _hasReported = true;
            }
        }
    }

    public class SoldierAttackState : BaseState
    {
        public SoldierAttackState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        private SoldierAgentController _observer;
        private SpatialDataPoint _destination;
        private SpatialDataPoint _nearestAggresive;
        private float _lastEvaluationTime;

        public override void DrawGizmos()
        {
            Gizmos.DrawWireSphere(_destination.Position, 1.15f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_nearestAggresive.Position, 1f);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.FaceTarget = true;
            GeneratePoints();
            _observer.SetMovementType(SoldierMovementType.WALK);
            _observer.Animator.SetLayerWeight(3, 1);
        }

        private void GeneratePoints()
        {
            Vector3 _combatPoint = _observer.transform.position;
            SpatialDataPoint[] points = _observer.CoverSensor.GetCombatSpatialData(_combatPoint, _observer.PlayerHeadPosition).ToArray();

            foreach (SpatialDataPoint point in points)
            {
                if (point.SafeFromCrouch && !point.SafeFromStanding)
                {
                    _destination = point;
                    _observer.SetTarget(_destination.Position);
                    return;
                }

                if (!point.SafeFromCrouch && !point.SafeFromStanding)
                {
                    _destination = point;
                    _observer.SetTarget(_destination.Position);
                    return;
                }
                else
                {
                    _observer.SetTarget(_observer.transform.position + (_observer.PlayerHeadPosition - _observer.transform.position).normalized * 6f);

                    return;
                }
            }
        }

        private bool _checkedMovementType;
        private float _evaluatinInterval;

        public override void Update()
        {
            if (Time.time - _lastEvaluationTime > _evaluatinInterval)
            {
                GeneratePoints();
                //_observer.SetMovementType(SoldierMovementType.RUN);
                _checkedMovementType = false;
                _lastEvaluationTime = Time.time;
                _evaluatinInterval = Random.Range(1, 10);
            }

            _observer.AllowFire(_observer.IsPlayerInRange(50) && _observer.IsPlayerVisible());

            _observer.FaceTarget = _observer.IsPlayerVisible();

            _observer.SetLookTarget(_observer.PlayerHeadPosition);

            if (Vector3.Distance(_destination.Position, _observer.transform.position) < 1 && !_checkedMovementType)
            {
                bool wantCrouch = Random.Range(0, 10) > 7;

                if (wantCrouch)
                {
                    _observer.SetMovementType(SoldierMovementType.CROUCH);
                }
                else _observer.SetMovementType(SoldierMovementType.WALK);
                _checkedMovementType = true;
            }
        }
    }

    public class SoldierDieState : BaseState
    {
        public SoldierDieState(AgentController context) : base(context)
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