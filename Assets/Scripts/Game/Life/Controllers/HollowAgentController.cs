using Game.Entities;
using Game.Life;
using Game.Player.Controllers;
using Game.Player.Sound;
using Game.Service;
using Life.StateMachines;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Life.Controllers
{
    public class HollowAgentController : AgentController
    {
        private bool _shouldAttackPlayer
        {
            get
            {
                if (_needWarp) return false;
                return Vector3.Distance(transform.position, PlayerPosition) < 2;
            }
        }

        private bool _shouldGoToPlayer
        {
            get
            {
                {
                    if (_needWarp) return false;
                    return Vector3.Distance(transform.position, PlayerPosition) > 2;
                }
            }
        }

        private bool _shouldWarpFromPlayer
        {
            get
            {
                //if (Vector3.Distance(transform.position, PlayerPosition) > 20) return true;
                return _needWarp;
            }
        }

        [SerializeField] private SkinnedMeshRenderer _mesh;

        [Header("Sound")]
        [SerializeField] private AudioClipGroup _warpSFX;

        [SerializeField] private AudioClipGroup _hurtSFX;
        [SerializeField] private AudioClipGroup _attackSFX;
        [SerializeField] private AudioClipGroup _deadSFX;

        [Header("Perception")]
        [SerializeField] private float _playerConeAperture = .90f;

        //materialFields

        private float _refractionDesired;
        private float _opacityDesired;

        public override void OnStart()
        {
            CreateStates();
            CreateTransitions();
            SetMaxHealth(250);
            SetHealth(250);
            _desiredSpeed = _runSpeed;
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_busy, _goto, new FuncPredicate(() => _shouldGoToPlayer));
            Machine.AddTransition(_goto, _attack, new FuncPredicate(() => _shouldAttackPlayer));
            Machine.AddTransition(_attack, _goto, new FuncPredicate(() => _shouldGoToPlayer));
            Machine.AddTransition(_attack, _warp, new FuncPredicate(() => _shouldWarpFromPlayer));
            Machine.AddTransition(_goto, _warp, new FuncPredicate(() => _shouldWarpFromPlayer));
            Machine.AddTransition(_warp, _goto, new FuncPredicate(() => _shouldGoToPlayer));

            Machine.SetState(_busy);
        }

        private HollowAttackState _attack;
        private HollowGoToPlayerState _goto;
        private HollowBusyState _busy;
        private HollowDieState _die;
        private HollowWarpState _warp;

        private float _attackTime = .25f;

        public void AttackPlayer()
        {
            StartCoroutine(TryAttackPlayer((attackSuccesful) =>
            {
                if (attackSuccesful)
                {
                    _opacityDesired = .5f;
                    _needWarp = true;
                }
            }
            ));
        }

        private IEnumerator TryAttackPlayer(Action<bool> callback)
        {
            //wait for preattacktime
            AudioToolService.PlayClipAtPoint(_attackSFX.GetRandom(), Head.position, 1, AudioChannels.AGENT, 20f);
            Animator.SetTrigger("ATTACK");
            yield return new WaitForSeconds(_attackTime);
            // get distance
            bool succesful = Vector3.Distance(PlayerPosition, transform.position) < 2;
            if (succesful)
            {
                //if distance near enought hurt player.
                PlayerGameObject.GetComponent<PlayerHealth>().Hurt(_damageAmout, PlayerPosition - transform.position);
            }
            callback(succesful);
            yield return null;
        }

        public override void OnDeath()
        {
            AudioToolService.PlayPlayerSound(_deadSFX.GetRandom());
            _opacityDesired = .75f;
            Machine.ForceChangeToState(_die);
            Ragdoll();
            NavMeshAgent.isStopped = true;
        }

        public void Ragdoll()
        {
            Animator.enabled = false;
            foreach (LimbHitbox body in GetComponentsInChildren<LimbHitbox>(true))
            {
                body.Ragdoll();
            }
        }

        private void CreateStates()
        {
            _attack = new(this);
            _goto = new(this);
            _busy = new(this);
            _die = new(this);
            _warp = new(this);
        }

        //TODO: ADAPTAR AL GLOBAL AGENT SYSTEM
        [SerializeField] private float _runSpeed = 5f;

        private float _desiredSpeed;
        private float _hurtStopVelocityMultiplier;

        public override void OnUpdate()
        {
            ManageMaterial();

            if (Vector3.Dot((Head.position - PlayerHeadPosition).normalized, PlayerHead.forward) > _playerConeAperture)
            {
                if (IsPlayerVisible())
                {
                    _needWarp = true;
                    _opacityDesired = 1;
                }
            }

            _hurtStopVelocityMultiplier = Mathf.Clamp(_hurtStopVelocityMultiplier + Time.deltaTime, 0, 1);
            NavMeshAgent.speed = _desiredSpeed * _hurtStopVelocityMultiplier;
        }

        private void ManageMaterial()
        {
            if (!IsDead)
            {
                _opacityDesired = Mathf.Clamp(_opacityDesired - Time.deltaTime, -0.05f, 1);
                _refractionDesired = Mathf.Clamp(_refractionDesired - Time.deltaTime, 0, 2);
            }
            _mesh.material.SetFloat("_Opacity_Core", Mathf.Lerp(_mesh.material.GetFloat("_Opacity_Core"), _opacityDesired, Time.deltaTime * 2));
            _mesh.material.SetFloat("_RefractionSize", Mathf.Lerp(_mesh.material.GetFloat("_RefractionSize"), _refractionDesired, Time.deltaTime * 2));
        }

        public override void OnHurt(AgentHurtPayload payload)
        {
            if (IsDead) return;

            AudioToolService.PlayClipAtPoint(_hurtSFX.GetRandom(), Head.position, 1, AudioChannels.AGENT, 40f);
            _needWarp = true;
            SetHealth(GetHealth() - payload.Amount);
            Animator.SetTrigger("HURT");
            _hurtStopVelocityMultiplier = 0;
            _refractionDesired = 2;
            _opacityDesired = 1;
        }

        public Vector3 FindWarpLocation()
        {
            //maybe creating an array of random points for drama effect

            SpatialDataQuery query = new(new(this, PlayerPosition, PlayerHeadPosition, 1));
            if (query.UnsafePoints.Count > 0) return query.UnsafePoints[Random.Range(0, query.UnsafePoints.Count)].Position;
            else if ((query.AllPoints.Count > 0)) return query.AllPoints[Random.Range(0, query.AllPoints.Count)].Position;
            else return PlayerPosition;
        }

        private bool _needWarp;
        [SerializeField] private float _damageAmout;

        public void WarpAway()
        {
            AudioToolService.PlayClipAtPoint(_warpSFX.GetRandom(), Head.position, 1, AudioChannels.AGENT, 20f);
            _needWarp = false;
            transform.forward = (PlayerPosition - transform.position).normalized;
        }

        private float _lastKickTime;

        public override void Kick(Vector3 position, Vector3 direction, float damage)
        {
            if (Time.time - _lastKickTime > .1f)
            {
                Animator.SetTrigger("HURT");
                _hurtStopVelocityMultiplier = 0;
                _needWarp = true;
                _lastKickTime = Time.time;
                _hurtStopVelocityMultiplier = 0;
                _refractionDesired = 2;
                _opacityDesired = 1;
            }
        }
    }

    internal class HollowDieState : BaseState
    {
        private HollowAgentController blind;

        public HollowDieState(AgentController context) : base(context)
        {
            blind = context as HollowAgentController;
        }

        public override void DrawGizmos()
        { }

        public override void End()
        { }

        public override void Start()
        {
        }

        public override void Think()
        {
        }
    }

    public class HollowBusyState : BaseState
    {
        private HollowAgentController blind;

        public HollowBusyState(AgentController context) : base(context)
        {
            blind = context as HollowAgentController;
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

    public class HollowGoToPlayerState : BaseState
    {
        private HollowAgentController blind;

        public HollowGoToPlayerState(AgentController context) : base(context)
        {
            blind = context as HollowAgentController;
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(blind.PlayerPosition + (blind.transform.position - blind.PlayerPosition).normalized * 2f, .35f);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            blind.FaceTarget = false;
        }

        public override void Think()
        {
            blind.SetLookTarget(blind.PlayerHeadPosition);
            blind.SetTarget(blind.PlayerPosition);
        }
    }

    public class HollowAttackState : BaseState
    {
        private HollowAgentController blind;

        public HollowAttackState(AgentController context) : base(context)
        {
            blind = context as HollowAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            blind.FaceTarget = true;
            blind.AttackPlayer();
        }

        public override void Think()
        {
            blind.SetLookTarget(blind.PlayerHeadPosition);
        }
    }

    public class HollowWarpState : BaseState
    {
        private HollowAgentController _hollow;

        private float _start;

        public HollowWarpState(AgentController context) : base(context)
        {
            _hollow = context as HollowAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _hollow.Animator.SetTrigger("WARP");
            _hollow.SetTarget(_hollow.transform.position);
            _start = Time.time;
        }

        public override void Think()
        {
            if (Time.time - _start > .15f)
            {
                _hollow.NavMeshAgent.Warp(_hollow.FindWarpLocation());
                _hollow.WarpAway();
            }
        }
    }
}