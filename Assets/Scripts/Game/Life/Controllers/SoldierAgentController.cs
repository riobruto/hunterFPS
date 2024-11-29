using Game.Entities;
using Game.Life;
using Game.Life.WaypointPath;
using Game.Player.Weapon;
using Game.Service;
using Life.StateMachines;

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

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
        public AgentWaypoints Waypoints => _waypoints;

        [SerializeField] private bool _useWaypoints;
        private AgentWaypoints _waypoints;

        //StealthData
        public override void OnStart()
        {
            _cover = gameObject.AddComponent<AgentCoverSensor>();
            _weapon = gameObject.GetComponent<AgentFireWeapon>();
            _waypoints = gameObject.GetComponent<AgentWaypoints>();

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
                    Animator.SetBool("AIM", false);
                    _desiredSpeed = _runSpeed;
                    break;

                case SoldierMovementType.WALK:
                    if (_current == SoldierMovementType.CROUCH)
                    {
                        Animator.SetBool("WARNING", true);
                        StartCoroutine(SetCrouch(false, _walkSpeed));
                        break;
                    }
                    Animator.SetBool("AIM", true);
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
                    Animator.SetBool("AIM", true);
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
        public bool UseWaypoints => _useWaypoints;

        private float _lastBurstTime = 0;
        private float _burstTime = 0;
        private bool _shooting;

        [SerializeField] private AudioClip[] _shouts;

        private SoldierPatrolState _wander;
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

            //suspect,detect,report, asuntos separados)como la iglesia y el estado))))

            Machine.AddTransition(_wander, _investigate, new FuncPredicate(() => !_isEngaged && _isSuspicius));
            Machine.AddTransition(_wander, _report, new FuncPredicate(() => _isEngaged));
            Machine.AddTransition(_report, _attack, new FuncPredicate(() => _isEngaged && _report.Done && AgentGlobalService.Instance.TryTakeAttackSlot(this)));
            Machine.AddTransition(_report, _retreatCover, new FuncPredicate(() => _isEngaged && _report.Done && !AgentGlobalService.Instance.TryTakeAttackSlot(this)));
            Machine.AddTransition(_attack, _retreatCover, new FuncPredicate(() => _weapon.WeaponEngine.CurrentAmmo < _weapon.WeaponEngine.MaxAmmo / 3));
            Machine.AddTransition(_retreatCover, _attack, new FuncPredicate(() => _retreatCover.Ready && _isEngaged && AgentGlobalService.Instance.TryTakeAttackSlot(this)));
            Machine.AddTransition(_attack, _wander, new FuncPredicate(() => !_isEngaged && !_isSuspicius));
            Machine.AddTransition(_investigate, _attack, new FuncPredicate(() => _isEngaged));
            Machine.AddTransition(_retreatCover, _investigate, new FuncPredicate(() => _retreatCover.Ready && !_isEngaged));

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
            _engagedTime = 50;
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
                _suspisiusTime = 120;
                _interestPoint = PlayerGameObject.transform.position;
                return;
            }
            _engagedTime = 40;
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
                _burstTime = Random.Range(0.5f, 1f);
                _lastBurstTime = Time.time;
                _shooting = !_shooting;
            }

            if (_shooting) _weapon.WeaponEngine.Fire();
            else _weapon.WeaponEngine.ReleaseFire();
        }

        public override void OnHeardCombat()
        {
            if (IsDead) return;
            _engagedTime = 50;
        }

        public void ForceRetreatToCover() => Machine.ForceChangeToState(_retreatCover);

        public void ForceAttack() => Machine.ForceChangeToState(_attack);

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

        public override void ForcePlayerPerception()
        {
            if (IsDead) return;
            _engagedTime = 120;
        }
    }

    public class SoldierPatrolState : BaseState
    {
        public SoldierPatrolState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        private SoldierAgentController _observer;

        private Vector3 _targetPos;
        private float _lastWaitTime;
        private float _waitTime;
        private Waypoint _currentWaypoint;

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
            _observer.Animator.SetLayerWeight(2, 0);

            _currentWaypoint = _observer.Waypoints.CurrentWaypoint;
            if (FindNewTarget()) _observer.SetTarget(_targetPos);
            _observer.FaceTarget = false;
        }

        public override void Update()
        {
            if (ReachedTarget())
            {
                _waitTime += Time.deltaTime;

                if (_waitTime > _currentWaypoint.WaitTime)
                {
                    _waitTime = 0;
                    if (FindNewTarget())
                    {
                        _observer.SetTarget(_targetPos);
                    }
                }
            }
        }

        private bool FindNewTarget()
        {
            if (!_observer.UseWaypoints)
            {
                int randomTries = 5;
                for (int i = 0; i < randomTries; i++)
                {
                    if (NavMesh.SamplePosition(_observer.transform.position + Random.insideUnitSphere * 5f, out NavMeshHit rhit, 5, NavMesh.AllAreas))
                    {
                        _targetPos = rhit.position;

                        return true;
                    }
                }
                return false;
            }
            _currentWaypoint = _currentWaypoint.NextWaypoint;

            if (NavMesh.SamplePosition(_currentWaypoint.transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas))
            {
                _targetPos = hit.position;

                return true;
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
            _observer.Animator.SetLayerWeight(3, 1);
           
            _observer.FaceTarget = false;
            _hasReached = false;
            _reloaded = false;
            _observer.AllowFire(false);
            _relaxTime = Random.Range(3, 5);
            _timeToExit = 0f;
            FindCover();
        }

        private void FindCover()
        {
            _lastFindCover = Time.time;
            Vector3 _combatPoint = _observer.transform.position;

            SpatialDataPoint[] points = _observer.CoverSensor.GetCombatSpatialData(_combatPoint, _observer.PlayerHeadPosition, 10).ToArray();

            points = points.OrderBy(x => x.PathLength).ToArray();

            if (points.Any(x => x.SafeFromStanding))
            {
                _destination = points.First(x => x.SafeFromStanding).Position;
                _observer.SetTarget(_destination);
                return;
            }
            else if (points.Any(x => x.SafeFromCrouch && !x.SafeFromStanding))
            {
                _destination = points.First(x => x.SafeFromCrouch && !x.SafeFromStanding).Position;
                _observer.SetTarget(_destination);
                return;
            }
            else
            {
                if (points.Length > 0) _destination = (points.OrderBy(x => x.DistanceFromThreat).First().Position);
                _observer.SetTarget(_destination);
            }
        }

        public bool _reloaded;
        private float _lastFindCover;

        public override void Update()
        {
            if (_observer.Weapon.WeaponEngine.CurrentAmmo > 0)
            {
                _observer.FaceTarget = true;
                _observer.Animator.SetLayerWeight(3, 1);
                _observer.AllowFire(_observer.PlayerVisualDetected);
                _observer.SetLookTarget(_observer.PlayerHeadPosition);
            }
            else
            {
                _observer.Animator.SetLayerWeight(3, 0);
                _observer.FaceTarget = false;
                _observer.AllowFire(false);
            }

            if (_observer.PlayerVisualDetected && Time.time - _lastFindCover > 5)
            {
                _observer.SetMovementType(SoldierMovementType.RUN);
                FindCover();
            }

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

        private float _cooldown;

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
            AgentGlobalService.Instance.ReleaseAttackSlot(_observer);
        }

        public override void Start()
        {
            _cooldown = 2;
            _observer.FaceTarget = true;
            GeneratePoints();
            _observer.SetMovementType(SoldierMovementType.WALK);

            _observer.Animator.SetLayerWeight(3, 1);
            _observer.Animator.SetLayerWeight(2, 1);
        }

        private void GeneratePoints()
        {
            Vector3 combatPoint = Vector3.Lerp(_observer.transform.position, _observer.PlayerHeadPosition, 0.5f);

            SpatialDataPoint[] points = _observer.CoverSensor.GetCombatSpatialData(combatPoint, _observer.PlayerHeadPosition, 10, Random.Range(3f, 10f)).ToArray();

            if (points.Any(x => x.SafeFromCrouch && !x.SafeFromStanding))
            {
                _observer.SetTarget(points.First(x => x.SafeFromCrouch && !x.SafeFromStanding).Position);
                return;
            }
            if (points.Any(x => !x.SafeFromCrouch && !x.SafeFromStanding))
            {
                _observer.SetTarget(points.First(x => !x.SafeFromCrouch && !x.SafeFromStanding).Position);
                return;
            }
            else
            {
                _observer.SetTarget(_observer.PlayerHeadPosition - (_observer.PlayerHeadPosition - _observer.transform.position).normalized * 6f);
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
                _evaluatinInterval = Random.Range(1, 5);
            }

            //_observer.FaceTarget = _observer.IsPlayerVisible();

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

            if (_cooldown > 0)
            {
                _cooldown -= Time.deltaTime;
                return;
            }

            _observer.AllowFire(_observer.PlayerVisualDetected);
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