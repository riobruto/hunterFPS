using Core.Engine;
using Game.Entities;
using Game.Hit;
using Life.Controllers;
using Life.StateMachines;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

namespace Game.Life.Controllers
{
    public class RenegateAgentController : AgentController
    {
        private RenegateSleepState _sleep;
        private RenegateAttackState _attack;
        private RenegateAwakeState _awake;
        private RenegateHurtState _hurt;
        private RenegateDeathState _dead;

        [SerializeField] private GameObject _weapon;
        [SerializeField] private AnimationHurtbox _hurtbox;
        [SerializeField] private AgentFireWeapon _fireWeapon;
        [SerializeField] private bool _useFireWeapon;
        [SerializeField] private Transform _lookAtTransform;

        private Vector2 _smoothDeltaPos;
        private Vector2 _velocity;

        public LayerMask CoverMask { get; internal set; }
        public bool UseFirearm { get => _useFireWeapon && _fireWeapon != null; }

        public override void OnStart()
        {
            SetMaxHealth(60);
            SetHealth(60);
            CreateStates();
            CreateTransitions();

            if (!UseFirearm)
            {
                _hurtbox.Initialize(Bootstrap.Resolve<GameSettings>().RaycastConfiguration.EnemyGunLayers, 22);
            }
            else
            {
                _fireWeapon.AllowReload = true;
            }
            CanLoseContact = true;
            Animator.applyRootMotion = true;
            NavMeshAgent.updatePosition = false;
            NavMeshAgent.updateRotation = true;
            NavMeshAgent.speed = 2;

            CoverMask = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers;
        }

        private void CreateStates()
        {
            _sleep = new(this);
            _attack = new(this);
            _awake = new(this);
            _hurt = new(this);
            _dead = new(this);
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_sleep, _awake, new FuncPredicate(() => HasPlayerVisual));
            Machine.AddTransition(_awake, _attack, new FuncPredicate(() => _awake.Finished));
            Machine.AddTransition(_hurt, _attack, new FuncPredicate(() => HasPlayerVisual));

            Machine.SetState(_sleep);
        }

        private void OnAnimatorMove()
        {
            if (IsDead) { return; }
            Vector3 rootPosition = Animator.rootPosition;
            rootPosition.y = NavMeshAgent.nextPosition.y;
            transform.position = rootPosition;
            NavMeshAgent.nextPosition = rootPosition;
        }

        private Vector3 _lookVelocity;
        [SerializeField] private MultiAimConstraint _aimRig;

        private void LateUpdate()
        {
            if (IsDead) { return; }

            if (_lookAtTransform != null) _lookAtTransform.position = Vector3.SmoothDamp(_lookAtTransform.position, AimTarget, ref _lookVelocity, 0.4f);
            if (UseFirearm)
            {
                _aimRig.weight = HasPlayerVisual ? 1 : 0;
                _fireWeapon.SetFireTarget(AimTarget);
            }
        }

        public override void UpdateMovement()
        {
            if (IsDead) { return; }

            var aimDir = (AimTarget - Head.position).normalized;
            float aim_horizontal = FaceTarget ? Vector3.Cross(transform.forward, aimDir).y : 0;
            Vector3 worldDelta = NavMeshAgent.nextPosition - transform.position;
            worldDelta.y = 0;
            float dx = Vector3.Dot(transform.right, worldDelta);
            float dy = Vector3.Dot(transform.forward, worldDelta);
            Vector2 deltaPos = new Vector2(dx, dy);
            float smooth = Mathf.Min(1, Time.deltaTime / 0.1f);
            _smoothDeltaPos = Vector2.Lerp(_smoothDeltaPos, deltaPos, smooth);
            _velocity = _smoothDeltaPos / Time.deltaTime;

            Animator.SetFloat("mov_turn", aim_horizontal * NavMeshAgent.angularSpeed * Time.deltaTime, .05f, Time.deltaTime);

            if (NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance)
            {
                _velocity = Vector2.Lerp(Vector2.zero, _velocity, NavMeshAgent.remainingDistance / NavMeshAgent.stoppingDistance);
            }

            if (FaceTarget)
            {
                transform.Rotate(Vector3.up, aim_horizontal * NavMeshAgent.angularSpeed * Time.deltaTime);
            }

            Animator.SetFloat("mov_forward", _velocity.y, .05f, Time.deltaTime);
            Animator.SetFloat("mov_right", _velocity.x, .05f, Time.deltaTime);
        }

        public override void OnLimbHurt(LimboxHit payload)
        {
            if (IsDead) return;
            if (GetHealth() > 0)
            {
                SetHealth(GetHealth() - payload.Damage);
            }
            //Machine.ForceChangeToState(_hurt);
        }

        public void Ragdoll()
        {
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
            }
            DropWeapon();
        }

        public void DropWeapon()
        {
            if (UseFirearm) _fireWeapon.DropWeapon();
            else _weapon.GetComponent<Rigidbody>().isKinematic = false;
        }

        public override void OnDeath()
        {
            Ragdoll();
            if (_fireWeapon != null) _fireWeapon.AllowFire = false;
            Machine.ForceChangeToState(_dead);
            Animator.enabled = false;
            NavMeshAgent.SetDestination(transform.position);
            NavMeshAgent.enabled = false;
        }

        internal void StartScan(int frames)
        {
            _hurtbox.StartScan(frames);
            FaceTarget = false;
        }

        internal void AllowFire(bool value)
        {
            _fireWeapon.AllowFire = value;
        }

        public override void ForcePlayerPerception()
        {
            StartCoroutine(IForceContact());
        }

        private IEnumerator IForceContact()
        {
            SetLookTarget(PlayerHeadPosition);
            CanLoseContact = false;
            yield return new WaitForEndOfFrame();
            CanLoseContact = true;
        }

        internal void ReportPlayerContact()
        {
            foreach (AgentController agent in AgentGlobalSystem.ActiveAgents)
            {
                if (agent is RenegateAgentController)
                {
                    if (agent == this) continue;
                    if (Vector3.Distance(agent.transform.position, transform.position) > 20) continue;
                    agent.ForcePlayerPerception();
                }
            }
        }
    }

    public class RenegateHurtState : BaseState
    {
        private RenegateAgentController _renegate;

        public RenegateHurtState(AgentController context) : base(context)
        {
            _renegate = context as RenegateAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _renegate.Animator.SetTrigger("HURT");
        }

        public override void Think()
        {
        }
    }

    public class RenegateAwakeState : BaseState
    {
        private RenegateAgentController _renegate;

        public RenegateAwakeState(AgentController context) : base(context)
        {
            _renegate = context as RenegateAgentController;
        }

        public bool Finished { get; internal set; }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _renegate.ReportPlayerContact();

            Finished = true;
        }

        public override void Think()
        {
        }
    }

    public class RenegateSleepState : BaseState
    {
        private RenegateAgentController _renegate;

        public RenegateSleepState(AgentController context) : base(context)
        {
            _renegate = context as RenegateAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _renegate.FaceTarget = false;
        }

        public override void Think()
        {
        }
    }

    public class RenegateDeathState : BaseState
    {
        private RenegateAgentController _renegate;

        public RenegateDeathState(AgentController context) : base(context)
        {
            _renegate = context as RenegateAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _renegate.FaceTarget = false;
        }

        public override void Think()
        {
        }
    }

    public class RenegateAttackState : BaseState
    {
        private RenegateAgentController _renegate;

        private NativeArray<SpatialData> _spatialData;
        private SpatialData[] _spatialDataDebug;

        private bool _inRange => Vector3.Distance(_renegate.transform.position, _idealPos) < 0.5f;

        private float _lastAttackTime;
        private Vector3 _idealPos;

        public RenegateAttackState(AgentController context) : base(context)
        {
            _renegate = context as RenegateAgentController;
        }

        public override void DrawGizmos()
        {
            if (_spatialDataDebug == null) return;

            foreach (SpatialData data in _spatialDataDebug)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, data.Weight);
                Gizmos.DrawSphere(data.Position, .25f);
            }
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _renegate.FaceTarget = true;
            _timeSinceSearchedNewSpot = 3;
        }

        public override void Think()
        {
            if (_renegate.UseFirearm)
            {
                DoFirearm();
            }
            else
            {
                DoMeelee();
            }
        }

        private void DoMeelee()
        {
            _renegate.NavMeshAgent.speed = Vector3.Distance(_renegate.transform.position, _renegate.PlayerPosition) > 8 ? 4 : 2;

            if (_inRange && (Time.time - _lastAttackTime > 3))
            {
                _renegate.Animator.SetTrigger("MEELEE");
                _lastAttackTime = Time.time;
                return;
            }
            else
            {
                _renegate.FaceTarget = true;
            }

            _spatialData = AgentSpatialUtility.CreateCircleArray(_renegate, _renegate.PlayerHeadPosition, _renegate.CoverMask, 2, 10);
            _spatialDataDebug = _spatialData.ToArray();

            if (_spatialData.Length > 0)
            {
                SpatialData? point = AgentSpatialUtility.GetBestPoint(_spatialData);
                if (point.HasValue)
                {
                    _renegate.NavMeshAgent.SetDestination(point.Value.Position);
                    _idealPos = point.Value.Position;
                }
            }
            _renegate.SetLookTarget(_renegate.PlayerHeadPosition);
        }

        private float _timeSinceSearchedNewSpot;
        private float _swaySearchTime;

        private void DoFirearm()
        {
            _renegate.AllowFire(_renegate.HasPlayerVisual);

            if (_renegate.HasPlayerVisual)
            {
                if (_swaySearchTime > 1)
                {
                    _spatialData = AgentSpatialUtility.CreateAttackArray(_renegate, new Vector2Int(6, 6), _renegate.transform.position, _renegate.PlayerHeadPosition, _renegate.CoverMask, 1);
                    _spatialDataDebug = _spatialData.ToArray();
                    SpatialData? point = AgentSpatialUtility.GetBestPoint(_spatialData);
                    if (point.HasValue)
                    {
                        _renegate.NavMeshAgent.SetDestination(point.Value.Position);
                    }
                    _swaySearchTime = 0;
                }
                _renegate.SetLookTarget(_renegate.PlayerHeadPosition);
                _swaySearchTime += _renegate.ThinkingInterval;
                return;
            }

            if (!_renegate.HasPlayerVisual) { _timeSinceSearchedNewSpot += _renegate.ThinkingInterval; }
            if (_timeSinceSearchedNewSpot > 3)
            {
                _spatialData = AgentSpatialUtility.CreateAttackArray(_renegate, new Vector2Int(20, 20), _renegate.PlayerHeadPosition, _renegate.PlayerHeadPosition, _renegate.CoverMask, 4);
                _spatialDataDebug = _spatialData.ToArray();
                SpatialData? point = AgentSpatialUtility.GetBestPoint(_spatialData);
                if (point.HasValue)
                {
                    _renegate.NavMeshAgent.SetDestination(point.Value.Position);
                    _idealPos = point.Value.Position;
                }

                _timeSinceSearchedNewSpot = 0;
            }
        }
    }
}