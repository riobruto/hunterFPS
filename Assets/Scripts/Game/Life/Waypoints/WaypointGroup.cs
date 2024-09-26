using UnityEngine;

namespace Game.Life.WaypointPath
{
    public class WaypointGroup : MonoBehaviour
    {
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

        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
        }
    }
}