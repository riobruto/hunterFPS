using System;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Life
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class AgentMoveBehavior : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private Animator _animator;
        private Vector3 _aimTarget;

        [SerializeField] private float _minMoveDistance = 1f;
        private Vector3 _lastPosition;
        private Vector3 CurrentTarget;

        [SerializeField] private Transform _aimTransform;

        private bool _faceTarget;
        private float _desiredRigWeight;
        private float _lastYRotation;
        public NavMeshAgent Agent => _agent;
        public Animator Animator => _animator;
        public bool FaceTarget { get => _faceTarget; set => _faceTarget = value; }

        public void SetTarget(Vector3 position)
        {
            _agent.isStopped = false;
            _agent.SetDestination(position);
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false;
            _agent.updateRotation = false;
            _agent.updatePosition = true;
        }

        // Update is called once per frame
        private void Update()
        {
            //_aimTarget = Bootstrap.Resolve<PlayerSpawnerService>().Player.transform.position;

            var aimDir = (_aimTarget - transform.position).normalized;

            float aim_horizontal = _faceTarget ? Vector3.Cross(transform.forward, aimDir).y : 0;
            float aim_vertical = _faceTarget ? Vector3.Dot(transform.up, aimDir) : 0;
             //_animator.SetFloat("aim_vertical", aim_vertical, .0125f, Time.deltaTime);

            if (Vector3.Distance(transform.position, _agent.destination) < _minMoveDistance)
            {
                _agent.ResetPath();
            }
            Vector3 relativeVelocity = transform.InverseTransformDirection(_agent.velocity);
            Debug.DrawRay(transform.position, relativeVelocity);

            _animator.SetFloat("mov_turn", aim_horizontal * _agent.angularSpeed * Time.deltaTime, .0125f, Time.deltaTime);

            if (_faceTarget)
            {
                transform.Rotate(Vector3.up, aim_horizontal * _agent.angularSpeed * Time.deltaTime);
            }

            _animator.SetFloat("mov_right", relativeVelocity.x, .05f, Time.deltaTime);
            _animator.SetFloat("mov_forward", relativeVelocity.z, .05f, Time.deltaTime);
            _animator.SetFloat("aim_vertical", aim_vertical, .05f, Time.deltaTime);
        }

        public void SetLookTarget(Vector3 target) => _aimTarget = target;

        internal void StartPatrol()
        {
            _faceTarget = false;
            _desiredRigWeight = 0;
            _agent.updateRotation = true;

            _animator.SetBool("PATROL", true);
            _animator.SetBool("WARNING", false);
        }

        internal void StopPatrol()
        {
            _animator.SetBool("PATROL", false);
        }

        internal void StartWarning()
        {
            // _agent.updateRotation = false;
            StopPatrol(); _faceTarget = true;
            _agent.updateRotation = false;
            _desiredRigWeight = 1;
            _animator.SetBool("WARNING", true);
        }

        internal void StopWarning()
        {
            _faceTarget = false;
            //_agent.updateRotation = true;

            StopPatrol();
            _desiredRigWeight = 0;
            _animator.SetBool("WARNING", false);
        }

        internal void StartRun()
        {
            _faceTarget = false;
            _animator.SetBool("RUN", true);
            _animator.SetBool("CROUCH", false);
            _animator.SetBool("PATROL", false);
        }

        internal void StopRun()
        {
            _animator.SetBool("RUN", false);
        }

        internal void Crouch(bool v)
        {
            _animator.SetBool("RUN", false);
            _animator.SetBool("CROUCH", v);
        }

        internal void Heal()
        {
            _animator.SetTrigger("HEAL");
        }

    }
}