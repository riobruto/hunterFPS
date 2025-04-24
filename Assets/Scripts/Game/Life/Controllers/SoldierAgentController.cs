using Game.Entities;
using Game.Life;
using Game.Life.Entities;
using Game.Player.Sound;
using Game.Player.Weapon;
using Game.Service;
using Life.StateMachines;
using Life.StateMachines.Interfaces;
using Player.Weapon.Interfaces;
using System.Collections;

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
        //TODO: CONSIDERAR EL DIAGRAMA DE FLUJO DE MIRO
        //IMPLEMENTAR INCAPACITATED O HURT STATE.
        //INVESTIGATE ANTES DE REPORTAR.
        //REPORTAR VISCON Y AUDCON

        [Header("Sounds")]
        [SerializeField] private AudioClipGroup _footsteps;

        [SerializeField] private AudioClip[] _shouts;
        [SerializeField] private AudioClipGroup _painScream;
        [SerializeField] private AudioClipGroup _hurtScream;
        [SerializeField] private AudioClipGroup _deadScream;
        [SerializeField] private AudioClipGroup _coverCall;
        [SerializeField] private AudioClipGroup _attackCall;
        [SerializeField] private AudioClipGroup _searchCall;
        [SerializeField] private AudioClipGroup _headshotSound;

        [Header("Movement")]
        [SerializeField] private bool _useWaypoints;

        [SerializeField] private float _footstepDistance;
        [SerializeField] private float _crouchSpeed = 1f;
        [SerializeField] private float _patrolSpeed = 1f;
        [SerializeField] private float _runSpeed = 4.5f;
        [SerializeField] private float _walkSpeed = 2.5f;

        [Header("Choreo")]
        [SerializeField] private ActBusySpotEntity _startActBusySpot;

        public ActBusySpotEntity StartActBusySpot { get => _startActBusySpot; }

        private SoldierMovementType _current;
        private Vector3 _attackPoint;
        private float _desiredSpeed;
        private Vector3 _enterPoint;

        private float _hurtStopVelocityMultiplier;

        private AudioSource _movementAudio;
        private float _suspisiusTime;
        private float _travelledDistance;
        private bool _hasNearThreat;

        [Header("Cover Values")]
        [SerializeField] private bool _canShootFromCover;

        [Header("Combat Values")]
        [SerializeField] private SoldierType _soldierType;

        private float _hurtMoveRegenerationTimeout = 0;

        //STATES

        private SoldierReportState _report;

        private SoldierGoToPlayer _goToPlayer;

        private SoldierEnterState _enter;

        private SoldierDieState _die;

        private SoldierEngagePlayerState _attack;

        private SoldierRetreatCoverState _retreatCover;

        private SoldierActBusyState _actBusy;

        private SoldierNearThreatAttackState _nearThreat;

        private SoldierHoldPositionState _holdPosition;
        private SoldierHurtState _hurt;

        //Investigate State
        private float _investigateTimeOut = 15f;

        private float _elapsedTimeSinceWantedInvestigation;

        private Vector3 _investigateLocation = Vector3.zero;
        public Vector3 InvestigateLocation { get => _investigateLocation; }

        public bool ShouldInvestigate
        {
            get
            {
                return (_elapsedTimeSinceWantedInvestigation < _investigateTimeOut);
            }
        }

        private SoldierInvestigateState _investigate;

        //grenade cover
        private SoldierCoverFromGrenadeState _grenadeCover;

        public override void OnStart()
        {
            CanLoseContact = true;
            //setting Timers;
            _elapsedTimeSinceWantedInvestigation = _investigateTimeOut;

            _weapon = gameObject.GetComponent<AgentFireWeapon>();
            _waypoints = gameObject.GetComponent<AgentWaypoints>();
            _movementAudio = gameObject.AddComponent<AudioSource>();
            _movementAudio.spatialBlend = 1;
            _movementAudio.playOnAwake = false;
            _movementAudio.outputAudioMixerGroup = AudioToolService.GetMixerGroup(AudioChannels.AGENT);
            _movementAudio.maxDistance = 15;
            _movementAudio.minDistance = 1;
            _desiredSpeed = _walkSpeed;
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

            SetMovementType(SoldierMovementType.PATROL);
        }

        public AudioClipGroup HurtScream { get => _hurtScream; }

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
            _investigate = new(this);
            _holdPosition = new(this);
            _hurt = new(this);
        }

        private void CreateTransitions()
        {
            //TODO: MEJORAR FLAGS DE DETECCION, ESTAN MUY INCONSISTENTES
            //Definir mejor las transiciones ya que crean bucles
            //suspect,detect,report, asuntos separados)como la iglesia y el estado)))

            Machine.AddTransition(_grenadeCover, _attack, new FuncPredicate(() => _grenadeCover.Safe && ShouldEngageThePlayer));
            Machine.AddTransition(_grenadeCover, _retreatCover, new FuncPredicate(() => _grenadeCover.Safe && ShouldCoverFromThePlayer));

            Machine.AddTransition(_actBusy, _attack, new FuncPredicate(() => ShouldEngageThePlayer));
            Machine.AddTransition(_actBusy, _retreatCover, new FuncPredicate(() => ShouldCoverFromThePlayer));
            Machine.AddTransition(_actBusy, _holdPosition, new FuncPredicate(() => ShouldHoldPosition));
            Machine.AddTransition(_actBusy, _investigate, new FuncPredicate(() => ShouldInvestigate));
            Machine.SetState(_actBusy);

            Machine.AddTransition(_investigate, _actBusy, new FuncPredicate(() => _investigate.Ready));
            Machine.AddTransition(_investigate, _holdPosition, new FuncPredicate(() => _investigate.Ready));

            Machine.AddTransition(_holdPosition, _goToPlayer, new FuncPredicate(() => ShouldSearchForThePlayer));
            Machine.AddTransition(_holdPosition, _retreatCover, new FuncPredicate(() => ShouldCoverFromThePlayer));
            Machine.AddTransition(_holdPosition, _attack, new FuncPredicate(() => ShouldEngageThePlayer));

            Machine.AddTransition(_attack, _goToPlayer, new FuncPredicate(() => ShouldSearchForThePlayer));
            Machine.AddTransition(_attack, _retreatCover, new FuncPredicate(() => ShouldCoverFromThePlayer));
            Machine.AddTransition(_attack, _holdPosition, new FuncPredicate(() => ShouldHoldPosition));

            Machine.AddTransition(_goToPlayer, _attack, new FuncPredicate(() => ShouldEngageThePlayer));
            Machine.AddTransition(_goToPlayer, _retreatCover, new FuncPredicate(() => ShouldCoverFromThePlayer));
            Machine.AddTransition(_goToPlayer, _holdPosition, new FuncPredicate(() => ShouldHoldPosition));

            Machine.AddTransition(_retreatCover, _goToPlayer, new FuncPredicate(() => ShouldSearchForThePlayer));
            Machine.AddTransition(_retreatCover, _attack, new FuncPredicate(() => ShouldEngageThePlayer));
            // Machine.AddTransition(_retreatCover, _actBusy, new FuncPredicate(() => false));
            Machine.AddTransition(_retreatCover, _holdPosition, new FuncPredicate(() => ShouldHoldPosition));

            Machine.AddTransition(_hurt, _attack, new FuncPredicate(() => _hurt.Ready && ShouldEngageThePlayer));
            Machine.AddTransition(_hurt, _retreatCover, new FuncPredicate(() => _hurt.Ready && ShouldCoverFromThePlayer));
            Machine.AddTransition(_hurt, _holdPosition, new FuncPredicate(() => _hurt.Ready && ShouldHoldPosition));
            Machine.AddTransition(_hurt, _investigate, new FuncPredicate(() => _hurt.Ready && ShouldInvestigate));
        }

        private AgentWaypoints _waypoints;
        private AgentFireWeapon _weapon;

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

        public SoldierSquad Squad => _currentSquad;
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

        public Vector3 AttackPoint { get => _attackPoint; set => _attackPoint = value; }
        public Vector3 EnterPoint { get => _enterPoint; }
        public bool UseWaypoints => _useWaypoints;
        public AgentWaypoints Waypoints => _waypoints;
        public AgentFireWeapon Weapon => _weapon;

        public void SetAimLayerWeight(float target)
        {
            _desiredWeight = target;
        }

        public void SetAllowFire(bool state) => Weapon.AllowFire = state;

        public void SetAllowReload(bool state) => Weapon.AllowReload = state;

        //TODO: IMPLEMENT ENTER STATE FOR SPAWNING
        public void ForceGoToPoint(Vector3 point)
        {
            _enterPoint = point;
            StartCoroutine(IGoToPointSpawn());
        }

        private void OnStateChangedFromMachine(IState current, IState next)
        {
            if (next == _retreatCover)
            {
                TakingCoverEvent?.Invoke(this);
                AudioToolService.PlayClipAtPoint(_coverCall.GetRandom(), Head.transform.position, 1, AudioChannels.AGENT, 30);
            }
            if (next == _attack)
            {
                AudioToolService.PlayClipAtPoint(_attackCall.GetRandom(), Head.transform.position, 1, AudioChannels.AGENT, 30);
                AdvancingAttackEvent?.Invoke(this);
            }
            if (next == _goToPlayer)
            {
                AudioToolService.PlayClipAtPoint(_searchCall.GetRandom(), Head.transform.position, 1, AudioChannels.AGENT, 30);
                SearchingPlayerEvent?.Invoke(this);
            }
        }

        public override void OnLimbHurt(LimboxHit payload)
        {
            if (IsDead) return;

            SetHealth(GetHealth() - payload.Damage);
            TakingDamageEvent?.Invoke(this);

            if (payload.Hitbox.Type == LimbType.HEAD)
            {
                AudioToolService.PlayUISound(_headshotSound.GetRandom(), 1);
                KillAndPush(payload.Direction, payload.Hitbox);
            }
            SetMovementType(SoldierMovementType.WALK);

            _attackPoint = PlayerHeadPosition;
            _elapsedTimeSinceWantedInvestigation = 0;
            _investigateLocation = PlayerPosition;

            _hurtStopVelocityMultiplier = 0;
            NavMeshAgent.isStopped = true;
            _hurtMoveRegenerationTimeout = 1;

            if (Machine.CurrentState != _hurt) Machine.ForceChangeToState(_hurt);
        }

        public override void OnUpdate()
        {
            ManageHurtSlowdown();
            ManageNearThreat();
            ManageFootsteps();
            ManagePlayerPerception();

            _weapon.SetFireTarget(_attackPoint);
            if (_hurtStopVelocityMultiplier > 1)
            {
                Animator.SetLayerWeight(2, Mathf.SmoothDamp(Animator.GetLayerWeight(2), _desiredWeight, ref _desiredRefWeight, .25f));
            }
        }

        private void ManagePlayerPerception()
        {
            if (HasPlayerVisual)
            {
                _attackPoint = PlayerHeadPosition;
                _currentSquad.UpdateContact();
            }
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
            _hurtMoveRegenerationTimeout = Mathf.Clamp(_hurtMoveRegenerationTimeout - Time.deltaTime, 0, float.MaxValue);

            if (_hurtMoveRegenerationTimeout <= 0)
            {
                NavMeshAgent.isStopped = false;
                _hurtStopVelocityMultiplier = Mathf.Clamp(_hurtStopVelocityMultiplier + Time.deltaTime, -10, 1);
            }
            NavMeshAgent.speed = _desiredSpeed * Mathf.Clamp01(_hurtStopVelocityMultiplier);
        }

        private AgentController _nearThreatAgent;
        public AgentController NearThreatAgent => _nearThreatAgent;

        private AgentController FindNearThreat()
        {
            return null;
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
                    FaceTarget = false;
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

                    FaceTarget = true;
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
                    FaceTarget = false;
                    SetAimLayerWeight(0);
                    Debug.LogError("NO IMPLEMENTADO PATROL MOVEMENT!");
                    _desiredSpeed = _walkSpeed;
                    break;

                case SoldierMovementType.CROUCH:

                    FaceTarget = true;
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
            AudioToolService.PlayClipAtPoint(_shouts[Random.Range(0, _shouts.Length)], Head.transform.position, 1f, AudioChannels.AGENT, 30);
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

        private IEnumerator IGoToPointSpawn()
        {
            yield return new WaitForEndOfFrame();

            Machine.ForceChangeToState(_enter);
            yield break;
        }

        private IEnumerator BindWeapon()
        {
            yield return new WaitForEndOfFrame();
            _weapon.WeaponEngine.WeaponChangedState += OnWeaponChangeState;
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

        public bool ShouldEngageThePlayer
        {
            get
            {
                if (Weapon.Empty)
                {
                    return false;
                }

                bool canAttackFromSquad = _hasSquad && !_currentSquad.HasLostContact;
                if (canAttackFromSquad)
                {
                    return _currentSquad.CanTakeAttackSlot(this);
                }
                else return false;
            }
        }

        public bool ShouldSearchForThePlayer
        {
            get
            {
                if (ShouldEngageThePlayer) return false;
                return (_currentSquad.IsAlert && _currentSquad.HasLostContact && _currentSquad.CanTakeAttackSlot(this));
            }
        }

        public bool ShouldCoverFromThePlayer
        {
            get
            {
                if (Weapon.Empty && _soldierType != SoldierType.SHOTGUNNER) return true;
                bool canAttackFromSquad = _hasSquad && !_currentSquad.HasLostContact && !_currentSquad.CanTakeAttackSlot(this);
                return canAttackFromSquad;
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

        public bool ShouldHoldPosition
        {
            get
            {
                return _currentSquad.IsAlert && _currentSquad.HasLostContact && !_currentSquad.CanTakeAttackSlot(this);
            }
        }

        private float _recieveKickCooldown = 0.1f;
        private float _lastRecieverkKickTime = 0;

        public override void Kick(Vector3 position, Vector3 direction, float damage)
        {
            //prevent multiples hitboxes being kicked
            if (Time.time - _lastRecieverkKickTime < _recieveKickCooldown) return;
            _lastRecieverkKickTime = Time.time;
            SetHealth(GetHealth() - damage);
            ForcePlayerPerception();
        }

        public override void ForcePlayerPerception() => _attackPoint = PlayerHeadPosition;

        private int _queryAmount;
        public SoldierType SoldierType { get => _soldierType; }

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

        private CoverSpotEntity _currentCoverSpot;

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

        [SerializeField] private bool _debugSpatialData;

        //math

        [SerializeField] private LayerMask _coverMask;

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

            Vector3 center = _currentSquad.SquadCentroid;
            if (_currentSquad != null && _currentSquad.ShouldHoldPlayer)
            {
                center = _currentSquad.HoldPosition;
            }
            SpatialData? data = AgentSpatialUtility.GetBestPoint(AgentSpatialUtility.CreateCoverArray(this, new Vector2Int(20, 20), center, _attackPoint, _coverMask));
            if (data.HasValue) { return data.Value.Position; }
            return PlayerHeadPosition + (transform.position - PlayerHeadPosition).normalized * 5f;

            /*
            SpatialDataQuery query = new SpatialDataQuery(new SpatialQueryPrefs(this, center, PlayerHeadPosition, 2f));
            LastSpatialDataQuery = query;

            if (query.WallCoverPoints.Count > 0) { return query.WallCoverPoints[0].Position; }
            if (query.SafePoints.Count > 0) { return query.SafePoints[0].Position; }
            if (query.WallCoverCrouchedPoints.Count > 0) { return query.WallCoverCrouchedPoints[0].Position; }
            if (query.SafeCrouchPoints.Count > 0) { return query.SafeCrouchPoints[0].Position; }
            else return query.AllPoints[query.AllPoints.Count].Position;*/
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
                    SpatialData? data = AgentSpatialUtility.GetBestPoint(AgentSpatialUtility.CreateAttackArray(this, new Vector2Int(20, 20), _attackPoint, _attackPoint, _coverMask |= 1 << gameObject.layer));
                    if (data.HasValue) { return data.Value.Position; }
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
            Vector3 center = _currentSquad.SquadCentroid;
            if (_currentSquad != null && _currentSquad.ShouldHoldPlayer)
            {
                center = _currentSquad.HoldPosition;
            }
            SpatialData? data = AgentSpatialUtility.GetBestPoint(AgentSpatialUtility.CreateAttackArray(this, new Vector2Int(30, 30), center, _attackPoint, _coverMask |= 1 << gameObject.layer));
            if (data.HasValue) { return data.Value.Position; }

            return PlayerHeadPosition;// + (transform.position - PlayerHeadPosition).normalized * 5f;
        }

        public ActBusySpotEntity FindActBusyEntity()
        {
            if (_startActBusySpot) return _startActBusySpot;

            ActBusyQuery query = new ActBusyQuery(this);
            if (query.ActBusySpots.Length > 0)
            {
                foreach (ActBusySpotEntity ent in query.ActBusySpots)
                {
                    if (ent.TryTake(this))
                    {
                        return ent;
                    }
                }
            }
            return null;
        }

        private Vector3 CalculateGrenadeThrowVector(Vector3 start, Vector3 target)
        {
            float displacementY = target.y - start.y;
            Vector3 displacementXZ = new Vector3(target.x - start.x, 0, target.z - start.z);
            float height = .25f;
            float gravity = Physics.gravity.y;
            float time = Mathf.Sqrt(-2 * height / gravity) + Mathf.Sqrt(2 * (displacementY - height) / gravity);
            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
            Vector3 velocityXZ = displacementXZ / time;

            //??? no es suficiente velocitdad
            return velocityY + velocityXZ;
        }

        private Vector3 _grenadePosition;
        private float _grenadeSafeDistance = 7;

        [Header("Grenade")]
        [SerializeField] private float _minDistanceGrenade = 10;

        [SerializeField] private float _maxDistanceGrenade = 25f;
        [SerializeField] private GameObject _granade;

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
            }

            if (Physics.Linecast(transform.position + Vector3.up * 1.60f + transform.forward * .5f + transform.right * .5f, _attackPoint)) return false;
            //throw grende
            //coroutine delay

            StartCoroutine(ThrowGrenade());
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
                grenade.Trigger(1);
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
            Gizmos.color = Color.blue;
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
            ShoutDead();
            Destroy(gameObject, 10);

            transform.parent = null;
        }

        public override void OnHeardCombat()
        {
            if (IsDead) return;
            //Alert, report!
            _attackPoint = PlayerHeadPosition;
            _currentSquad.UpdateContact();
        }

        public override void OnHeardSteps()
        {
            if (IsDead) return;
            _currentSquad.UpdateContact();
            _attackPoint = PlayerHeadPosition;
            _elapsedTimeSinceWantedInvestigation = 0;
            _investigateLocation = PlayerPosition;
            //Alert, but dont report!
        }

        public override void OnPlayerDetectionChanged(bool detected)
        {
        }

        public void RagdollBody()
        {
            Animator.enabled = false;
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
            }
        }

        internal CoverSpotEntity FindCoverSpot()
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
                        return CurrentCoverSpot;
                    }
                }
            }
            return null;
        }
    }
}