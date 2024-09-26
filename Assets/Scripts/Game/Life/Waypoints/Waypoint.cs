using UnityEngine;

namespace Game.Life.WaypointPath
{
    public class Waypoint : MonoBehaviour
    {
        [SerializeField] private Waypoint _nextWaypoint;
        public Waypoint NextWaypoint => _nextWaypoint;

        public void SetNextWaypoint(Waypoint nextWaypoint)
        {
            _nextWaypoint = nextWaypoint;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _nextWaypoint != null ? Color.green : Color.red;

            Gizmos.DrawSphere(transform.position, 0.33f);
            if (_nextWaypoint != null)
            {
                Gizmos.DrawLine(transform.position, _nextWaypoint.transform.position);
            }
        }
    }
}