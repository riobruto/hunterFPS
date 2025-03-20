using System;
using UnityEngine;

namespace Game.Life.WaypointPath
{
    public class WaypointGroup : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;
        
        internal Waypoint GetWaypoint()
        {
            return GetComponentInChildren<Waypoint>();
        }

        [ContextMenu("CreateWaypoints")]
        private void CreateWaypoints()
        {
            Transform[] children = gameObject.GetComponentsInChildren<Transform>();
            Waypoint lastInLoop = null;

            foreach (Transform child in children)
            {
                Waypoint waypoint = child.gameObject.AddComponent<Waypoint>();
                if (lastInLoop != null) lastInLoop.SetNextWaypoint(waypoint);
                lastInLoop = waypoint;
            }
        }
    }
}