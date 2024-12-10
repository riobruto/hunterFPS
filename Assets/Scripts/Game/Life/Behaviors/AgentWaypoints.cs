using Game.Life.WaypointPath;
using System;
using System.Collections;
using UnityEngine;

namespace Game.Life
{
    public class AgentWaypoints : MonoBehaviour
    {
        [SerializeField] private Waypoint _currentWaypoint;

        // [SerializeField] private WaypointGroup _group;
        public Waypoint CurrentWaypoint { get => _currentWaypoint; }

        public Waypoint GetNearestWaypoint()
        {
            Waypoint[] waypoint = FindObjectsOfType<Waypoint>();
            Array.Sort(waypoint, WaypointArraySortComparer);
            return waypoint[0];
        }

        private int WaypointArraySortComparer(Waypoint A, Waypoint B)
        {
            if (A == null && B != null) { return 1; }
            else if (A != null && B == null) { return -1; }
            else if (A == null && B == null) { return 0; }
            else return
                    Vector3.Distance(transform.position, A.transform.position).CompareTo(
                        Vector3.Distance(transform.position, A.transform.position));
        }

        internal void SetNextWaypoint()
        {
            _currentWaypoint = _currentWaypoint.NextWaypoint;
        }
    }
}