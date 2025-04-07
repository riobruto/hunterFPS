using System;
using System.Linq;
using Game.Entities;
using Game.Life;
using Life.Entities;
using Life.StateMachines;
using UnityEngine;

namespace Life.Controllers
{
    public class ChivalryAgentController : AgentController
    {
        [Header("References")]
        [SerializeField] private Transform _npcMount;

        [SerializeField] public LayerMask CoverMask;
        [SerializeField] private AgentFireWeapon _weapon;
        [SerializeField] public Animator Soldier;

        [Header("Combat")]
        [SerializeField] private HorseNavHintGroup _combatHintsGroup;

        [SerializeField] private LimbHitbox[] _humanHitboxes;
        [SerializeField] private LimbHitbox[] _horseHitboxes;

        private Vector3 _attackPoint;

        public override void OnStart()
        {
            CanLoseContact = true;

            SetSpeed(6);
            SetMaxHealth(200);
            SetHealth(200);
            SetUpStates();
            SetUpTransitions();
            NavMeshAgent.updatePosition = false;
            NavMeshAgent.updateRotation = true;
            NavMeshAgent.angularSpeed = 120f;
            Animator.applyRootMotion = true;
            _weapon.AllowReload = true;

            SetLimboxes();
        }

        private void SetLimboxes()
        {
            foreach (LimbHitbox hLimb in _humanHitboxes)
            {
                hLimb.LimbHitEvent += OnHumanLimbHurt;
            }
            foreach (LimbHitbox horseLimb in _horseHitboxes)
            {
                horseLimb.LimbHitEvent += OnLimbHurt;
            }
        }

        private float _soldierHealth = 100;

        private void OnHumanLimbHurt(LimboxHit payload)
        {
            if (_soldierHealth < 0) return;
            if (payload.Hitbox.Type == LimbType.HEAD) _soldierHealth = 0;
            else _soldierHealth -= payload.Damage;

            if (_soldierHealth <= 0)
            {
                OnSoldierDeath();
            }
        }

        private void OnSoldierDeath()
        {
            //ir al estado de despawn
            Soldier.enabled = false;
            Soldier.transform.SetParent(null);
            _weapon.AllowFire = false;
            foreach (LimbHitbox hLimb in _humanHitboxes)
            {
                hLimb.Ragdoll();
            }
        }

        public override void OnLimbHurt(LimboxHit payload)
        {
            if (IsDead) return;
            SetHealth(GetHealth() - payload.Damage);
            if (GetHealth() <= 0)
            {
                OnDeath();
            }
        }

        public override void OnDeath()
        {
            Animator.enabled = false;
            foreach (LimbHitbox horseLimb in _horseHitboxes)
            {
                horseLimb.Ragdoll();
            }
            OnSoldierDeath();
            _soldierHealth = 0;
        }

        private HorsePatrolActBusyState _horsePatrol;
        private HorseApproachCombatState _approachCombat;
        private HorseRetreatCombatState _retreatCombat;

        private void SetUpStates()
        {
            _horsePatrol = new(this);
            _approachCombat = new(this);
            _retreatCombat = new(this);
        }

        private void SetUpTransitions()
        {
            Machine.AddAnyTransition(_horsePatrol, new FuncPredicate(() => false));
            Machine.AddTransition(_horsePatrol, _approachCombat, new FuncPredicate(() => _shouldApproachCombat));

            Machine.AddTransition(_approachCombat, _retreatCombat, new FuncPredicate(() => _approachCombat.Completed));
            Machine.AddTransition(_retreatCombat, _approachCombat, new FuncPredicate(() => _retreatCombat.Completed));

            Machine.SetState(_horsePatrol);
        }

        private bool _shouldApproachCombat { get => HasPlayerVisual; }
        private bool _shouldRetreatCombat { get; }
        private bool _shouldGetAlerted { get; }

        private void OnAnimatorMove()
        {
            // if (!Initialized) return;

            Vector3 root = Animator.rootPosition;
            root.y = NavMeshAgent.nextPosition.y;

            transform.position = root;
            NavMeshAgent.nextPosition = root;
        }

        private Vector2 _smoothDeltaPos;
        private Vector2 _velocity;
        private float _maxspeed;

        public Vector3 AttackPoint { get => _attackPoint; }
        public HorseNavHintGroup CombatHintsGroup { get => _combatHintsGroup; }

        public void SetAttackPoint(Vector3 value) => _attackPoint = value;

        public void SetSpeed(float value) => NavMeshAgent.speed = value;

        public override void OnUpdate()
        {
            _weapon.SetFireTarget(_attackPoint);
            ManageAnimate();
        }

        private void ManageAnimate()
        {
            Vector3 dir;
            if (FaceTarget)
            {
                dir = AttackPoint - Head.position;
                dir.Normalize();
                dir = transform.InverseTransformDirection(dir);
            }
            else
            {
                dir = Vector3.back;
            }
            Soldier.SetFloat("dir_x", dir.x, 0.1f, Time.deltaTime);
            Soldier.SetFloat("dir_y", dir.z, 0.1f, Time.deltaTime);
            Soldier.SetLayerWeight(1, Animator.velocity.magnitude / 10.7f);
        }

        public override void UpdateMovement()
        {
            Vector3 worldDelta = NavMeshAgent.nextPosition - transform.position;
            worldDelta.y = 0;

            float dx = Vector3.Dot(transform.right, worldDelta);
            float dy = Vector3.Dot(transform.forward, worldDelta);
            Vector2 deltaPos = new Vector2(dx, dy);
            float smooth = Mathf.Min(1, Time.deltaTime / 0.1f);
            _smoothDeltaPos = Vector2.Lerp(_smoothDeltaPos, deltaPos, smooth);
            _velocity = _smoothDeltaPos / Time.deltaTime;
            float turn = Vector3.Dot(transform.right, (transform.position - NavMeshAgent.nextPosition).normalized);
            float steeringDot = Vector3.Dot((NavMeshAgent.steeringTarget - transform.position).normalized, transform.right.normalized);

            if (NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance)
            {
                _velocity = Vector2.Lerp(Vector2.zero, _velocity, NavMeshAgent.remainingDistance / NavMeshAgent.stoppingDistance);
            }
            _maxspeed = Mathf.Clamp(NavMeshAgent.remainingDistance, 2f, 10.7f);
            _velocity = _velocity.normalized * Mathf.Lerp(7, 1, Mathf.Abs(turn));
            if (steeringDot < 0.25f)
            {
                Animator.SetFloat("vel_forward", _velocity.y, 0.1f, Time.deltaTime);
            }
            else
            {
                Animator.SetFloat("vel_forward", 1, 0.1f, Time.deltaTime);
            }
            Animator.SetFloat("turn", turn, .15f, Time.deltaTime);
        }

        internal void AllowFire(bool v)
        {
            if (IsDead) return;
            _weapon.AllowFire = v;
        }
    }

    public class HorsePatrolActBusyState : BaseState
    {
        private ChivalryAgentController _horse;

        public HorsePatrolActBusyState(AgentController context) : base(context)
        {
            _horse = context as ChivalryAgentController;
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

        public override void Think()
        {
        }
    }

    public class HorseApproachCombatState : BaseState
    {
        private ChivalryAgentController _horse;

        public HorseApproachCombatState(AgentController context) : base(context)
        {
            _horse = context as ChivalryAgentController;
        }

        public bool Completed => _completed;

        public override void DrawGizmos()
        {
            if (_travel != null)
            {
                foreach (Vector3 child in _travel)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(child, Vector3.one * 1f);
                }
            }
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _horse.Animator.SetTrigger("ALERT");
            index = 0;
            _completed = false;
            _hasTravel = false;
            CreateVectors();
        }

        private Vector3 _startVector = Vector3.zero;

        private bool _hasTravel;
        private bool _completed;

        //that can't raycast to either npc nor target, but can raycast to vector A and B
        private Vector3 _middleVector = Vector3.zero;

        private void CreateVectors()
        {
            Vector3 start = AgentSpatialUtility.GetBestPoint(AgentSpatialUtility.CreateCoverArray(new Vector2Int(30, 30), _horse.transform.position, _horse.PlayerHeadPosition, _horse.CoverMask)).Value.Position;
            HorseNavHint startHint = _horse.CombatHintsGroup.NearestHintFromPoint(start);
            Vector3 attack = AgentSpatialUtility.GetBestPoint(AgentSpatialUtility.CreateAttackArray(new Vector2Int(30, 30), _horse.PlayerGameObject.transform.position, _horse.PlayerHeadPosition, _horse.CoverMask)).Value.Position;
            HorseNavHint endHint = _horse.CombatHintsGroup.NearestHintFromPoint(attack);
            _horse.SetTarget(startHint.position);
            index = 0;

            HorseNavHint[] hints = _horse.CombatHintsGroup.FindPath(startHint, endHint);

            if (hints.Length > 0)
            {
                _travel = new Vector3[hints.Length];
                for (int i = 0; i < hints.Length; i++)
                {
                    _travel[i] = hints[i].position;
                }
                _hasTravel = true;
            }
        }

        public override void Think()
        {
            ManageApproach();
            ManageShooting();
        }

        private Vector3[] _travel;
        private int index = 0;

        private void ManageApproach()
        {
            if (!_hasTravel) return;

            if (Vector3.Distance(_horse.transform.position, _travel[index]) < 5)
            {
                if (index < _travel.Length - 1)
                {
                    index++;
                    _horse.SetTarget(_travel[index]);
                }
                else
                {
                    _hasTravel = false;
                    _completed = true;
                }
            }
        }

        private void ManageShooting()
        {
            Vector3 dir = Vector3.zero;

            if (_horse.HasPlayerVisual)
            {
                _horse.AllowFire(true);
                _horse.SetAttackPoint(_horse.PlayerHeadPosition);
                _horse.FaceTarget = true;
            }
            else
            {
                _horse.FaceTarget = false;
                _horse.AllowFire(false);
            }
        }
    }

    public class HorseRetreatCombatState : BaseState
    {
        private ChivalryAgentController _horse;
        private int index;
        private Vector3[] _travel;
        private bool _hasTravel;
        private bool _completed;
        public bool Completed => _completed;

        public HorseRetreatCombatState(AgentController context) : base(context)
        {
            _horse = context as ChivalryAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _horse.Animator.SetTrigger("ALERT");
            _hasTravel = false;
            _completed = false;
            CreateVectors();
        }

        private void CreateVectors()
        {
            SpatialData[] spatialData = AgentSpatialUtility.CreateAttackArray(new Vector2Int(30, 30), _horse.PlayerGameObject.transform.position, _horse.PlayerHeadPosition, _horse.CoverMask).ToArray();

            Vector3 escapePoint = spatialData[spatialData.Length - 1].Position;
            HorseNavHint[] hintsDistance = _horse.CombatHintsGroup.Hints;
            Array.Sort(hintsDistance, _horse.CombatHintsGroup.CompareDistance);

            HorseNavHint endHint = hintsDistance.Reverse().ToArray()[0];

            HorseNavHint startHint = _horse.CombatHintsGroup.NearestHintFromPoint(_horse.transform.position);

            _horse.SetTarget(startHint.position);
            index = 0;

            HorseNavHint[] path = _horse.CombatHintsGroup.FindPath(startHint, endHint);

            if (path.Length > 0)
            {
                _travel = new Vector3[path.Length];
                for (int i = 0; i < path.Length; i++)
                {
                    _travel[i] = path[i].position;
                }
                _hasTravel = true;
            }
        }

        private void ManageApproach()
        {
            if (!_hasTravel) return;

            if (Vector3.Distance(_horse.transform.position, _travel[index]) < 5)
            {
                if (index < _travel.Length - 1)
                {
                    index++;
                    _horse.SetTarget(_travel[index]);
                }
                else
                {
                    _hasTravel = false;
                    _completed = true;
                }
            }
        }

        public override void Think()
        {
            ManageApproach();
            ManageShooting();
        }

        private void ManageShooting()
        {
            Vector3 dir = Vector3.zero;

            if (_horse.HasPlayerVisual)
            {
                _horse.AllowFire(true);
                _horse.SetAttackPoint(_horse.PlayerHeadPosition);

                _horse.FaceTarget = true;
            }
            else
            {
                _horse.FaceTarget = false;
                _horse.AllowFire(false);
            }
        }
    }
}