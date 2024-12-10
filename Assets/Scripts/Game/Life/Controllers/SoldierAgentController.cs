using Game.Audio;
using Game.Entities;
using Game.Life;
using Game.Life.WaypointPath;
using Game.Player.Sound;
using Game.Player.Weapon;
using Game.Service;
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
        [SerializeField] private AudioClip[] _footsteps;
        [SerializeField] private AudioClip[] _shouts;
        [SerializeField] private AudioClipCompendium _painScream;
        [SerializeField] private AudioClipCompendium _deadScream;

        [SerializeField] private bool _useWaypoints;
        [SerializeField] private float _footstepDistance;
        [SerializeField] private float _crouchSpeed = 1f;
        [SerializeField] private float _patrolSpeed = 1f;
        [SerializeField] private float _runSpeed = 4.5f;
        [SerializeField] private float _walkSpeed = 2.5f;
        private bool _allowFire;
        private Vector3 _attackPoint;
        private float _burstTime = 0;
        private SoldierMovementType _current;
        private float _desiredSpeed;
        private float _engagedTime;
        private Vector3 _enterPoint;
        private float _hurtStopVelocityMultiplier;
        private Vector3 _interestPoint;
        private float _lastBurstTime = 0;
        private AudioSource _movementAudio;
        private bool _shooting;
        private float _suspisiusTime;
        private float _travelledDistance;

        //STATES
        private SoldierReportState _report;

        private SoldierSearchAlertState _investigate;
        private SoldierEnterState _enter;
        private SoldierDieState _die;
        private SoldierAttackState _attack;
        private SoldierIncapacitatedState _incapacitated;
        private SoldierRetreatCoverState _retreatCover;

        private SoldierPatrolState _wander;
        private AgentWaypoints _waypoints;
        private AgentFireWeapon _weapon;
        private AgentCoverSensor _cover;
        private float _desiredWeight;
        private float _desiredRefWeight;

        public Vector3 AttackPoint { get => _attackPoint; }
        public AgentCoverSensor CoverSensor => _cover;
        public Vector3 EnterPoint { get => _enterPoint; }
        public Vector3 InterestPoint { get => _interestPoint; }
        public bool UseWaypoints => _useWaypoints;
        public AgentWaypoints Waypoints => _waypoints;
        public AgentFireWeapon Weapon => _weapon;
        private bool _isEngaged => _engagedTime > 0;
        private bool _isSuspicius => _suspisiusTime > 0;

        public void SetArmsLayerWeight(float target)
        {
            _desiredWeight = target;
        }

        public void AllowFire(bool state)
        {
            _allowFire = state;
        }

        public void ForceAttack() => Machine.ForceChangeToState(_attack);

        //TODO: IMPLEMENT ENTER STATE FOR SPAWNING
        public void ForceGoToPoint(Vector3 point)
        {
            _enterPoint = point;
            StartCoroutine(IGoToPointSpawn());
        }

        public override void ForcePlayerPerception()
        {
            if (IsDead) return;
            _engagedTime = 120;
            _attackPoint = PlayerHeadPosition;
            _interestPoint = PlayerGameObject.transform.position;
        }

        public void ForceRetreatToCover() => Machine.ForceChangeToState(_retreatCover);

        public override void OnDeath()
        {
            Machine.ForceChangeToState(_die);
            NavMesh.isStopped = true;
            Animator.SetTrigger("DIE");
            Animator.SetLayerWeight(2, 0);
            Animator.SetLayerWeight(3, 0);
            Animator.SetLayerWeight(4, 0);
            if (!IsPlayerInRange(15)) { ShoutDead(); }
        }

        public override void OnHeardCombat()
        {
            if (IsDead) return;
            _engagedTime = 50;
            _attackPoint = PlayerHeadPosition;
        }

        public override void OnHeardSteps()
        {
            if (IsDead) return;

            _suspisiusTime = 120;
            _interestPoint = PlayerGameObject.transform.position;
            _attackPoint = PlayerHeadPosition;
        }

        public override void OnHurt(float value)
        {
            if (IsDead) return;
            Machine.ForceChangeToState(_incapacitated);
            SetHealth(GetHealth() - value);

            Animator.SetTrigger("HURT");
            _hurtStopVelocityMultiplier = 0;
            _engagedTime = 50;
            _interestPoint = PlayerGameObject.transform.position;
            _attackPoint = PlayerHeadPosition;
        }

        public override void OnStart()
        {
            _cover = gameObject.AddComponent<AgentCoverSensor>();
            _weapon = gameObject.GetComponent<AgentFireWeapon>();
            _waypoints = gameObject.GetComponent<AgentWaypoints>();

            _movementAudio = gameObject.AddComponent<AudioSource>();
            _movementAudio.spatialBlend = 1;
            _movementAudio.playOnAwake = false;
            _movementAudio.outputAudioMixerGroup = AudioToolService.GetMixerGroup(AudioChannels.AGENT);
            _movementAudio.maxDistance = 15;
            _movementAudio.minDistance = 1;

            CreateStates();
            CreateTransitions();
            SetMaxHealth(100);
            SetHealth(100);

            StartCoroutine(BindWeapon());
        }

        public override void OnUpdate()
        {
            ManageStealth();
            ManageWeapon();

            Animator.SetLayerWeight(3, Mathf.SmoothDamp(Animator.GetLayerWeight(3), _desiredWeight, ref _desiredRefWeight, .25f));

            if (PlayerVisualDetected)
            {
                _interestPoint = PlayerGameObject.transform.position;
                _attackPoint = PlayerHeadPosition;
                _engagedTime = 50;
                _suspisiusTime = 150;
            }

            _hurtStopVelocityMultiplier = Mathf.Clamp(_hurtStopVelocityMultiplier + Time.deltaTime, 0, 1);
            NavMesh.speed = _desiredSpeed * _hurtStopVelocityMultiplier;

            ManageFootsteps();
        }

        public void RagdollBody()
        {
            Animator.enabled = false;
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
            }
        }

        public void ResetInvestigate()
        {
            _interestPoint = Vector3.zero;
        }

        public override void Restore()
        {
            base.Restore();

            Animator.enabled = true;
            NavMesh.isStopped = false;
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.GetComponent<Rigidbody>().isKinematic = true;
            }
            Machine.ForceChangeToState(_wander);
        }

        public override void RunOver(Vector3 velocity)
        {
            SetHealth(0);

            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
                body.Impulse(velocity);
            }
        }

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

        internal void DropWeapon()
        {
            _weapon.DropWeapon();
        }

        internal void Shout()
        {
            AudioSource.PlayClipAtPoint(_shouts[Random.Range(0, _shouts.Length)], Head.transform.position);
        }

        private IEnumerator BindWeapon()
        {
            yield return new WaitForEndOfFrame();
            _weapon.WeaponEngine.WeaponChangedState += OnWeaponChangeState;
        }

        private void CreateStates()
        {
            _enter = new(this);
            _wander = new(this);
            _report = new(this);
            _attack = new(this);
            _retreatCover = new(this);
            _die = new(this);
            _investigate = new(this); _incapacitated = new(this);
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

            Machine.AddTransition(_attack, _investigate, new FuncPredicate(() => !_isEngaged && _isSuspicius && _interestPoint != Vector3.zero));
            Machine.AddTransition(_investigate, _attack, new FuncPredicate(() => _isEngaged));
            Machine.AddTransition(_retreatCover, _investigate, new FuncPredicate(() => _retreatCover.Ready && !_isEngaged));
            Machine.AddTransition(_retreatCover, _attack, new FuncPredicate(() => _retreatCover.Ready && _isEngaged && AgentGlobalService.Instance.TryTakeAttackSlot(this)));
            Machine.AddTransition(_enter, _retreatCover, new FuncPredicate(() => _enter.Reached));
            Machine.AddTransition(_enter, _retreatCover, new FuncPredicate(() => _isEngaged && !AgentGlobalService.Instance.TryTakeAttackSlot(this)));
            Machine.AddTransition(_enter, _attack, new FuncPredicate(() => _isEngaged && AgentGlobalService.Instance.TryTakeAttackSlot(this)));

            Machine.AddTransition(_incapacitated, _retreatCover, new FuncPredicate(() => _incapacitated.Done && _isEngaged && !AgentGlobalService.Instance.TryTakeAttackSlot(this)));
            Machine.AddTransition(_incapacitated, _attack, new FuncPredicate(() => _incapacitated.Done && _isEngaged && AgentGlobalService.Instance.TryTakeAttackSlot(this)));

            Machine.SetState(_wander);
        }

        private IEnumerator IGoToPointSpawn()
        {
            yield return new WaitForEndOfFrame();

            Machine.ForceChangeToState(_enter);
            yield break;
        }

        private void ManageFootsteps()
        {
            _travelledDistance += NavMesh.velocity.sqrMagnitude * Time.deltaTime;

            if (_travelledDistance / (NavMesh.speed + 0.001f) > _footstepDistance)
            {
                PlayFootstep();
                _travelledDistance = 0;
            }
        }

        private void ManageStealth()
        {
            if (PlayerVisualDetected) { _engagedTime = 40; }

            _suspisiusTime = Mathf.Clamp(_suspisiusTime - Time.deltaTime, 0, float.MaxValue);
            _engagedTime = Mathf.Clamp(_engagedTime - Time.deltaTime, 0, float.MaxValue);
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

        private void PlayFootstep()
        {
            _movementAudio.PlayOneShot(_footsteps[Random.Range(0, _footsteps.Length)]);
        }

        private IEnumerator SetCrouch(bool state, float target)
        {
            _desiredSpeed = 0;
            Animator.SetBool("CROUCH", state);
            yield return new WaitForSeconds(1);
            _desiredSpeed = target;
        }

        public override void DrawGizmos()
        {
            base.DrawGizmos();
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_attackPoint, .125f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_interestPoint, .125f);
        }
        public void ShoutPain() => AudioToolService.PlayClipAtPoint(_painScream.GetRandom(), transform.position + Vector3.one, 1, AudioChannels.AGENT, 10);
        public void ShoutDead() => AudioToolService.PlayClipAtPoint(_deadScream.GetRandom(), transform.position + Vector3.one, 1, AudioChannels.AGENT, 50);
    }

    public class SoldierAttackState : BaseState
    {
        private bool _checkedMovementType;

        private float _cooldown;

        private SpatialDataPoint _destination;

        private float _evaluatinInterval;

        private float _lastEvaluationTime;

        private SpatialDataPoint _nearestAggresive;

        private SoldierAgentController _observer;

        public SoldierAttackState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

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
            _observer.SetArmsLayerWeight(1);
            //_observer.Animator.SetLayerWeight(3, 1);
            //_observer.Animator.SetLayerWeight(2, 1);
            //_observer.Animator.SetLayerWeight(1, 1);
        }

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

            _observer.SetLookTarget(_observer.AttackPoint);

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

        private void GeneratePoints()
        {
            Vector3 combatPoint = Vector3.Lerp(_observer.transform.position, _observer.AttackPoint, 0.5f);

            SpatialDataPoint[] points = _observer.CoverSensor.GetCombatSpatialData(combatPoint, _observer.AttackPoint, _observer.DetectionRange, Random.Range(3f, 10f)).ToArray();

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
                _observer.SetTarget(_observer.AttackPoint - (_observer.AttackPoint - _observer.transform.position).normalized * 6f);
            }
        }
    }

    public class SoldierDieState : BaseState
    {
        private SoldierAgentController _observer;

        private float _waitForRagdollTime = 0;
        private float _time;
        private bool _done;

        public SoldierDieState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

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
            _time = 0;
            _done = false;
        }

        public override void Update()
        {
            if (_done) return;
            _time += Time.deltaTime;
            if (_time > _waitForRagdollTime)
            {
                _done = true;
                _observer.RagdollBody();
            }
        }
    }

    public class SoldierEnterState : BaseState
    {
        private float _enterTime = 0;

        private SoldierAgentController _observer;

        private bool _waited;

        private float _waitTime = 1000;

        public SoldierEnterState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public bool Reached => Vector3.Distance(_observer.transform.position, _observer.EnterPoint) < 1 && _waited;

        public override void DrawGizmos()
        {
            Gizmos.DrawCube(_observer.EnterPoint, Vector3.one);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.SetTarget(_observer.EnterPoint);
            _observer.FaceTarget = false;
            _observer.SetMovementType(SoldierMovementType.RUN);
            _enterTime = Time.time;
        }

        public override void Update()
        {
            if (_waited) return;
            if (Time.time - _enterTime > _waitTime) { _waited = true; }
        }
    }

    public class SoldierPatrolState : BaseState
    {
        private Waypoint _currentWaypoint;

        private float _lastWaitTime;

        private SoldierAgentController _observer;

        private Vector3 _targetPos;

        private float _waitTime;

        public SoldierPatrolState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(_targetPos, 0.33f);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.SetMovementType(SoldierMovementType.WALK);
            _observer.Animator.SetBool("WARNING", true);
            //_observer.Animator.SetLayerWeight(3, 0);
            //_observer.Animator.SetLayerWeight(2, 0);
            _observer.SetArmsLayerWeight(1);
            _currentWaypoint = _observer.Waypoints.CurrentWaypoint;
            if (FindNewTarget()) _observer.SetTarget(_targetPos);
            _observer.FaceTarget = false;
        }

        public override void Update()
        {
            if (ReachedTarget())
            {
                _waitTime += Time.deltaTime;

                if (_waitTime > (!_observer.UseWaypoints ? 5 : _currentWaypoint.WaitTime))
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

    public class SoldierReportState : BaseState
    {
        private bool _hasReported;

        private SoldierAgentController _observer;

        private float _time;

        public SoldierReportState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

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

    public class SoldierRetreatCoverState : BaseState
    {
        public bool _reloaded;
        private Vector3 _destination;
        private bool _hasReached;
        private float _lastFindCover;
        private SoldierAgentController _observer;
        private float _relaxTime;
        private float _timeToExit;

        public SoldierRetreatCoverState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public bool Ready => _hasReached && _reloaded && _timeToExit > _relaxTime;
        private bool _reached => Vector3.Distance(_observer.transform.position, _destination) < 2f;

        public override void DrawGizmos()
        {
            Gizmos.DrawWireSphere(_destination, 1f);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.SetArmsLayerWeight(1);
            _observer.SetMovementType(SoldierMovementType.RUN);
            //_observer.Animator.SetLayerWeight(3, 1);

            _observer.FaceTarget = false;
            _hasReached = false;
            _reloaded = false;
            _observer.AllowFire(false);
            _relaxTime = Random.Range(3, 5);
            _timeToExit = 0f;
            FindCover();
        }

        public override void Update()
        {
            if (_observer.Weapon.WeaponEngine.CurrentAmmo > 0)
            {
                _observer.FaceTarget = true;
                //_observer.Animator.SetLayerWeight(3, 1);
                _observer.AllowFire(_observer.PlayerVisualDetected);
                _observer.SetLookTarget(_observer.AttackPoint);
                _observer.SetMovementType(SoldierMovementType.WALK);
            }
            else
            {
                _observer.SetMovementType(SoldierMovementType.RUN);
                //_observer.Animator.SetLayerWeight(3, 0);
                _observer.FaceTarget = false;
                _observer.AllowFire(false);
            }

            if (_observer.IsPlayerInRange(10) && _observer.IsPlayerVisible() && _reloaded) _observer.ForceAttack();

            if (_observer.PlayerVisualDetected && Time.time - _lastFindCover > 5)
            {
                _observer.FaceTarget = false;
                _observer.AllowFire(false);
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

        private void FindCover()
        {
            _lastFindCover = Time.time;
            Vector3 _combatPoint = _observer.transform.position;

            SpatialDataPoint[] points = _observer.CoverSensor.GetCombatSpatialData(_combatPoint, _observer.AttackPoint, 10).ToArray();

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
    }

    public class SoldierSearchAlertState : BaseState
    {
        private SoldierAgentController _observer;

        private Vector3 _targetPos;

        public SoldierSearchAlertState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

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
            //  _observer.Animator.SetLayerWeight(3, 0);
            _observer.SetTarget(_observer.InterestPoint);
            _observer.FaceTarget = false;
        }

        public override void Update()
        {
            if (ReachedTarget())
            {
                _observer.SetMovementType(SoldierMovementType.WALK);
                //_observer.ResetInvestigate();
            }
        }

        private bool ReachedTarget()
        {
            Vector3 pos = _observer.InterestPoint;
            pos.y = _observer.transform.position.y;
            return Vector3.Distance(_observer.transform.position, pos) < 1.5f;
        }
    }

    public class SoldierIncapacitatedState : BaseState
    {
        private SoldierAgentController _observer;

        private float _waitFor = 2.65f;
        private float _time;
        private bool _done;

        public SoldierIncapacitatedState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public bool Done { get => _done; }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
            _observer.HealthChangedEvent -= Hurt;
        }

        public override void Start()
        {
            _observer.HealthChangedEvent += Hurt;
            Hurt(0);
        }

        private void Hurt(float ignore)
        {
            _observer.AllowFire(false);
            _time = 0;
            _done = false;
            _observer.NavMesh.isStopped = true;
            _observer.Animator.SetFloat("INCAP_ANIM", Random.Range(0f, 1f));
            _observer.Animator.SetTrigger("INCAP");
            _observer.Animator.SetLayerWeight(2, 0);
            _observer.SetArmsLayerWeight(0.05f);
            if (_observer.IsPlayerInRange(15)) { _observer.ShoutPain(); }
        }

        public override void Update()
        {
            if (_done) return;
            _time += Time.deltaTime;
            if (_time > _waitFor)
            {
                Recover();
            }
        }

        private void Recover()
        {
            _observer.Animator.SetLayerWeight(2, 1);
            _observer.SetArmsLayerWeight(1);
            _observer.NavMesh.isStopped = false;
            _done = true;
        }
    }
}