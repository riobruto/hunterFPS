using Core.Engine;
using Game.Life;
using Game.Player.Controllers;
using Game.Service;
using Life.StateMachines;
using Life.StateMachines.Interfaces;
using Nomnom.RaycastVisualization;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Life.Controllers
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class AgentController : MonoBehaviour
    {
        private StateMachine _machine;

        private Animator _animator;
        private NavMeshAgent _navMeshAgent;

        public bool Initialized { get; private set; }

        private float _health;
        private float _maxHealth;
        private bool _isDead => !_alive;

        public IState CurrentState => _machine.CurrentState;

        public float GetHealth() => _health;

        public void SetHealth(float value)
        {
            _health = Mathf.Clamp(value, 0, _maxHealth);
        }

        public float GetMaxHealth() => _maxHealth;

        public void SetMaxHealth(float value)
        {
            _maxHealth = value;
            _alive = true;
        }

        public UnityAction<float> HealthChangedEvent;
        public UnityAction DeadEvent;
        public UnityAction<bool> PlayerPerceptionEvent;

        public StateMachine Machine
        {
            get => _machine;
        }

        public Animator Animator
        {
            get => _animator;
        }

        public NavMeshAgent NavMesh
        {
            get => _navMeshAgent;
        }

        public bool IsDead => _isDead;

        [Header("Player Perception")]
        [SerializeField] private LayerMask _ignoreMask;

        [SerializeField] private AgentGroup _group;
        [SerializeField] private Transform _head;
        [SerializeField] private float _rangeDistance = 20;
        private GameObject _player;
        private Camera _playerCamera;
        private AgentGlobalSystem _agentGlobalsystem;
        private PlayerSoundController _playerSound;
        private bool _playerDetected => IsPlayerInRange(_rangeDistance) && IsPlayerInViewAngle(-0.3f)  && IsPlayerVisible();

        public Vector3 PlayerPosition => _player.transform.position;
        public Vector3 PlayerHeadPosition => _playerCamera.transform.position;
        public bool PlayerVisualDetected => _playerDetected;
        public GameObject PlayerGameObject { get => _player; }
        public Vector3 LastPlayerKnownPosition => _lastKnownPosition;
        public Transform Head => _head;
        public AgentGlobalSystem AgentGlobalSystem => _agentGlobalsystem;

        public AgentGroup AgentGroup => _group;

       
        private void Start()
        {
            _machine = new StateMachine();

            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false;
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.updatePosition = true;
            _navMeshAgent.speed = 3;

            _player = Bootstrap.Resolve<PlayerService>().Player;
            _playerCamera = Bootstrap.Resolve<PlayerService>().PlayerCamera;
            _ignoreMask = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.IgnoreLayers;

            _playerSound = _player.GetComponentInChildren<PlayerSoundController>();
            _playerSound.StepSound += OnPlayerStep;
            _playerSound.GunSound += OnPlayerGun;

            _agentGlobalsystem = Bootstrap.Resolve<AgentGlobalService>().Instance;
            _agentGlobalsystem.RegisterAgent(this);
            Initialized = true;
            OnStart();
        }

        private void Update()
        {
            if (!Initialized) return;

            _machine?.Update();
            UpdateMovement();

            if (_lastPlayerDetected != _playerDetected)
            {
                _lastKnownPosition = _player.transform.position;
                PlayerPerceptionEvent?.Invoke(_playerDetected);
                _lastPlayerDetected = _playerDetected;
            }

            if (_alive && _health <= 0 && _maxHealth != 0)
            {
                _playerSound.StepSound -= OnPlayerStep;
                _playerSound.GunSound -= OnPlayerGun;
                _agentGlobalsystem.DiscardAgent(this);
                OnDeath();
                _alive = false;
            }

            OnUpdate();
        }

        private void OnPlayerGun(Vector3 position, float radius)
        {
            if (Vector3.Distance(position, transform.position) <= radius)
            {
                OnHeardCombat();
            }
        }

        private void OnPlayerStep(Vector3 position, float radius)
        {
            if (Vector3.Distance(position, transform.position) <= radius)
            {
                OnHeardSteps();
            }
        }

        public bool IsPlayerInRange(float distance)
        {
            return Vector3.Distance(_head.position, _playerCamera.transform.position) < distance;
        }

        public bool IsPlayerInViewAngle(float dotAngle)
        {
            return Vector3.Dot(transform.forward, _playerCamera.transform.position - transform.position) > dotAngle;
        }

        public bool IsPlayerVisible()
        {
            //Debug.DrawLine(_playerCamera.transform.position, transform.position);

            if (VisualPhysics.Linecast(_playerCamera.transform.position, _head.position, out RaycastHit hit, _ignoreMask))
            {
                return hit.collider.gameObject.transform.root == transform;
            }

            return false;
        }

        private Vector3 _aimTarget;
        private bool _faceTarget;
        private bool _alive = false;

        [Header("Movement")]
        [SerializeField] private float _minMoveDistance = 1f;

        [SerializeField] private Transform _aimTransform;

        public void SetLookTarget(Vector3 target) => _aimTarget = target;

        public void SetTarget(Vector3 position)
        {
            _navMeshAgent.isStopped = false;
            _navMeshAgent.SetDestination(position);
        }

        public bool FaceTarget
        {
            get => _faceTarget;
            set
            {
                _faceTarget = value;
                _animator.SetBool("FACETARGET", value);
            }
        }

        private Vector3 _lastKnownPosition;
        private bool _lastPlayerDetected;

        private void UpdateMovement()
        {
            if (!_alive) return;
            _navMeshAgent.updateRotation = !_faceTarget;
            //_aimTarget = Bootstrap.Resolve<PlayerSpawnerService>().Player.transform.position;

            var aimDir = (_aimTarget - _head.position).normalized;
            float aim_horizontal = _faceTarget ? Vector3.Cross(transform.forward, aimDir).y : 0;
            float aim_vertical = _faceTarget ? Vector3.Dot(transform.up, aimDir) : 0;

            //_animator.SetFloat("aim_vertical", aim_vertical, .0125f, Time.deltaTime);

            if (Vector3.Distance(transform.position, _navMeshAgent.destination) < _minMoveDistance)
            {
                _navMeshAgent.ResetPath();
            }
            Vector3 relativeVelocity = transform.InverseTransformDirection(_navMeshAgent.velocity);
            Debug.DrawRay(transform.position, relativeVelocity);

            _animator.SetFloat("mov_turn", aim_horizontal * _navMeshAgent.angularSpeed * Time.deltaTime, .05f, Time.deltaTime);

            if (_faceTarget)
            {
                transform.Rotate(Vector3.up, aim_horizontal * _navMeshAgent.angularSpeed * Time.deltaTime);
            }

            _animator.SetFloat("mov_right", relativeVelocity.x, .05f, Time.deltaTime);
            _animator.SetFloat("mov_forward", relativeVelocity.z, .05f, Time.deltaTime);
            _animator.SetFloat("aim_vertical", aim_vertical, .05f, Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying && _machine != null)
            {
                _machine.DrawGizmos();
            }

            Gizmos.DrawLine(_head.position, _aimTarget);
        }

        #region Virtual Methods

        public virtual void OnUpdate()
        {
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnDeath()
        {
        }

        public virtual void OnHurt(float value)
        { }

        public virtual void OnHeardCombat()
        { }

        public virtual void OnHeardSteps()
        { }

        internal void NotifyHurt(float value)
        {
            OnHurt(value);
        }

        #endregion Virtual Methods
    }
}