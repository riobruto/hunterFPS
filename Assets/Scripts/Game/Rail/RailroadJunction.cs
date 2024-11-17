using UnityEditor;
using UnityEngine;

namespace Rail
{
    public delegate void JunctionChangeSwitchDelegate(bool state);

    public class RailroadJunction : Railroad
    {
        [SerializeField] private Railroad _juncionOutputRailroad;

        [Tooltip("Does this rail merges into one or splits in two???????????????")]
        [SerializeField] private bool _goesIn;

        [SerializeField] private bool _switchState;

        public event JunctionChangeSwitchDelegate JunctionChangeSwitch;

        private int _currentSplineIndex => _switchState ? 1 : 0;
        public override int ActiveSplineIndex => _currentSplineIndex;

        public void SetSwitchState(bool state)
        {
            _switchState = state;
            JunctionChangeSwitch?.Invoke(state);
            Debug.Log($"SWITCH STATE CHANGED TO {state}");
        }

        private void Start()
        {
            JunctionChangeSwitch?.Invoke(_switchState);
        }

        public Railroad GetCRail() => _juncionOutputRailroad;

        public bool GetSwitchState() => _switchState;

        internal override void DrawGizmos()
        {
            RailData CArrow = GetRailDataFromTime(_goesIn ? 0 : 1, 1);

            if (!_goesIn)
            {
                CArrow.Tangent *= -1;
            }

            CArrow.NearestPosition = CArrow.NearestPosition + CArrow.Tangent.normalized;
            bool Connected;
            Connected = _juncionOutputRailroad != null;
            Handles.color = Connected ? Color.green : Color.red;
            DrawArrowFromData(CArrow);
        }

        public override RailData GetRailDataFromPoint(Vector3 point, int splineIndex = 0)
        {
            RailData data = base.GetRailDataFromPoint(point, splineIndex);
            data.GoesIn = _goesIn;
            if (splineIndex == 1)
            {
                data.CRail = _juncionOutputRailroad;
            }
            return data;
        }

        public void SetCRail(Railroad c)
        {
            _juncionOutputRailroad = c;
        }
    }
}