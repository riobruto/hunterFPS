using Core.Engine;
using Game.Entities;
using Game.Life;
using Game.Life.Entities;
using Game.Life.WaypointPath;
using Game.Player.Sound;
using Game.Player.Weapon;
using Game.Service;
using Life.StateMachines;
using Life.StateMachines.Interfaces;
using Nomnom.RaycastVisualization;
using Player.Weapon.Interfaces;
using System;
using System.Collections;

using System.Linq;

#if UNITY_EDITOR

using UnityEditor;

#endif

using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Life.Controllers
{
    public enum SoldierMovementType
    {
        RUN,
        WALK,
        PATROL,
        CROUCH
    }

    public enum SoldierType
    {
        ASSAULT,
        SHOTGUNNER,
        SNIPER,
        HEAVY
    }

    public delegate void SoldierDelegate(SoldierAgentController csoldier);

    public delegate void SoldierGrenadeDelegate(SoldierAgentController csoldier, Vector3 grenadeTarget);

    public class SoldierAgentController : AgentController
    {
        [Header("Sounds")]
        [SerializeField] private AudioClipGroup _footsteps;

        [SerializeField] private AudioClip[] _shouts;
        [SerializeField] private AudioClipGroup _painScream;
        [SerializeField] private AudioClipGroup _deadScream;
        [SerializeField] private AudioClipGroup _coverCall;
        [SerializeField] private AudioClipGroup _attackCall;
        [SerializeField] private AudioClipGroup _searchCall;

        [Header("Movement")]
        [SerializeField] private bool _useWaypoints;

        [SerializeField] private float _footstepDistance;
        [SerializeField] private float _crouchSpeed = 1f;
        [SerializeField] private float _patrolSpeed = 1f;
        [SerializeField] private float _runSpeed = 4.5f;
        [SerializeField] private float _walkSpeed = 2.5f;

        private Vector3 _attackPoint;

        private SoldierMovementType _current;

        private float _desiredSpeed;
        private float _engagedTime;
        private Vector3 _enterPoint;
        private float _hurtStopVelocityMultiplier;
        private Vector3 _interestPoint;

        private AudioSource _movementAudio;

        private float _suspisiusTime;
        private float _travelledDistance;
        private bool _hasNearThreat;

        [Header("Cover Values")]
        [SerializeField] private bool _canShootFromCover;

        [Header("Combat Values")]
        [SerializeField] private SoldierType _soldierType;

        [Range(1, 100)]
        private float _minAttackDesiredRange;

        [Range(10, 100)]
        private float _maxAttackDesiredRange;

        [Range(0, 1)]
        private float _boldness;

        [Header("Grenade")]
        [SerializeField] private float _minDistanceGrenade = 4;

        [SerializeField] private GameObject _granade;

        //STATES
        private SoldierReportState _report;

        private SoldierGoToPlayer _goToPlayer;
        private SoldierEnterState _enter;
        private SoldierDieState _die;
        private SoldierEngagePlayerState _attack;
        private SoldierRetreatCoverState _retreatCover;
        private SoldierActBusyState _actBusy;
        private SoldierNearThreatAttackState _nearThreat;

        private AgentWaypoints _waypoints;
        private AgentFireWeapon _weapon;
        private AgentCoverSensor _cover;
        private float _desiredWeight;
        private float _desiredRefWeight;

        //Events
        public event SoldierDelegate TakingCoverEvent;

        public event SoldierDelegate FlankingEvent;

        public event SoldierDelegate TakingDamageEvent;

        public event SoldierDelegate AdvancingAttackEvent;

        public event SoldierDelegate SearchingPlayerEvent;

        public event SoldierGrenadeDelegate ThrowGrenadeEvent;

        //Squad
        private SoldierSquad _currentSquad;

        private bool _hasSquad => _currentSquad != null;

        public void SetSquad(SoldierSquad squad)
        {
            if (squad == null && _hasSquad)
            {
                //TODO: DUMP SQUAD EVENTS HERE!
                _currentSquad.SquadMemberSawPlayer -= OnSquadSawPlayer;
                _currentSquad.SquadMemberThrowGranadeToPlayer -= OnSquadThrownGrenade;
                _currentSquad = null;
                return;
            }

            _currentSquad = squad;
            squad.SquadMemberSawPlayer += OnSquadSawPlayer;
            squad.SquadMemberThrowGranadeToPlayer += OnSquadThrownGrenade;
            //TODO: SQUAD VALUES AND EVENTS
        }

        private void OnSquadThrownGrenade(SoldierAgentController soldier, Vector3 target)
        {
            if (soldier == this) return;
            if (Vector3.Distance(target, transform.position) < _grenadeSafeDistance)
            {
                _grenadePosition = target;
                Machine.ForceChangeToState(_grenadeCover);
            }
        }

        private void OnSquadSawPlayer(SoldierAgentController soldier)
        {
            _attackPoint = PlayerHeadPosition;
        }

        public Vector3 AttackPoint { get => _attackPoint; }
        public AgentCoverSensor CoverSensor => _cover;
        public Vector3 EnterPoint { get => _enterPoint; }
        public Vector3 InterestPoint { get => _interestPoint; }
        public bool UseWaypoints => _useWaypoints;
        public AgentWaypoints Waypoints => _waypoints;
        public AgentFireWeapon Weapon => _weapon;

        public SoldierSquad Squad => _currentSquad;

        public void SetAimLayerWeight(float target)
        {
            _desiredWeight = target;
        }

        private bool _incapacitated;

        //TODO: CREATE RELEASES FOR ATTACK SLOTS
        public void SetAllowFire(bool state) => Weapon.AllowFire = state;

        public void SetAllowReload(bool state) => Weapon.AllowReload = state;

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

        public override void OnDeath()
        {
            AllowThinking(false);

            Machine.ForceChangeToState(_die);

            NavMeshAgent.isStopped = true;

            Animator.SetTrigger("DIE");
            Animator.SetLayerWeight(2, 0);
            Animator.SetLayerWeight(3, 0);
            Animator.SetLayerWeight(4, 0);
            CurrentCoverSpot = null;

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

        public bool WaitToRagdoll;

        public override void OnHurt(AgentHurtPayload payload)
        {
            if (IsDead) return;
            SetHealth(GetHealth() - payload.Amount);
            if (GetHealth() <= 0) return;
            if (payload.HurtByPlayer) TakingDamageEvent?.Invoke(this);

            _hurtStopVelocityMultiplier = 0;
            _engagedTime = 50;
            _interestPoint = PlayerGameObject.transform.position;
            _attackPoint = PlayerHeadPosition;

            if (_currentSquad != null) _currentSquad.ReleaseAttackSlot(this);

            Animator.SetTrigger("HURT");
            Animator.SetFloat("INCAP_ANIM", Random.Range(0f, 1f));
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
            _cover.DetectionRadius = 10;

            CreateStates();
            CreateTransitions();

            float health = 100;
            if (_soldierType == SoldierType.HEAVY) health = 175;

            SetMaxHealth(health);
            SetHealth(health);
            StartCoroutine(BindWeapon());
            SetAllowReload(true);

            if (!_hasSquad) AgentGlobalSystem.GiveSquadToAgent(this);

            Machine.ChangeStateEvent += OnStateChangedFromMachine;
        }

        private void OnStateChangedFromMachine(IState current, IState next)
        {
            if (next == _retreatCover)
            {
                TakingCoverEvent?.Invoke(this);
                AudioSource.PlayClipAtPoint(_coverCall.GetRandom(), Head.transform.position);
            }
            if (next == _attack)
            {
                AudioSource.PlayClipAtPoint(_attackCall.GetRandom(), Head.transform.position);
                AdvancingAttackEvent?.Invoke(this);
            }
            if (next == _goToPlayer)
            {
                AudioSource.PlayClipAtPoint(_searchCall.GetRandom(), Head.transform.position);
                SearchingPlayerEvent?.Invoke(this);
            }
        }

        public override void OnUpdate()
        {
            ManageHurtSlowdown();
            ManageNearThreat();
            ManageStealth();
            ManageContact();

            _weapon.SetFireTarget(_attackPoint);

            if (_hurtStopVelocityMultiplier > 1)
            {
                Animator.SetLayerWeight(2, Mathf.SmoothDamp(Animator.GetLayerWeight(2), _desiredWeight, ref _desiredRefWeight, .25f));
            }

            ManageFootsteps();
        }

        private void ManageNearThreat()
        {
            _nearThreatAgent = FindNearThreat();
            _hasNearThreat = _nearThreatAgent != null;
            if (_hasNearThreat)
            {
                _attackPoint = _nearThreatAgent.Head.position;
            }
        }

        private void ManageHurtSlowdown()
        {
            _hurtStopVelocityMultiplier = Mathf.Clamp(_hurtStopVelocityMultiplier + Time.deltaTime, -10, 1);
            NavMeshAgent.speed = _desiredSpeed * Mathf.Clamp01(_hurtStopVelocityMultiplier);
        }

        private void ManageContact()
        {
            if (!HasPlayerVisual)
            {
                if (_currentSquad != null) _currentSquad.ReleaseAttackSlot(this);
            }

            if (HasPlayerVisual && !_hasNearThreat)
            {
                if (_currentSquad != null && _currentSquad.HasEngageTimeout)
                {
                    _currentSquad.TakeAttackSlotForce(this);
                }

                _interestPoint = PlayerGameObject.transform.position;
                _attackPoint = PlayerHeadPosition;
                _engagedTime = 50;
                _suspisiusTime = 150;
            }
        }

        private AgentController _nearThreatAgent;
        public AgentController NearThreatAgent => _nearThreatAgent;

        private AgentController FindNearThreat()
        {
            AgentController[] agents = AgentGlobalService.Instance.ActiveAgents.ToArray();
            if (agents.Length == 0) { return null; }
            agents = agents.Where(x => x.AgentGroup == AgentGroup.MONSTER).ToArray();
            if (agents.Length == 0) { return null; }
            AgentController result = agents.OrderBy(x => Vector3.Distance(x.transform.position, transform.position)).ToArray()[0];
            if (Vector3.Distance(result.transform.position, transform.position) > 3) { return null; }
            return result;
        }

        public void RagdollBody()
        {
            Animator.enabled = false;
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
            }
        }

        public override void Restore()
        {
            base.Restore();

            Animator.enabled = true;
            NavMeshAgent.isStopped = false;
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.GetComponent<Rigidbody>().isKinematic = true;
            }
            Machine.ForceChangeToState(_actBusy);
        }

        //this method will be called if any body detects damage over the gethealth() and push all the rigidbodies
        public override void KillAndPush(Vector3 velocity)
        {
            if (IsDead) return;
            SetHealth(0);
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
            }
            StartCoroutine(IPush(velocity, null));
        }

        public override void KillAndPush(Vector3 velocity, LimbHitbox hitbox)
        {
            if (IsDead) return;
            SetHealth(0);
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
            }
            StartCoroutine(IPush(velocity, hitbox));
        }

        public IEnumerator IPush(Vector3 velocity, LimbHitbox specificHitbox)
        {
            yield return new WaitForEndOfFrame();

            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                if (specificHitbox == null) body.Impulse(velocity);
                else if (body == specificHitbox) body.Impulse(velocity);
            }
            yield return null;
        }

        public void SetMovementType(SoldierMovementType type)
        {
            if (type == _current) return;

            switch (type)
            {
                case SoldierMovementType.RUN:

                    SetAimLayerWeight(0);

                    if (_current == SoldierMovementType.CROUCH)
                    {
                        StartCoroutine(SetCrouch(false, _runSpeed));
                        Animator.SetBool("RUN", true);
                        break;
                    }

                    Animator.SetBool("RUN", true);
                    _desiredSpeed = _runSpeed;
                    break;

                case SoldierMovementType.WALK:

                    SetAimLayerWeight(1);

                    if (_current == SoldierMovementType.CROUCH)
                    {
                        StartCoroutine(SetCrouch(false, _walkSpeed));
                        Animator.SetBool("RUN", false);
                        break;
                    }

                    Animator.SetBool("RUN", false);
                    _desiredSpeed = _walkSpeed;
                    break;

                case SoldierMovementType.PATROL:
                    Debug.LogError("NO IMPLEMENTADO PATROL MOVEMENT!");
                    break;

                case SoldierMovementType.CROUCH:

                    StartCoroutine(SetCrouch(true, _crouchSpeed));
                    Animator.SetBool("RUN", false);
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
            _actBusy = new(this);
            _report = new(this);
            _attack = new(this);
            _retreatCover = new(this);
            _die = new(this);
            _goToPlayer = new(this);
            _nearThreat = new(this);
            _grenadeCover = new(this);
        }

        private void CreateTransitions()
        {
            //TODO: MEJORAR FLAGS DE DETECCION, ESTAN MUY INCONSISTENTES

            //Definir mejor las transiciones ya que crean bucles

            //suspect,detect,report, asuntos separados)como la iglesia y el estado)))

            Machine.AddTransition(_retreatCover, _goToPlayer, new FuncPredicate(() => ShouldSearchForThePlayer));
            Machine.AddTransition(_retreatCover, _attack, new FuncPredicate(() => ShouldEngageThePlayer));

            Machine.AddTransition(
                _actBusy, _attack,
                new FuncPredicate(() => ShouldEngageThePlayer));

            Machine.AddTransition(_retreatCover, _actBusy, new FuncPredicate(() => false));

            Machine.AddTransition(_attack, _goToPlayer, new FuncPredicate(() => ShouldSearchForThePlayer));
            Machine.AddTransition(_attack, _retreatCover, new FuncPredicate(() => ShouldCoverFromThePlayer));

            Machine.AddTransition(_goToPlayer, _attack, new FuncPredicate(() => ShouldEngageThePlayer));
            Machine.AddTransition(_goToPlayer, _retreatCover, new FuncPredicate(() => ShouldCoverFromThePlayer));

            Machine.AddTransition(_grenadeCover, _attack, new FuncPredicate(() => _grenadeCover.Safe && ShouldEngageThePlayer));
            Machine.AddTransition(_grenadeCover, _retreatCover, new FuncPredicate(() => _grenadeCover.Safe && ShouldCoverFromThePlayer));

            Machine.AddTransition(_actBusy, _attack, new FuncPredicate(() => ShouldEngageThePlayer));
            Machine.AddTransition(_actBusy, _retreatCover, new FuncPredicate(() => ShouldCoverFromThePlayer));

            Machine.SetState(_actBusy);
        }

        private IEnumerator IGoToPointSpawn()
        {
            yield return new WaitForEndOfFrame();

            Machine.ForceChangeToState(_enter);
            yield break;
        }

        private void ManageStealth()
        {
            if (HasPlayerVisual) { _engagedTime = 40; }

            _suspisiusTime = Mathf.Clamp(_suspisiusTime - Time.deltaTime, 0, float.MaxValue);
            _engagedTime = Mathf.Clamp(_engagedTime - Time.deltaTime, 0, float.MaxValue);
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

        private IEnumerator SetCrouch(bool state, float target)
        {
            _desiredSpeed = 0;
            Animator.SetBool("CROUCH", state);
            yield return new WaitForSeconds(.5f);
            _desiredSpeed = target;
        }

        private void PlayFootstep()
        {
            _movementAudio.PlayOneShot(_footsteps.GetRandom());
        }

        private void ManageFootsteps()
        {
            _travelledDistance += NavMeshAgent.velocity.sqrMagnitude * Time.deltaTime;

            if (_travelledDistance / (NavMeshAgent.speed + 0.001f) > _footstepDistance)
            {
                PlayFootstep();
                _travelledDistance = 0;
            }
        }

        public void ShoutPain() => AudioToolService.PlayClipAtPoint(_painScream.GetRandom(), transform.position + Vector3.one, 1, AudioChannels.AGENT, 10);

        public void ShoutDead() => AudioToolService.PlayClipAtPoint(_deadScream.GetRandom(), transform.position + Vector3.one, 1, AudioChannels.AGENT, 50);

        [ContextMenu("Force Attack")]
        public void ForceAttack()
        {
            if (!_currentSquad.TryTakeAttackSlot(this))
            {
                ForceRetreatToCover();
                return;
            }
            _attackPoint = PlayerHeadPosition;
            if (_currentSquad != null) _currentSquad.ForceEngage(1000000);
        }

        [ContextMenu("Force Cover")]
        public void ForceRetreatToCover() => Machine.ForceChangeToState(_retreatCover);

        public void ForceActBusy()
        { }

        public bool ShouldEngageThePlayer
        {
            get
            {
                if (Weapon.Empty)
                {
                    return false;
                }
                if (_currentSquad != null)
                {
                    if (_currentSquad.HasEngageTimeout) return false;
                    return _currentSquad.TryTakeAttackSlot(this);
                }
                return false;
            }
        }

        public bool ShouldSearchForThePlayer
        {
            get
            {
                if (Weapon.Empty) return false;
                if (HasPlayerVisual) return false;
                if (_currentSquad == null) return true;
                if (_currentSquad.HasEngageTimeout && _currentSquad.TryTakeAttackSlot(this))
                {
                    return true;
                }
                return false;
            }
        }

        public bool ShouldCoverFromThePlayer
        {
            get
            {
                if (Weapon.Empty && _soldierType != SoldierType.SHOTGUNNER) return true;
                if (_currentSquad != null)
                {
                    return !_currentSquad.TryTakeAttackSlot(this);
                }
                return false;
            }
        }

        public bool ShouldFlankThePlayer
        {
            get
            {
                //tengo municion?
                //ya hay otros enemigos flanqueando?

                return false;
            }
        }

        public bool ShouldThrowGranadeToThePlayer
        {
            get
            {
                //tengo granadas?
                //hace poco que tiraron una granada?
                //hay otros enemigos tirando una granada?
                //hay radio para la granada?

                return false;
            }
        }

        public bool ShouldCoverFromGrenade
        {
            get
            {
                //hay una granada cerca?
                //hay covertura de la granada cerca?
                return false;
            }
        }

        public bool ShouldMove
        {
            get
            {
                return true;
            }
        }

        private float _recieveKickCooldown = 0.1f;
        private float _lastRecieverkKickTime = 0;

        public override void Kick(Vector3 position, Vector3 direction, float damage)
        {
            //prevent multiples hitboxes being kicked
            if (Time.time - _lastRecieverkKickTime < _recieveKickCooldown) return;
            _lastRecieverkKickTime = Time.time;
            Damage(damage);
        }

        private int _queryAmount;

        public SpatialDataQuery LastSpatialDataQuery
        {
            get { return _lastSpatialDataQuery; }
            private set
            {
                _queryAmount++;
                _lastSpatialDataQuery = value;
            }
        }

        private SpatialDataQuery _lastSpatialDataQuery;

        [SerializeField] private bool _debugSpatialData;

        public SoldierType SoldierType { get => _soldierType; }

        public CoverSpotEntity CurrentCoverSpot
        {
            get { return _currentCoverSpot; }
            set
            {
                if (value == null && _currentCoverSpot != null)
                {
                    _currentCoverSpot.Release(this);
                    _currentCoverSpot = null;
                    return;
                }
                _currentCoverSpot = value;
            }
        }

        private CoverSpotEntity _currentCoverSpot;

        public Vector3 FindCoverFromPlayer(bool mantainVisual)
        {
            CurrentCoverSpot = null;

            CoverSpotQuery cpQuery = new CoverSpotQuery(this);
            if (cpQuery.CoverSpots.Length > 0)
            {
                foreach (CoverSpotEntity ent in cpQuery.CoverSpots)
                {
                    if (ent.TryTake(this, PlayerHeadPosition))
                    {
                        CurrentCoverSpot = ent;
                        return CurrentCoverSpot.transform.position;
                    }
                }
            }

            Vector3 center = PlayerPosition;
            if (_currentSquad != null && _currentSquad.ShouldHoldPlayer)
            {
                center = _currentSquad.HoldPosition;
            }

            SpatialDataQuery query = new SpatialDataQuery(new SpatialQueryPrefs(this, center, PlayerHeadPosition, 2f));
            LastSpatialDataQuery = query;

            if (query.WallCoverPoints.Count > 0) { return query.WallCoverPoints[0].Position; }
            if (query.SafePoints.Count > 0) { return query.SafePoints[0].Position; }
            if (query.WallCoverCrouchedPoints.Count > 0) { return query.WallCoverCrouchedPoints[0].Position; }
            if (query.SafeCrouchPoints.Count > 0) { return query.SafeCrouchPoints[0].Position; }
            else return query.AllPoints[query.AllPoints.Count].Position;
        }

        public Vector3 FindAgressivePosition(bool mantainVisual)
        {
            //buscar el punto en el jugador o el el player o en el punto medio? el punto medio deberia tener un limite de distancia, ya que despues todos los puntos quedan invalidos
            //find first cover spots
            switch (_soldierType)
            {
                case SoldierType.HEAVY:
                case SoldierType.SNIPER:
                case SoldierType.ASSAULT:
                    return AssaultAttackPoint();

                case SoldierType.SHOTGUNNER:
                    //shotgunners are very agressive and will flush the player, le ponemos que no busquen ataques cubiertos si no que den cara de cerca.
                    SpatialDataQuery shotquery = new SpatialDataQuery(new SpatialQueryPrefs(this, PlayerGameObject.transform.position, PlayerHeadPosition, 5f));
                    LastSpatialDataQuery = shotquery;
                    if (shotquery.WallCoverCrouchedPoints.Count > 0) { return shotquery.WallCoverCrouchedPoints[0].Position; }
                    if (shotquery.SafeCrouchPoints.Count > 0) { return shotquery.SafeCrouchPoints[0].Position; }
                    if (shotquery.UnsafePoints.Count > 0) return shotquery.UnsafePoints[0].Position;

                    return PlayerHeadPosition + (transform.position - PlayerHeadPosition).normalized * 5f;

                default:
                    return transform.position;
            }
        }

        private Vector3 AssaultAttackPoint()
        {
            CurrentCoverSpot = null;
            CoverSpotQuery cpQuery = new CoverSpotQuery(this);
            if (cpQuery.CoverSpots.Length > 0)
            {
                foreach (CoverSpotEntity ent in cpQuery.CoverSpots)
                {
                    if (!ent.HasVisibility(PlayerHeadPosition)) continue;
                    if (ent.TryTake(this, PlayerHeadPosition))
                    {
                        CurrentCoverSpot = ent;
                        return CurrentCoverSpot.transform.position;
                    }
                }
            }

            //si no hay cover points, fallbackear a esto!
            //find first cover spots
            Vector3 center = PlayerPosition;
            if (_currentSquad != null && _currentSquad.ShouldHoldPlayer)
            {
                center = _currentSquad.HoldPosition;
            }

            SpatialDataQuery query = new SpatialDataQuery(new SpatialQueryPrefs(this, center, PlayerHeadPosition, 5f));
            LastSpatialDataQuery = query;
            if (query.WallCoverCrouchedPoints.Count > 0) { return query.WallCoverCrouchedPoints[0].Position; }
            if (query.SafeCrouchPoints.Count > 0) { return query.SafeCrouchPoints[0].Position; }
            if (query.UnsafePoints.Count > 0) return query.UnsafePoints[0].Position;

            if (_currentSquad != null)
            {
                _currentSquad.ReleaseAttackSlot(this);
            }

            return PlayerHeadPosition + (transform.position - PlayerHeadPosition).normalized * 5f;
        }

        private Vector3 CalculateGrenadeThrowVector(Vector3 start, Vector3 target)
        {
            float displacementY = target.y - start.y;
            Vector3 displacementXZ = new Vector3(target.x - start.x, 0, target.z - start.z);
            float height = 2.5f;
            float gravity = Physics.gravity.y;
            float time = Mathf.Sqrt(-2 * height / gravity) + Mathf.Sqrt(2 * (displacementY - height) / gravity);
            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
            Vector3 velocityXZ = displacementXZ / time;

            //??? no es suficiente velocitdad
            return velocityY + velocityXZ;
        }

        private Vector3 _grenadePosition;
        private SoldierCoverFromGrenadeState _grenadeCover;

        private float _grenadeSafeDistance = 7;
        private float _maxDistanceGrenade = 15f;

        public SpatialDataPoint FindCoverFromGrenade()
        {
            SpatialDataQuery query = new SpatialDataQuery(new SpatialQueryPrefs(this, transform.position, _grenadePosition, _grenadeSafeDistance));
            LastSpatialDataQuery = query;
            return query.AllPoints[query.AllPoints.Count - 1];
        }

        public bool TryThrowGrenade()
        {
            if (_soldierType != SoldierType.ASSAULT) return false;

            if (Vector3.Distance(AttackPoint, transform.position) < _minDistanceGrenade) return false;
            if (Vector3.Distance(AttackPoint, transform.position) > _maxDistanceGrenade) return false;

            if (PlayerOccluderGameObject != null)
            {
                if (Vector3.Distance(PlayerOccluderPosition, transform.position) < _minDistanceGrenade) return false;
                if (PlayerOccluderGameObject.layer == gameObject.layer) return false;
            }

            if (_currentSquad != null)
            {
                if (!_currentSquad.CanThrowGrenade) return false;
                else _currentSquad.ReleaseAttackSlot(this);
            }

            StartCoroutine(ThrowGrenade());

            //throw grende
            //coroutine delay

            return true;
        }

        private IEnumerator ThrowGrenade()
        {
            Animator.SetTrigger("GRENADE");
            _desiredSpeed = 0;
            ThrowGrenadeEvent?.Invoke(this, AttackPoint);
            //we stop thinking so it maintains location and state
            AllowThinking(false);
            SetAllowFire(false);
            SetAllowReload(false);
            //create shout/call
            Vector3 instancePos = transform.position + Vector3.up * 1.60f + transform.forward * .5f + transform.right * .5f;

            if (IsDead)
            {// prevent enemy dying and throwing grenade
                GameObject GOgrenade = Instantiate(_granade);
                GOgrenade.transform.position = instancePos;
                IGrenade grenade = GOgrenade.GetComponent<IGrenade>();
                grenade.Rigidbody.AddTorque(Random.insideUnitSphere);
                grenade.Trigger(3);

                yield break;
            }

            yield return new WaitForSeconds(1.25f);
            {
                GameObject GOgrenade = Instantiate(_granade);
                GOgrenade.transform.position = instancePos;
                IGrenade grenade = GOgrenade.GetComponent<IGrenade>();
                grenade.Trigger(3);
                grenade.Rigidbody.AddForce(CalculateGrenadeThrowVector(GOgrenade.transform.position, AttackPoint), ForceMode.VelocityChange);
                grenade.Rigidbody.AddTorque(Random.insideUnitSphere);
                AllowThinking(true);
                SetAllowReload(true);
                yield return null;
            }
        }

        public override void DrawGizmos()
        {
            Debug.DrawRay(transform.position + Vector3.up * 1.60f + transform.forward * .5f, CalculateGrenadeThrowVector(transform.position + Vector3.up * 1.60f + transform.forward * .5f, PlayerPosition).normalized, Color.red);
            base.DrawGizmos();
            if (_debugSpatialData && LastSpatialDataQuery != null)
            {
#if UNITY_EDITOR
                Handles.Label(transform.position + transform.up * 2,
                    $"Querys Done:{_queryAmount}" +
                    $"Points: {LastSpatialDataQuery.AllPoints.Count}" +
                    $"SafePoints: {LastSpatialDataQuery.SafePoints.Count}" +
                    $"SafeCrouchedPoints: {LastSpatialDataQuery.SafeCrouchPoints.Count}" +
                    $"UnsafePoints: {LastSpatialDataQuery.UnsafePoints.Count}");
#endif
                foreach (SpatialDataPoint point in LastSpatialDataQuery.UnsafePoints) { Gizmos.color = Color.red; Gizmos.DrawCube(point.Position, Vector3.one * .125f); }
                foreach (SpatialDataPoint point in LastSpatialDataQuery.SafePoints) { Gizmos.color = Color.green; Gizmos.DrawCube(point.Position, Vector3.one * .125f); }
                foreach (SpatialDataPoint point in LastSpatialDataQuery.SafeCrouchPoints) { Gizmos.color = Color.yellow; Gizmos.DrawCube(point.Position, Vector3.one * .125f); }
                foreach (SpatialDataPoint point in LastSpatialDataQuery.WallCoverCrouchedPoints) { Gizmos.color = Color.yellow + Color.red; Gizmos.DrawCube(point.Position, Vector3.one * .125f); }
                foreach (SpatialDataPoint point in LastSpatialDataQuery.WallCoverPoints) { Gizmos.color = Color.blue; Gizmos.DrawCube(point.Position, Vector3.one * .125f); }
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_attackPoint, .125f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_interestPoint, .125f);
            Gizmos.color = Color.blue;
        }

        public enum SquadCallType
        {
            COVER,
            CONTACT,
            LOSTCONTACT,
            SEARCH,
        }

        internal void SquadCall(SquadCallType type)
        {
            switch (type)
            {
                case SquadCallType.COVER:
                    break;

                case SquadCallType.CONTACT:
                    break;

                case SquadCallType.LOSTCONTACT:
                    break;

                case SquadCallType.SEARCH:
                    break;
            }
        }
    }

    public class SoldierCoverFromGrenadeState : BaseState
    {
        private SoldierAgentController _soldier;

        public SoldierCoverFromGrenadeState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public bool Safe => Vector3.Distance(_targetPosition, _soldier.transform.position) < 1f;

        private Vector3 _targetPosition;

        public override void Start()
        {
            _targetPosition = _soldier.FindCoverFromGrenade().Position;
            _soldier.SetTarget(_targetPosition);
            _soldier.SetMovementType(SoldierMovementType.RUN);
            _soldier.FaceTarget = false;
            _soldier.SetAllowFire(false);
        }

        public override void Update()
        {
        }
    }

    public class SoldierEngagePlayerState : BaseState
    {
        private Vector3 _destination;
        private SoldierAgentController _soldier;

        //creates a timespan so shooting is not inmediate
        public float _waitForChase;

        private float _reactionTime;
        private float _chaseWaitTime = .5f;

        //creates a timeout for not wanting to shoot anymore
        private float _engageTimeOut;

        private float _shotgunnerMoveTime;

        public SoldierEngagePlayerState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _reactionTime = Time.time;
            _engageTimeOut = Random.Range(4f, 8f);
            _shotgunnerMoveTime = 0;
            FindAttackPoint();

            if (_soldier.HasPlayerVisual) { _soldier.FaceTarget = true; _soldier.SetMovementType(SoldierMovementType.WALK); }
            else { _soldier.SetMovementType(SoldierMovementType.RUN); _soldier.FaceTarget = false; }
        }

        public override void Update()
        {
            if (Time.time - _reactionTime > _engageTimeOut) { _soldier.Squad.ReleaseAttackSlot(_soldier); }
            _shotgunnerMoveTime += .1f;

            bool hasDistance = Vector3.Distance(_destination, _soldier.transform.position) > 2f;
            _soldier.SetLookTarget(_soldier.AttackPoint);
            _soldier.SetAllowFire(_soldier.IsPlayerInViewAngle(.25f) && _soldier.FaceTarget && Time.time - _reactionTime > 1f);

            if (_soldier.IsPlayerVisible())
            {
                _soldier.FaceTarget = true;
                _soldier.SetMovementType(SoldierMovementType.WALK);

                if (_soldier.SoldierType == SoldierType.SHOTGUNNER)
                {
                    if (_shotgunnerMoveTime > 2)
                    {
                        /*
                        if (_soldier.LastSpatialDataQuery != null)
                        {
                            _soldier.SetTarget(_soldier.LastSpatialDataQuery.UnsafePoints[Random.Range(0, _soldier.LastSpatialDataQuery.UnsafePoints.Count)].Position);
                        }*/
                        FindAttackPoint();
                        _shotgunnerMoveTime = 0;
                    }
                }

                return;
            }

            _soldier.FaceTarget = !hasDistance;
            _soldier.SetMovementType(hasDistance ? SoldierMovementType.RUN : SoldierMovementType.WALK);

            //si perdi al jugador y no tengo recorrido hacia un nuevo punto valido
            if (!_soldier.IsPlayerVisible() && !hasDistance)
            {
                if (_soldier.SoldierType == SoldierType.SHOTGUNNER)
                {
                    FindAttackPoint();
                    return;
                }

                //Check return value maybe???
                if (_soldier.TryThrowGrenade())
                {
                    _soldier.SetTarget(_soldier.transform.position);
                    //reset reaction time for a pause
                    _reactionTime = Time.time;
                    return;
                }

                if (_waitForChase > _chaseWaitTime)
                {
                    FindAttackPoint();
                    _waitForChase = 0;
                }
                else _waitForChase += Time.deltaTime;
            }
        }

        private void FindAttackPoint()
        {
            _shotgunnerMoveTime = 0;
            _destination = _soldier.FindAgressivePosition(true);
            //_destination = await _soldier.GetAttackPointAsync();
            _soldier.SetTarget(_destination);
        }
    }

    public class SoldierNearThreatAttackState : BaseState
    {
        private SoldierAgentController _observer;

        public SoldierNearThreatAttackState(AgentController context) : base(context)
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
            _observer.SetMovementType(SoldierMovementType.RUN);
            _observer.SetAllowFire(true);
        }

        public override void Update()
        {
            _observer.SetLookTarget(_observer.NearThreatAgent.Head.position);
            _observer.SetTarget(_observer.transform.position + (_observer.transform.position - _observer.NearThreatAgent.transform.position).normalized * 4f);
        }
    }

    public class SoldierDieState : BaseState
    {
        private SoldierAgentController _observer;
        private float _waitForRagdollTime = .4f;
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
            _observer.SetAllowFire(false);
            _observer.DropWeapon();
            _observer.RagdollBody();
        }

        public override void Update()
        {
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
            ;
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
        private Vector3 _destination;
        private float _lastFindCover;
        private SoldierAgentController _soldier;

        public SoldierRetreatCoverState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public bool IsCurrentPositionValid()
        {
            return _soldier.HasPlayerVisual;
        }

        public bool IsCurrentPositionValidCrouch()
        {
            if (VisualPhysics.Linecast(_soldier.PlayerHeadPosition, _soldier.transform.position + _soldier.transform.up * _soldier.CrouchHeight, out RaycastHit hit, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.gameObject.layer == 8) return false;
                return !hit.collider.gameObject.transform.root == _soldier.transform;
            }

            return false;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
            _soldier.HurtEvent -= OnHurt;
        }

        public override void Start()
        {
            _soldier.Animator.SetTrigger("COVER");
            _soldier.Squad.ReleaseAttackSlot(_soldier);
            _soldier.SetAllowFire(false);
            _soldier.FaceTarget = false;
            _soldier.HurtEvent += OnHurt;

            if (IsCurrentPositionValidCrouch())
            {
                _soldier.SetAllowReload(true);
                _soldier.SetMovementType(SoldierMovementType.CROUCH);
            }
            else MoveToCover();
        }

        private void OnHurt(AgentHurtPayload payload, AgentController controller)
        {
            MoveToCover();
        }

        public override void Update()
        {
            if (Time.time - _lastFindCover < 2) return;

            if (Vector3.Distance(_destination, _soldier.transform.position) < 1.1)
            {
                if (_soldier.CurrentCoverSpot != null || IsCurrentPositionValidCrouch())
                {
                    _soldier.SetAllowReload(true);
                    _soldier.SetMovementType(SoldierMovementType.CROUCH);
                }
                else if (!IsCurrentPositionValid())
                {
                    MoveToCover();
                }
            }
            _soldier.SetLookTarget(_soldier.AttackPoint);
        }

        private void MoveToCover()
        {
            _soldier.SetMovementType(SoldierMovementType.RUN);

            _lastFindCover = Time.time;
            _destination = _soldier.FindCoverFromPlayer(true);
            _soldier.SetTarget(_destination);
        }
    }

    public class SoldierGoToPlayer : BaseState
    {
        private SoldierAgentController _soldier;
        private Vector3 _destination;
        private Vector3 _lookPoint;

        public SoldierGoToPlayer(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _soldier.SetMovementType(SoldierMovementType.WALK);
            _soldier.Animator.SetBool("WARNING", true);
            _soldier.Animator.SetTrigger("SUSPECT");
            _soldier.SetTarget(_destination = _soldier.AttackPoint);
            _lookPoint = _soldier.AttackPoint;
        }

        private void SearchRandom()
        {
            if (Vector3.Distance(_soldier.transform.position, _destination) < 2)
            {
                SpatialDataQuery newQuery = new SpatialDataQuery(new SpatialQueryPrefs(_soldier, _soldier.PlayerPosition, _soldier.PlayerHeadPosition, 1));
                _soldier.SetTarget(_destination = newQuery.AllPoints[Random.Range(0, newQuery.AllPoints.Count)].Position);
                //REPORT CLEAR HERE??
                _lookPoint = _destination + Vector3.up * 1.75f;
            }
        }

        public override void Update()
        {
            _soldier.SetLookTarget(_lookPoint);
            SearchRandom();
        }
    }

    public class SoldierActBusyState : BaseState
    {
        private SoldierAgentController _soldier;
        private Vector3 _destination;
        private Vector3 _lookPoint;

        // todo: idle state, unalerted, scare
        public SoldierActBusyState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
            _soldier.Animator.SetBool("RELAX", false);
        }

        public override void Start()
        {
            _soldier.Animator.SetBool("RELAX", true);
            _soldier.SetTarget(_soldier.transform.position);
            _soldier.FaceTarget = false;
        }

        public override void Update()
        {
        }
    }
}