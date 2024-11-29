using Game.Entities;
using Life.StateMachines;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Life.Controllers
{
    public class FighterAgentController : AgentController
    {
        private bool _isAttackingPlayer;

        private float _timeToForgetPlayer = 20;
        private float _lastReportTime;

        private bool _forgotPlayer => Time.realtimeSinceStartup - _lastReportTime > _timeToForgetPlayer;

        public override void OnStart()
        {
            CreateStates();
            CreateTransitions();

            SetMaxHealth(100);
            SetHealth(100);
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_rest, _goto, new FuncPredicate(() => PlayerVisualDetected));
            Machine.AddTransition(_rest, _goto, new FuncPredicate(() => PendingPlayerSoundChase && !PlayerVisualDetected));
            Machine.AddTransition(_goto, _attack, new FuncPredicate(() => IsPlayerInRange(1.5f)));
            Machine.AddTransition(_attack, _goto, new FuncPredicate(() => !_isAttackingPlayer && !_forgotPlayer));
            Machine.AddTransition(_attack, _rest, new FuncPredicate(() => !_isAttackingPlayer && _forgotPlayer));
            Machine.AddTransition(_goto, _rest, new FuncPredicate(() => _forgotPlayer));

            Machine.SetState(_rest);
        }

        private BlindAttackState _attack;
        private BlindGoToPlayerState _goto;
        private BlindRestState _rest;
        private BlindDieState _die;

        private float _attackTime = 1;

        private IEnumerator AttackPlayer()
        {
            _isAttackingPlayer = true;
            Animator.SetInteger("ATTACKTYPE", Random.Range(0, 2));
            Animator.SetTrigger("MEELEE");
            yield return new WaitForSeconds(_attackTime);
            {
                Debug.Log("NIGGER WAS ATTACKED, ALLEGEDLY");
            }
            _isAttackingPlayer = false;
            _lastReportTime = Time.realtimeSinceStartup;
            yield return null;
        }

        public override void OnDeath()
        {
            Machine.ForceChangeToState(_die);
            Ragdoll();
            NavMesh.isStopped = true;
        }

        public void Ragdoll()
        {
            Animator.enabled = false;
            foreach (CharacterLimbHitbox body in GetComponentsInChildren<CharacterLimbHitbox>(true))
            {
                body.Ragdoll();
            }
        }

        private void CreateStates()
        {
            _attack = new(this);
            _goto = new(this);
            _rest = new(this);
            _die = new(this);
        }

        public bool PendingPlayerSoundChase;

        internal void BeginAttackPlayer()
        {
            StartCoroutine(AttackPlayer());
        }
        //TODO: ADAPTAR AL GLOBAL AGENT SYSTEM
        private float _runSpeed = 5f;
        private float _walkSpeed = 3f;
        private float _patrolSpeed = 1f;
        private float _crouchSpeed = 1f;
        private float _desiredSpeed;

        private SoldierMovementType _current;
        private float _hurtStopVelocityMultiplier;

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
                    _desiredSpeed = _runSpeed;
                    break;

                case SoldierMovementType.WALK:
                    if (_current == SoldierMovementType.CROUCH)
                    {
                        Animator.SetBool("WARNING", true);
                        StartCoroutine(SetCrouch(false, _walkSpeed));
                        break;
                    }
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

        public override void OnUpdate()
        {
            _hurtStopVelocityMultiplier = Mathf.Clamp(_hurtStopVelocityMultiplier + Time.deltaTime, 0, 1);
            NavMesh.speed = _desiredSpeed * _hurtStopVelocityMultiplier;

            if (PlayerVisualDetected)
            {
                _lastReportTime = Time.realtimeSinceStartup;
            }

            if (_isAttackingPlayer) NavMesh.speed = 0;
        }

        public override void OnHurt(float value)
        {
            if (IsDead) return;

            SetHealth(GetHealth() - value);

            Animator.SetTrigger("HURT");

            _hurtStopVelocityMultiplier = 0;
        }
    }

    internal class BlindDieState : BaseState
    {
        private FighterAgentController blind;

        public BlindDieState(AgentController context) : base(context)
        {
            blind = context as FighterAgentController;
        }

        public override void DrawGizmos()
        { }

        public override void End()
        { }

        public override void Start()
        {
        }

        public override void Update()
        {
        }
    }

    public class BlindRestState : BaseState
    {
        private FighterAgentController blind;

        public BlindRestState(AgentController context) : base(context)
        {
            blind = context as FighterAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            blind.SetMovementType(SoldierMovementType.CROUCH);
        }

        public override void Update()
        {
           // blind.SetTarget(Vector3.zero);
        }
    }

    public class BlindGoToPlayerState : BaseState
    {
        private FighterAgentController blind;

        public BlindGoToPlayerState(AgentController context) : base(context)
        {
            blind = context as FighterAgentController;
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(blind.PlayerPosition + (blind.transform.position - blind.PlayerPosition).normalized * 2f, .35f);
        }

        public override void End()
        {
            blind.PendingPlayerSoundChase = false;
        }

        public override void Start()
        {
            blind.FaceTarget = true;
            blind.SetMovementType(SoldierMovementType.RUN);

        }

        public override void Update()
        {
            blind.SetLookTarget(blind.PlayerHeadPosition);

            if (blind.PlayerVisualDetected)
            {
                blind.SetTarget(blind.PlayerPosition + (blind.transform.position - blind.PlayerPosition).normalized * 1.25f);
            }
        }
    }

    public class BlindAttackState : BaseState
    {
        private FighterAgentController blind;

        public BlindAttackState(AgentController context) : base(context)
        {
            blind = context as FighterAgentController;
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
            blind.SetMovementType(SoldierMovementType.WALK);
            blind.BeginAttackPlayer();
        }

        public override void Update()
        {
            blind.SetLookTarget(blind.PlayerHeadPosition);
        }
    }
}