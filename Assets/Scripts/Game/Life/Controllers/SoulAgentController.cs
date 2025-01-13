using Game.Entities;
using Game.Hit;
using Game.Service;
using Life.StateMachines;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Life.Controllers
{
    public class SoulAgentController : AgentController, IInteractable, IHittableFromWeapon
    {
        [SerializeField] private Transform _shell;
        [SerializeField] private Transform _core;
        [SerializeField] private AnimationCurve _interactAnimation;
        [SerializeField] private Light[] _lights;
        private float _hoverIntensity;
        private Vector3 _coreRefVelocity;
        private Vector3 _bodyRefVelocity;
        [SerializeField] private Transform _body;
        [SerializeField] private float _bodyMovement = .15f;

        public override void OnUpdate()
        {
            Animate();
        }

        private void Animate()
        {
            _body.localPosition = Vector3.SmoothDamp(_body.localPosition, Random.insideUnitSphere * _bodyMovement + Vector3.up * 1.5f, ref _bodyRefVelocity, 1);
            _shell.localPosition = new Vector3(0, MathF.Sin(Time.time) * _hoverIntensity * NavMeshAgent.velocity.magnitude, 0);
            _core.position = Vector3.SmoothDamp(_core.position, _shell.position, ref _coreRefVelocity, .4f);

            foreach (Light light in _lights)
            {
                light.color = Color.Lerp(light.color, Random.ColorHSV(0, 1, 0, 1, .8f, 1), Time.deltaTime * 4);
            }
        }

        public override void OnStart()
        {
            SetMaxHealth(1000000);
            SetHealth(1000000);
            CreateStates();
            CreateTransitions();
        }

        public override bool GetPlayerDetection()
        {
            return IsPlayerInRange(35);
        }

        private void CreateTransitions()
        {
            Machine.AddTransition(_wander, _follow, new FuncPredicate(() => _followPlayer));
            Machine.AddTransition(_follow, _wander, new FuncPredicate(() => _lostPlayer));
            Machine.AddTransition(_follow, _wander, new FuncPredicate(() => !_followPlayer));

            //todo: fix perception loop bug
            Machine.AddAnyTransition(_merge, new FuncPredicate(() => _canMerge));

            Machine.SetState(_wander);
        }

        private SoulWanderState _wander;
        private SoulFollowState _follow;
        private SoulMergeState _merge;
        private bool _canMerge;
        private bool _followPlayer;
        private bool _canInteract;
        private bool _lostPlayer => !GetPlayerDetection() && _followPlayer;

        private void CreateStates()
        {
            _wander = new(this);
            _follow = new(this);
            _merge = new(this);
        }

        public void SetFollowPlayer(bool value) => _followPlayer = value;

        bool IInteractable.BeginInteraction(Vector3 position)
        {
            _followPlayer = !_followPlayer;
            StartCoroutine(AnimateInteraction());
            return true;
        }

        private IEnumerator AnimateInteraction()
        {
            Vector3 scale = _shell.localScale;
            float duration = 1;
            float time = 0;
            while (time < duration)
            {
                _shell.localScale = scale * _interactAnimation.Evaluate(time / duration);
                time += 0.01f;
                yield return null;
            }
        }

        bool IInteractable.IsDone(bool cancelRequest) => true;

        bool IInteractable.CanInteract() => _canInteract;

        void IHittableFromWeapon.Hit(HitWeaponEventPayload payload)
        {
            Debug.Log("Soul Hit");
        }
    }

    public class SoulWanderState : BaseState
    {
        private SoulAgentController _soul;

        public SoulWanderState(AgentController context) : base(context)
        {
            _soul = context as SoulAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _soul.SetFollowPlayer(false);
            UIService.CreateMessage("This soul is no longer following you.");
        }

        public override void Update()
        {
            _soul.SetTarget(_soul.transform.forward + UnityEngine.Random.insideUnitSphere * 10); ;
        }
    }

    public class SoulFollowState : BaseState
    {
        private SoulAgentController _soul;

        public SoulFollowState(AgentController context) : base(context)
        {
            _soul = context as SoulAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            UIService.CreateMessage("This soul is now following you.");
        }

        public override void Update()
        {
            if (Vector3.Distance(_soul.transform.position, _soul.PlayerHeadPosition) > 2) _soul.SetTarget(_soul.PlayerPosition);
        }
    }

    public class SoulMergeState : BaseState
    {
        private SoulAgentController _soul;

        public SoulMergeState(AgentController context) : base(context)
        {
            _soul = context as SoulAgentController;
        }

        public override void DrawGizmos()
        {
            throw new System.NotImplementedException();
        }

        public override void End()
        {
            throw new System.NotImplementedException();
        }

        public override void Start()
        {
            UIService.CreateMessage("This soul has found a body.");
        }

        public override void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}