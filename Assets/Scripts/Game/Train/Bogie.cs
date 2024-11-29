using Core.Engine;
using Game.Service;
using Rail;
using Unity.Collections;
using UnityEngine;

namespace Game.Train
{
    public delegate void RailChangedDelegate(Railroad current, Railroad next);

    public delegate void RailJointDelegate();

    public delegate void BogieDerailedDelegate(Vector3 inertia);

    public class Bogie : MonoBehaviour
    {
        public event RailChangedDelegate RailChangedEvent;

        public event RailJointDelegate RailJointEvent;

        public event BogieDerailedDelegate DerrailEvent;

        public Rigidbody rb;
        [SerializeField] private HingeJoint _joint;
        [SerializeField] private Railroad _currentRail;

        public HingeJoint Joint => _joint;
        public float Speed => rb.velocity.magnitude;
        public Vector3 Velocity => rb.velocity;
        public bool IsOnRail { get => _isOnRail; }
        public float SidewayStress => _sidewayStress;
        private bool _isOnRail;
        private float _breakForce;
        private int _lastTime;
        private Vector3 _inertia;
        private float _sidewayStress => transform.InverseTransformDirection(_inertia).x;
        private int _currentRailSplineIndex;

        private void Awake()
        {
            _joint = GetComponent<HingeJoint>();
        }

        private void Start()
        {
            _isOnRail = _currentRail != null;

            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.useGravity = false;
        }

        private int timeSegment = 0;

        private void FixedUpdate()
        {
            if (!_isOnRail)
            {
                return;
            }

            _currentRailSplineIndex = _currentRail.ActiveSplineIndex;

            RailData data = _currentRail.GetRailDataFromPoint(transform.position, _currentRailSplineIndex);
            ManageCurrentRail(data);

            Vector3 currentForward = transform.forward;
            if (Vector3.Dot(rb.velocity, transform.forward) < 0)
            {
                currentForward *= -1;
            }
            transform.position = data.NearestPosition;
            ManageRailSegments(data);
            Vector3 forward = Vector3.Normalize(data.Tangent);
            if (Vector3.Dot(forward, transform.forward) < 0)
            {
                forward = -forward;
            }
            Vector3 up = data.Up;

            _inertia = Vector3.Lerp(_inertia, rb.velocity, Time.fixedDeltaTime * 3);

            if (Mathf.Abs(_sidewayStress) > 2)
            {
                Derail();
            }
            Quaternion axisRemapRotation = Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward, Vector3.up));
            transform.rotation = Quaternion.LookRotation(forward, up) * axisRemapRotation;
            rb.velocity = rb.velocity.magnitude * currentForward + (currentForward * Vector3.Dot(currentForward, Vector3.down));
            if (Vector3.Dot(rb.velocity, currentForward) > 0.001f)
            {
                rb.velocity = Vector3.MoveTowards(rb.velocity, Vector3.zero, _breakForce);
            }

            OnFixedUpdate();
        }

        private void ManageCurrentRail(RailData data)
        {
            Railroad next = data.NearestRail;

            if (next)
            {
                Vector3 nPos = next.GetRailDataFromPoint(transform.position, next.ActiveSplineIndex).NearestPosition;
                float dot = Vector3.Dot((nPos - data.NearestPosition).normalized, Velocity.normalized);

             

                float distance = Vector3.Distance(transform.position, nPos);
                bool canJump = dot > 0 && distance < 0.005f;
                // Debug.Log(canJump + ";" + next + "Distance: " + distance + " Dot: " + dot);


                if (distance <= 3f && next is RailroadJunction)
                {
                    if (next.GetRailDataFromPoint(transform.position, 1).CRail == _currentRail)
                    {
                        (next as RailroadJunction).SetSwitchState(true);
                    }
                    if (next.GetRailDataFromPoint(transform.position, 0).ARail == _currentRail && next.GetRailDataFromPoint(transform.position, 0).GoesIn)
                    {
                        (next as RailroadJunction).SetSwitchState(false);
                    }
                    if (next.GetRailDataFromPoint(transform.position, 0).BRail == _currentRail && !next.GetRailDataFromPoint(transform.position, 0).GoesIn)
                    {
                        (next as RailroadJunction).SetSwitchState(false);
                    }
                }

                if (canJump)
                {
                    _currentRail = next;
                }
            }
            else if (data.Time < 0.001f || data.Time > 0.990f)
            {
                Derail();
            }
        }

        private void ManageRailSegments(RailData data)
        {
            timeSegment = Mathf.RoundToInt(data.Segment);
            if (timeSegment != _lastTime)
            {
                RailJointEvent?.Invoke();
                _lastTime = timeSegment;
            }
        }

        public void Derail()
        {
            rb.useGravity = true;
            _isOnRail = false;
            _currentRail = null;
            rb.freezeRotation = false;
            rb.AddTorque(transform.forward * _sidewayStress, ForceMode.VelocityChange);

            _inertia = Vector3.zero;

            DerrailEvent?.Invoke(_inertia);
        }

        private void OnGUI()
        {/*
            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position);
            Vector2 GUIPos = GUIUtility.ScreenToGUIPoint(screen);
            Rect rect = new(GUIPos.x, Screen.height - GUIPos.y, 100, 25);
            //GUI.Label(rect, $"{timeSegment}");*/
        }

        public virtual void OnFixedUpdate()
        { }

        public void SetBrakeForce(float value)
        {
            _breakForce = value;
        }

        internal void SetCurrentRail(Railroad spawnRailroad)
        {
            _currentRail = spawnRailroad;
        }
    }
}