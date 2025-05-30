﻿using Core.Engine;
using Game.Entities;
using Game.Inventory;
using Game.Life;
using Game.Player.Controllers;
using Game.Service;
using Life.StateMachines;
using Life.StateMachines.Interfaces;
using Nomnom.RaycastVisualization;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Life.Controllers
{

    public delegate void AgentPerceptionDelegate(AgentController controller);

    public delegate void AgentHurtDelegate(AgentHurtPayload payload, AgentController controller);

    public class AgentHurtPayload
    {
        public bool HurtByPlayer = false;
        public float Damage = 0;
        public Vector3 Source = Vector3.zero;
        public Vector3 Direction = Vector3.zero;
        public LimbHitbox Hitbox = null;

        public AgentHurtPayload(bool hurtByPlayer, float damage, Vector3 source, Vector3 direction, LimbHitbox hitbox)
        {
            HurtByPlayer = hurtByPlayer;
            Damage = damage;
            Source = source;
            Direction = direction;
            Hitbox = hitbox;
        }
    }

    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class AgentController : MonoBehaviour
    {
        private StateMachine _machine;
        private Animator _animator;
        private NavMeshAgent _navMeshAgent;

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

        public UnityAction<AgentController, bool> PlayerPerceptionEvent;

        public event AgentHurtDelegate HurtEvent;

        public event AgentPerceptionDelegate DeadEvent;

        public event AgentPerceptionDelegate HeardStepsEvent;

        public event AgentPerceptionDelegate HeardGunshotsEvent;

        public StateMachine Machine
        {
            get => _machine;
        }

        public Animator Animator
        {
            get => _animator;
        }

        public NavMeshAgent NavMeshAgent
        {
            get => _navMeshAgent;
        }

        public bool IsDead => _isDead;

        [Header("Player Perception")]
        [SerializeField] private float _thinkingInterval = .1f;

        [SerializeField] private AgentGroup _group;
        [SerializeField] private float _rangeDistance = 20;
        [SerializeField] private Transform _head;
        private LayerMask _ignoreMask;
        private GameObject _player;
        private Camera _playerCamera;
        private AgentGlobalSystem _agentGlobalsystem;
        private PlayerSoundController _playerSound;
        private InventorySystem _playerInventory;

        public bool CanLoseContact
        {
            get { return _canLoseContact; }
            set { _canLoseContact = value; }
        }

        public virtual bool GetPlayerDetection()
        {
            if (!PlayerService.Active) return false;
            if (!_canLoseContact) return true;
            if (IsPlayerInRange(2)) return true;
            return IsPlayerInRange(_rangeDistance) && IsPlayerInViewAngle(_currentViewAngle) && IsPlayerVisible();
        }

        public Vector3 PlayerPosition
        {
            get
            {
                if (!PlayerService.Active) return Vector3.zero;
                return _player.transform.position;
            }
        }

        public Vector3 PlayerHeadPosition
        {
            get
            {
                if (!PlayerService.Active) return Vector3.zero;
                return _playerCamera.transform.position;
            }
        }

        public Transform PlayerTransform => _player.transform;
        public Transform PlayerHead => _playerCamera.transform;
        public bool HasPlayerVisual => GetPlayerDetection();
        public GameObject PlayerGameObject { get => _player; }
        public Transform Head => _head;
        public AgentGlobalSystem AgentGlobalSystem => _agentGlobalsystem;
        public float DetectionRange { get => _rangeDistance; }
        public bool Initialized { get; private set; }
        public AgentGroup AgentGroup => _group;
        public Vector3 AimTarget => _aimTarget;
        public float ThinkingInterval { get => _thinkingInterval; }

        public virtual void Restore()
        {
            SetMaxHealth(_maxHealth);
            SetHealth(_maxHealth);
        }

        internal void AllowThinking(bool value)
        {
            _isStopped = !value;
        }

        private void Start()
        {
            _agentGlobalsystem = AgentGlobalService.Instance;
            _agentGlobalsystem.RegisterAgent(this);
            _machine = new StateMachine();

            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false;
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.updatePosition = true;
            _navMeshAgent.speed = 3;

            SetLimboxes();

            //TODO: IF THE PLAYER IS NOT SPAWNED, MAKE ALREADY SPAWNED.
            if (!PlayerService.Active)
            {
                PlayerService.PlayerSpawnEvent += OnPlayerSpawn;
            }
            else
            {
                SetUpPlayerReference();
            }

            Initialized = true;
            OnStart();
        }

        private void SetLimboxes()
        {
            foreach (LimbHitbox hitbox in GetComponentsInChildren<LimbHitbox>(true))
            {
                hitbox.LimbHitEvent += OnLimbHit;
            }
        }

        private void OnLimbHit(LimboxHit payload)
        {
            OnLimbHurt(payload);
        }

        private void SetUpPlayerReference()
        {
            _player = Bootstrap.Resolve<PlayerService>().Player;
            _playerCamera = Bootstrap.Resolve<PlayerService>().PlayerCamera;
            _ignoreMask = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.IgnoreLayers;
            _playerSound = _player.GetComponentInChildren<PlayerSoundController>();
            _playerInventory = InventoryService.Instance;
            _playerSound.StepSound += OnPlayerStep;
            _playerSound.GunSound += OnPlayerGun;
            _playerInventory.DropItem += OnPlayerDropped;
        }

        private void OnPlayerSpawn(GameObject player)
        {
            SetUpPlayerReference();
        }

        private void Update()
        {
            if (!Initialized) return;

            ManagePerception();
            ManageDeath();
            UpdateMovement();

            if (_isStopped) return;

            if (Time.time - _lastThinkMoment > _thinkingInterval)
            {
                Think();
                _lastThinkMoment = Time.time;
            }

            OnUpdate();
        }

        private void ManageDeath()
        {
            if (_alive && _health <= 0)
            {
                _alive = false;
                _playerSound.StepSound -= OnPlayerStep;
                _playerSound.GunSound -= OnPlayerGun;
                _agentGlobalsystem.DiscardAgent(this);
                DeadEvent?.Invoke(this);

                OnDeath();
            }
        }

        private void ManagePerception()
        {
            if (IsDead) return;

            if (_lastHasPlayerVisual != HasPlayerVisual)
            {
                PlayerPerceptionEvent?.Invoke(this, HasPlayerVisual);
                OnPlayerDetectionChanged(HasPlayerVisual);
                _lastHasPlayerVisual = HasPlayerVisual;
            }
        }

        public void Think()
        {
            if (!Initialized) return;

            if (!AgentGlobalService.AIDisabled)
            {
                _machine?.Update();
            }
        }

        private void OnPlayerDropped(InventoryItem item, GameObject gameObject)
        {
            OnPlayerItemDropped(item, gameObject);
        }

        private void OnPlayerGun(Vector3 position, float radius)
        {
            if (AgentGlobalService.IgnorePlayer) return;
            if (Vector3.Distance(position, transform.position) <= radius)
            {
                HeardGunshotsEvent?.Invoke(this);
                OnHeardCombat();
            }
        }

        private void OnPlayerStep(Vector3 position, float radius)
        {
            if (AgentGlobalService.IgnorePlayer) return;
            if (Vector3.Distance(position, transform.position) <= radius)
            {
                HeardStepsEvent?.Invoke(this);
                OnHeardSteps();
            }
        }

        public bool IsPlayerInRange(float distance)
        {
            if (AgentGlobalService.IgnorePlayer) return false;
            if (!PlayerService.Active) return false;

            return Vector3.Distance(transform.position + transform.up * 2f, _playerCamera.transform.position) < distance;
        }

        public bool IsPlayerInViewAngle(float dotAngle)
        {
            if (AgentGlobalService.IgnorePlayer) return false;
            if (!PlayerService.Active) return false;
           
            return Vector3.Dot(transform.forward, (_playerCamera.transform.position - _head.position).normalized) > dotAngle;
        }

        public Vector3 PlayerOccluderPosition;
        public GameObject PlayerOccluderGameObject;

        public bool IsPlayerVisible(Vector3 from)
        {
            if (AgentGlobalService.IgnorePlayer) return false;
            if (!PlayerService.Active) return false;
            //Debug.DrawLine(_playerCamera.transform.position, transform.position);

            if (VisualPhysics.SphereCast(_playerCamera.transform.position, 0.025f, from - _playerCamera.transform.position, out RaycastHit hit, _rangeDistance, _ignoreMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.transform.root == transform)
                {
                    PlayerOccluderPosition = Vector3.zero;
                    PlayerOccluderGameObject = null;
                    return true;
                }
                else
                {
                    PlayerOccluderPosition = hit.point;
                    PlayerOccluderGameObject = hit.transform.gameObject;

                    return false;
                }
            }
            PlayerOccluderPosition = Vector3.zero;
            PlayerOccluderGameObject = null;
            return false;
        }
        public bool IsPlayerVisible()
        {
            return IsPlayerVisible(_head.position);
        }
        private Vector3 _aimTarget;
        private bool _faceTarget;
        private bool _alive = false;
        [Header("Movement")]
        [SerializeField] private float _minMoveDistance = 1f;
        public float MinMoveDistance { get => _minMoveDistance; set => _minMoveDistance = value; }

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
                if (value != _faceTarget)
                {
                    Animator.SetLayerWeight(2, value ? 1 : 0);
                    _animator.SetBool("FACETARGET", value);
                }
                _faceTarget = value;
            }
        }
        public float Height { get => _height; }
        public float CrouchHeight { get => _crouchHeight; }
        public Vector3 Destination { get => _navMeshAgent.destination; }

        private bool _lastHasPlayerVisual;
        [SerializeField] private float _crouchHeight = 1.75f;
        [SerializeField] private float _height = 1f;
        [SerializeField] private float _currentViewAngle = .3f;
        private float _lastThinkMoment;
        private bool _isStopped;
        private bool _canLoseContact;

        public float ViewAngle { get => _currentViewAngle; set => _currentViewAngle = value; }

        public virtual void UpdateMovement()
        {
            if (!_alive) return;
            _navMeshAgent.updateRotation = !_faceTarget;
      
            var aimDir = (_aimTarget - _head.position).normalized;
            float aim_horizontal = _faceTarget ? Vector3.Cross(transform.forward, aimDir).y : 0;
            float aim_vertical = _faceTarget ? Vector3.Dot(transform.up, aimDir) : 0;


            if (Vector3.Distance(transform.position, _navMeshAgent.destination) < _minMoveDistance)
            {
                _navMeshAgent.ResetPath();
            }
            Vector3 relativeVelocity = transform.InverseTransformDirection(_navMeshAgent.velocity);
            Debug.DrawRay(transform.position, relativeVelocity);

            _animator.SetFloat("mov_turn", aim_horizontal * _navMeshAgent.angularSpeed * Time.deltaTime, .05f, Time.deltaTime);

            if (_faceTarget){
                transform.Rotate(Vector3.up, aim_horizontal * _navMeshAgent.angularSpeed * Time.deltaTime);
            }

            _animator.SetFloat("mov_right", relativeVelocity.x, .05f, Time.deltaTime);
            _animator.SetFloat("mov_forward", relativeVelocity.z, .05f, Time.deltaTime);
            _animator.SetFloat("aim_vertical", aim_vertical, .05f, Time.deltaTime);
        }

        private void OnDestroy()
        {
            PlayerService.PlayerSpawnEvent -= OnPlayerSpawn;

            if (PlayerService.Active)
            {
                _playerSound.StepSound -= OnPlayerStep;
                _playerSound.GunSound -= OnPlayerGun;
                _playerInventory.DropItem -= OnPlayerDropped;
            }

            AgentGlobalSystem.DiscardAgent(this);
            OnDestroyAgent();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying && _machine != null)
            {
                _machine.DrawGizmos();
            }
            Gizmos.DrawLine(_head.position, _aimTarget);
            DrawGizmos();
        }

        #region Virtual Methods

        public virtual void DrawGizmos()
        { }

        public virtual void OnUpdate()
        { }

        public virtual void OnStart()
        { }

        public virtual void OnDeath()
        { }

        public virtual void OnPlayerDetectionChanged(bool detected)
        { }

        
        public virtual void OnHeardCombat()
        { }

        public virtual void OnHeardSteps()
        { }

        public virtual void KillAndPush(Vector3 velocity)
        { }

        public virtual void KillAndPush(Vector3 velocity, LimbHitbox hitbox)
        { }

        public virtual void ForcePlayerPerception()
        { }

        public virtual void OnPlayerItemDropped(InventoryItem item, GameObject gameObject)
        { }

        public virtual void Kick(Vector3 position, Vector3 direction, float damage)
        { }

        public virtual void OnLimbHurt(LimboxHit payload)
        { }

        public virtual void OnDestroyAgent()
        { }

        #endregion Virtual Methods
    }
}