using Game.Life;
using Game.Service;
using Life.Controllers;
using System.Xml.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    internal class TEST_EQS_PERFORMANCE : MonoBehaviour
    {
        [SerializeField] private int _grid_x = 20;
        [SerializeField] private int _grid_z = 20;

        [SerializeField] private Transform _me;
        [SerializeField] private Transform _threat;
        [SerializeField] private LayerMask _mask;
        [SerializeField] private Transform _UBICATOR;

        private NativeArray<SpatialData> CreateGrid()
        {
            NavMeshPath path = new NavMeshPath();
            NativeArray<SpatialData> spatialData = new NativeArray<SpatialData>(_grid_x * _grid_z, Allocator.Temp);

            int index = 0;
            for (int x = 1; x < _grid_x; x++)
            {
                for (int z = 1; z < _grid_z; z++)
                {
                    Vector3 position = _me.transform.position + new Vector3(x - _grid_x / 2f, 0, z - _grid_z / 2f);
                    //is on navmesh

                    //too near
                    if (Vector3.Distance(position, _me.position) < 2) continue;
                    if (Vector3.Distance(position, _threat.position) < 2) continue;

                    bool validPoint = NavMesh.SamplePosition(position, out NavMeshHit hit, 20, NavMesh.AllAreas);
                    if (!validPoint) continue;
                    //is reachable
                    bool validPath = NavMesh.CalculatePath(_me.transform.position, hit.position, NavMesh.AllAreas, path);
                    if (!validPath || path.status == NavMeshPathStatus.PathPartial) continue;

                    SpatialData point = new SpatialData();
                    point.Position = hit.position;

                    //unsafe standing up
                    if (!Physics.Linecast(hit.position + Vector3.up * 1.8f, _threat.transform.position, _mask, QueryTriggerInteraction.Ignore))
                    {
                        point.Weight += 10f;
                    }
                    //unsafe crouching
                    else if (!Physics.Linecast(hit.position + Vector3.up * 1f, _threat.transform.position, _mask, QueryTriggerInteraction.Ignore))
                    {
                        point.Weight += 5f;
                    }

                    point.Weight += CalcultePathDistance(path);
                    // point.Cost += 1 / Vector3.Distance(_threat.transform.position, point.Position);

                    spatialData[index] = point;
                    index++;
                }
            }

            return spatialData;
        }

        private bool IsInAgentDestination(AgentController controller, Vector3 hit)
        {
            foreach (AgentController c in AgentGlobalService.Instance.ActiveAgents)
            {
                if (Vector3.Distance(hit, c.Destination) < 1.5f) return true;
            }
            return false;
        }

        private float CalcultePathDistance(NavMeshPath path)
        {
            if (path == null) return 0;

            float lng = 0.0f;

            if ((path.status != NavMeshPathStatus.PathInvalid) && (path.corners.Length > 1))
            {
                for (int i = 1; i < path.corners.Length; ++i)
                {
                    lng += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }
            }

            return lng;
        }

        [SerializeField] private bool _debug;

        private void Update()
        {
        }

        private void OnDrawGizmos()
        {
            if (!_debug) return;

            Gizmos.DrawCube(_threat.position, Vector3.one * .5f);
            NativeArray<SpatialData> data = AgentSpatialUtility.CreateCoverArray(new Vector2Int(_grid_x, _grid_z), _me.position, _threat.position, _mask);
            SpatialData? best = AgentSpatialUtility.GetBestPoint(data);

            foreach (SpatialData spatialData in data)
            {
                if (!spatialData.Valid) continue;
                //Handles.Label(spatialData.Position, spatialData.Weight.ToString());
                Gizmos.color = Color.Lerp(Color.green, Color.red, spatialData.Weight / 1);
                Gizmos.DrawWireSphere(spatialData.Position, .125f);
            }

            if (best != null) Gizmos.DrawSphere(best.Value.Position, .25f);
            
        }
    }
}