using Core.Engine;
using Game.Entities;
using Game.Player;
using Game.Player.Controllers;
using Game.Service;
using Nomnom.RaycastVisualization;
using Rail;
using System;
using UnityEngine;

namespace Game.Train
{
    public delegate void ConnectedPartDelegate(TrainBase from, TrainBase to);

    public class TrainBase : MonoBehaviour, ITrainPart
    {
        [SerializeField] private Railroad _spawnRailroad;

        [Header("Coupled Cars")]
        [SerializeField] private TrainBase _connectedPartNext;

        [SerializeField] private TrainBase _connectedPartPrevious;

        [Header("Couple Points")]
        [SerializeField] private CouplingPoint[] _couplingPoints;

        [Header("Working Elements")]
        [SerializeField] private Bogie[] _bogies;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private ConfigurableJoint _currentJoin;

        private float _speed;
        private bool _active;
        private float _currentBreakForce;

        public float Speed => _speed;
        public Rigidbody Rigidbody => _rigidbody;
        public TrainBase ConnectedPartNext { get => _connectedPartNext; set => _connectedPartNext = value; }
        public TrainBase ConnectedPartPrevious { get => _connectedPartPrevious; set => _connectedPartPrevious = value; }
        public virtual float BrakeForce { get => _currentBreakForce; }
        public Railroad SpawnRailroad { get => _spawnRailroad; }
        internal Bogie[] Bogies { get => _bogies; }

        public void Activate()
        {
            _active = true;
        }

        public void CoupleTo(TrainBase part)
        {
            ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
            _currentJoin = joint;
            CouplingPoint p = GetNearestCouplingPointFrom(part.transform.position);
            p.IsCoupled = true;
            joint.anchor = transform.InverseTransformPoint(p.Position);
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = 0.02f;

            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.xMotion = ConfigurableJointMotion.Free;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.linearLimit = limit;
            joint.connectedBody = part.Rigidbody;

            part.NotifyConnectionFromPrevious(this);
            OnPartConnected(part);
            ConnectedPartNext = part;
            Debug.Log("Coupling to" + part.name);
            _active = true;
        }

        public void NotifyConnectionFromPrevious(TrainBase part)
        {
            _connectedPartPrevious = part;
        }

        private void Awake()
        {
            _bogies = GetComponentsInChildren<Bogie>();
            _rigidbody = GetComponent<Rigidbody>();
            foreach (Bogie bogie in _bogies)
            {
                bogie.SetCurrentRail(_spawnRailroad);
            }
            OnAwake();
        }

        private CouplingPoint GetNearestCouplingPointFrom(Vector3 from)
        {
            CouplingPoint point = _couplingPoints[0];
            float lastDistance = Vector3.Distance(point.Position, from);

            foreach (var couplingPoint in _couplingPoints)
            {
                float newDistance = Vector3.Distance(couplingPoint.Position, from);
                if (newDistance < lastDistance)
                {
                    lastDistance = newDistance;
                    point = couplingPoint;
                }
            }
            return point;
        }

        private void Start()
        {
            if (_connectedPartNext == this)
            {
                _connectedPartNext = null;
            }

            if (_connectedPartNext != null)
            {
                CoupleTo(_connectedPartNext);
            }

            foreach (Bogie bogie in _bogies)
            {
                if (bogie.transform == transform) break;

                bogie.Joint.connectedBody = _rigidbody;
                bogie.DerrailEvent += Derailed;
            }

            //Evaluar si hay un coche cerca para conectar
            p = Bootstrap.Resolve<PlayerService>().Player;
            OnStart();
        }

        private GameObject p;
        private Vector3 _lastVelocity;

        private void OnCollisionEnter(Collision collision)
        {




            if (collision.gameObject == p)
            {
                if (Vector3.Dot(collision.relativeVelocity, -Vector3.up) > 0.5f)
                {
                    if (collision.relativeVelocity.magnitude > .5f)
                    {
                        p.GetComponent<PlayerHealth>().Hurt(10000000);
                        p.GetComponent<PlayerRigidbodyMovement>().Push(collision.relativeVelocity);
                        _rigidbody.velocity = _lastVelocity;
                    }
                }
            }

            if (collision.gameObject.TryGetComponent(out LimbHitbox hitbox))
            {
                if (collision.relativeVelocity.magnitude > .5f)
                {
                    hitbox.RunOver(_rigidbody.velocity, 10000);
                }
                _rigidbody.velocity = _lastVelocity;
            }
        }

        public virtual void SetBreak(float value)
        {
            _currentBreakForce = value;

            foreach (Bogie bogie in _bogies)
            {
                bogie.SetBrakeForce(value);
            }
        }

        private void FindNearCars()
        {
            if (!_active) return;

            foreach (CouplingPoint point in _couplingPoints)
            {
                if (point.IsCoupled) continue;

                Ray ray = new Ray(point.Position, point.Forward);
                RaycastHit hit;

                if (VisualPhysics.Raycast(ray, out hit, .5f))
                {
                    //if (Input.GetKeyDown(KeyCode.K))
                    {
                        if (!hit.collider.gameObject.TryGetComponent(out TrainBase part)) return;
                        point.IsCoupled = true;
                        part.CoupleTo(this);
                        Debug.Log(hit.collider.name);
                        OnPartConnected(part);
                    }
                }
            }
        }

        public virtual void OnPartConnected(TrainBase part)
        {
        }

        private void FixedUpdate()
        {
            FindNearCars();

            _speed = Vector3.Magnitude(Rigidbody.velocity);
            _lastVelocity = _rigidbody.velocity;
            OnFixedUpdate();
        }

        private void Derailed(Vector3 inertia)
        {
            foreach (Bogie bogie in _bogies)
            {
                if (bogie.IsOnRail) { bogie.Derail(); }
            }
            Rigidbody.useGravity = true;
        }

        [Serializable]
        private class CouplingPoint
        {
            [SerializeField] private Transform _transform;
            [SerializeField] private bool _isCoupled;

            public Transform Point { get => _transform; set => _transform = value; }
            public bool IsCoupled { get => _isCoupled; set => _isCoupled = value; }
            public Vector3 Position => _transform.position;
            public Vector3 Forward => _transform.forward;
        }

        public virtual void OnFixedUpdate()
        {
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnAwake()
        {
        }
    }
}