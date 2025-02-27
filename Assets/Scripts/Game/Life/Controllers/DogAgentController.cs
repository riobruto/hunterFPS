using Core.Engine;
using Game.Entities;
using Game.Inventory;
using Game.Player.Controllers;
using Game.Service;
using Life.StateMachines;
using Nomnom.RaycastVisualization;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

namespace Life.Controllers
{
    public class DogAgentController : AgentController
    {
        //TODO:
        //CREAR SKIN DE PERRO Y PERRO SALVAJE
        //PERRO SALVAJE => MOOD -1 SIEMPRE Y ATACARA CULQUIER NPC TIPO AGGRO O PLAYER
        //PERRO NORMAL PODRA SER DOMESTICADO DROPEANDOLE ALGUNA COMIDA
        //EL PERRO NORMAL SE PONDRA AGRESIVO SI DISPARAMOS O LO LASTIMAMOS ( ON HURT DEBERIA TENER SENDER, MAL AHI)
        //EL PERRO SALVAJE SERA MAS FEO Y FACIL DE RECONOCER

        // 0 indicates neutrality, -1 angry and 1 happy.
        private int _mood = 0;

        private bool _hasFoodNear => _foodItem != null;
        private bool _wantToAttackPlayer;
        private bool _isAttackingPlayer;
        private GameObject _foodItem;

        [SerializeField] private Transform _lookAtTarget;
        [SerializeField] private Rig _headRig;

        public override void OnStart()
        {
            CreateStates();
            CreateTransitions();
            SetMaxHealth(100);
            SetHealth(100);
            SetMood(-1);

            NavMeshAgent.updatePosition = true;
            NavMeshAgent.updateRotation = true;
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_attack, _gotoTarget, new FuncPredicate(() => _attack.Ready && _attackTarget != null));
            Machine.AddTransition(_attack, _gotoPlayer, new FuncPredicate(() => _attackTarget == null && _isFriendlyToPlayer));

            Machine.AddTransition(_gotoPlayer, _gotoFood, new FuncPredicate(() => _hasFoodNear && _canEatFood));
            Machine.AddTransition(_gotoPlayer, _gotoTarget, new FuncPredicate(() => _attackTarget != null));
            Machine.AddTransition(_gotoPlayer, _attackPlayer, new FuncPredicate(() => _wantToAttackPlayer && _gotoPlayer.IsInAttackRange));
            Machine.AddTransition(_attackPlayer, _gotoPlayer, new FuncPredicate(() => _attackPlayer.Ready));

            Machine.AddTransition(_gotoTarget, _attack, new FuncPredicate(() => _attackTarget != null && _gotoTarget.IsInAttackRange));
            Machine.AddTransition(_gotoTarget, _gotoPlayer, new FuncPredicate(() => _attackTarget == null));
            Machine.AddTransition(_gotoFood, _eat, new FuncPredicate(() => _gotoFood.IsInEatRange && _canEatFood));

            Machine.AddTransition(_eat, _gotoPlayer, new FuncPredicate(() => _eat.Ready));
            Machine.AddTransition(_rest, _gotoPlayer, new FuncPredicate(() => HasPlayerVisual));
            Machine.SetState(_rest);
        }

        private void CreateStates()
        {
            _gotoTarget = new(this);
            _gotoPlayer = new(this);
            _gotoFood = new(this);
            _rest = new(this);
            _die = new(this);
            _attack = new(this);
            _attackPlayer = new(this);
            _eat = new(this);
        }

        private DogGoToPlayerState _gotoPlayer;
        private DogGoToAttackTargetState _gotoTarget;
        private DogGoToFoodState _gotoFood;
        private DogRestState _rest;
        private DogDieState _die;
        private DogAttackState _attack;
        private DogAttackPlayerState _attackPlayer;
        private DogEatState _eat;
        private AgentController _attackTarget;

        public override void OnDeath()
        {
            Machine.ForceChangeToState(_die);
            Ragdoll();
            NavMeshAgent.isStopped = true;
        }

        public override void UpdateMovement()
        {
            if (Vector3.Distance(transform.position, NavMeshAgent.destination) < MinMoveDistance)
            {
                NavMeshAgent.ResetPath();
            }

            //fwd  *direction dot = 0.25f
            //|   /
            //|  /
            //| /
            //|/____________right

            // inyeccion letal

            float steeringDot = Vector3.Dot((NavMeshAgent.steeringTarget - transform.position).normalized, transform.right.normalized);
            //Debug.Log(steeringDot);

            Vector3 relativeVelocity = transform.InverseTransformDirection(NavMeshAgent.velocity);
            Animator.SetFloat("mov_forward", relativeVelocity.z, 0.05f, Time.deltaTime);
            Animator.SetFloat("TURN", steeringDot, .15f, Time.deltaTime);

            VisualPhysics.Raycast(transform.position + transform.up * .25f, Vector3.down, out RaycastHit hit, 2f, LayerMask.GetMask("Default"));
            Debug.DrawRay(transform.position, hit.normal, Color.magenta);
            Animator.SetFloat("INCLINE", Vector3.SignedAngle(hit.normal, Vector3.up, transform.right), .15f, Time.deltaTime);

            if (NavMeshAgent.hasPath)
            {
                Vector3 cross = Vector3.Cross(transform.forward, (NavMeshAgent.steeringTarget - transform.position).normalized);
                cross.z = 0;
                cross.x = 0;

                //transform.Rotate(cross * Time.deltaTime * _steeringVelocity, Space.Self);
                if (Mathf.Abs(steeringDot) > 0.25f)
                {
                    _desiredSpeed = 0;
                    NavMeshAgent.updateRotation = false;
                    NavMeshAgent.Move(transform.forward.normalized * Time.deltaTime * _walkSpeed / 2);
                    transform.Rotate(cross.normalized, _steeringVelocity * Time.deltaTime, Space.Self);
                    Debug.Log("TURN");
                }
                else
                {
                    //NavMesh.updatePosition = true;
                    NavMeshAgent.updateRotation = true;
                }
                _desiredSpeed = Mathf.Abs(steeringDot) < 0.25f && Vector3.Distance(transform.position, NavMeshAgent.destination) > 2 ? _runSpeed : _walkSpeed;
            }
        }

        public void Ragdoll()
        {
            Animator.enabled = false;
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
            }
        }

        //TODO: ADAPTAR AL GLOBAL AGENT SYSTEM
        [SerializeField] private float _runSpeed = 5f;

        [SerializeField] private float _walkSpeed = 1f;
        [SerializeField] private float _steeringVelocity;

        private float _desiredSpeed;
        private float _hurtStopVelocityMultiplier;
        private float _headLookWeightRefVelocity;
        private bool _allowLookAtPlayer;
        private bool _isFriendlyToPlayer;

        private bool _canEatFood => !_wantToAttackPlayer;

        public void Bark()
        {
            SubtitleParameters bark = new SubtitleParameters();
            bark.Transform = Head;
            bark.Location = Head.position;
            bark.FollowTransform = true;
            bark.Content = "Woof!";
            bark.Duration = 1f;

            UIService.CreateSubtitle(bark);
            Animator.SetTrigger("BARK");
        }

        private float _lastBarkTime;
        private float _barkInterval;

        public override void OnUpdate()
        {
            Animator.SetFloat("MOOD", _mood, .15f, Time.deltaTime);

            bool lookAtPlayer = IsPlayerInRange(2.1f) && IsPlayerInViewAngle(0.5f) && _isFriendlyToPlayer;
            _headRig.weight = Mathf.SmoothDamp(_headRig.weight, lookAtPlayer ? 1 : 0, ref _headLookWeightRefVelocity, .25f);

            if (lookAtPlayer)
            {
                _lookAtTarget.rotation = Quaternion.LookRotation((PlayerHeadPosition - _lookAtTarget.position).normalized);
            }

            _attackTarget = FindNearestAggroAgent();

            _hurtStopVelocityMultiplier = Mathf.Clamp(_hurtStopVelocityMultiplier + Time.deltaTime, 0, 1);
            NavMeshAgent.speed = _desiredSpeed * _hurtStopVelocityMultiplier;

            if (_isAttackingPlayer) NavMeshAgent.speed = 0;

            if (!IsDead && Time.time - _lastBarkTime > _barkInterval)
            {
                _lastBarkTime = Time.time;
                _barkInterval = Random.Range(2f, 10f);
                Bark();
            }
        }

        public override void OnHurt(AgentHurtPayload payload)
        {
            if (IsDead) return;
            SetHealth(GetHealth() - payload.Amount);
            Animator.SetTrigger("HURT");
            _hurtStopVelocityMultiplier = 0;
        }

        public override void OnHeardCombat()
        {
            if (_isFriendlyToPlayer) return;
            if (_mood == 0) SetMood(-1);
            _wantToAttackPlayer = true;
        }

        public AgentController FindNearestAggroAgent()
        {
            AgentController[] agents = AgentGlobalService.Instance.ActiveAgents.ToArray();
            if (agents.Length == 0) { return null; }
            agents = agents.Where(x => x.AgentGroup == Game.Life.AgentGroup.AGGRO).ToArray();
            if (agents.Length == 0) { return null; }
            AgentController result = agents.OrderBy(x => Vector3.Distance(x.transform.position, transform.position)).ToArray()[0];
            if (Vector3.Distance(result.transform.position, transform.position) > 5) { return null; }
            return result;
        }

        public GameObject FoodTarget => _foodItem;

        public AgentController AggroTarget { get => _attackTarget; }

        internal void SetMood(int v)
        {
            _mood = Mathf.Clamp(v, -1, 1);
        }

        public override void OnPlayerItemDropped(InventoryItem item, GameObject gameObject)
        {
            if (Vector3.Distance(transform.position, gameObject.transform.position) > 5) return;

            if (item is ConsumableItem && (item as ConsumableItem).CanConsumeDog) { _foodItem = gameObject; }
        }
    }

    public class DogDieState : BaseState
    {
        private DogAgentController _dog;

        public DogDieState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
        }

        public override void DrawGizmos()
        { }

        public override void End()
        { _dog.Animator.SetBool("MOVE", true); }

        public override void Start()
        {
            _dog.FaceTarget = true;
        }

        public override void Think()
        {
        }
    }

    public class DogRestState : BaseState
    {
        private DogAgentController _dog;

        private float _timeBetween = 2;
        private float _lastTime = 0;
        private Transform _camera;

        public DogRestState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
            _dog.Animator.SetBool("MOVE", true);
        }

        public override void Start()
        {
            _camera = Bootstrap.Resolve<PlayerService>().PlayerCamera.transform;
            _dog.FaceTarget = false;
        }

        public override void Think()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                Physics.Raycast(_dog.PlayerHeadPosition, _camera.forward, out RaycastHit hit, 100);
                _dog.SetTarget(hit.point);
            }

            if (Time.time - _lastTime > _timeBetween)
            {
                _lastTime = Time.time;
            }
        }
    }

    public class DogGoToPlayerState : BaseState
    {
        private DogAgentController _dog;

        public DogGoToPlayerState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
        }

        public bool IsInAttackRange => Vector3.Distance(_dog.PlayerPosition, _dog.transform.position) < 2.1f;

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(_dog.PlayerPosition + (_dog.transform.position - _dog.PlayerPosition).normalized * 2f, .35f);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _dog.FaceTarget = true;
        }

        public override void Think()
        {
            _dog.SetLookTarget(_dog.PlayerHeadPosition);

            if (_dog.HasPlayerVisual)
            {
                _dog.SetTarget(_dog.PlayerPosition + (_dog.transform.position - _dog.PlayerPosition).normalized * 1.25f);
            }
        }
    }

    public class DogGoToFoodState : BaseState
    {
        private DogAgentController _dog;

        public bool IsInEatRange => _dog.FoodTarget != null && Vector3.Distance(_dog.transform.position, _dog.FoodTarget.transform.position) < 2.1f;

        public DogGoToFoodState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
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
            _dog.SetTarget(_dog.FoodTarget.transform.position);
        }
    }

    public class DogEatState : BaseState
    {
        private DogAgentController _dog;

        public DogEatState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
        }

        private float eatTime = 2.5f;
        private float _startTime;
        private bool _hasEaten;
        public bool Ready => Time.time - _startTime > eatTime && _hasEaten;

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _startTime = Time.time;
            _hasEaten = true;
            _dog.Animator.SetTrigger("EAT");
            _dog.SetMood(1);
            GameObject.Destroy(_dog.FoodTarget);
            _dog.SetHealth(100);
        }

        public override void Think()
        {
        }
    }

    public class DogGoToAttackTargetState : BaseState
    {
        private DogAgentController _dog;

        public DogGoToAttackTargetState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
        }

        public bool IsInAttackRange => Vector3.Distance(_dog.AggroTarget.transform.position, _dog.transform.position) < 2.1f;

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
            _dog.SetTarget(_dog.AggroTarget.transform.position);
        }
    }

    public class DogAttackState : BaseState
    {
        private DogAgentController _dog;

        public DogAttackState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
        }

        public bool Ready
        { get { return (Time.time - _startTime > _attackDuration); } }

        public float _attackDuration = 2.1f;
        public float _startTime;

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _startTime = Time.time;
            _dog.Animator.SetTrigger("ATTACK");
            _dog.AggroTarget.Damage(85);
        }

        public override void Think()
        {
            _dog.transform.forward = _dog.AggroTarget.transform.position - _dog.transform.position;
            _dog.SetTarget(_dog.transform.position);
        }
    }

    public class DogAttackPlayerState : BaseState
    {
        private DogAgentController _dog;

        public DogAttackPlayerState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
        }

        public bool Ready
        { get { return (Time.time - _startTime > _attackDuration); } }

        public float _attackDuration = 2.1f;
        public float _startTime;

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _startTime = Time.time;
            _dog.Animator.SetTrigger("ATTACK");
            _dog.PlayerGameObject.GetComponent<PlayerHealth>().Hurt(25, _dog.PlayerHeadPosition - _dog.transform.position);
        }

        public override void Think()
        {
            _dog.transform.forward = _dog.PlayerPosition - _dog.transform.position;
            _dog.SetTarget(_dog.transform.position);
        }
    }

    public class DogScareState : BaseState
    {
        private DogAgentController _dog;

        public DogScareState(AgentController context) : base(context)
        {
            _dog = context as DogAgentController;
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
}