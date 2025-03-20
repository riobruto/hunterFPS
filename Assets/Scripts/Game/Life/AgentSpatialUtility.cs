using Game.Life.Entities;
using Game.Service;
using Life.Controllers;
using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Life
{
    public struct SpatialData : IComparable<float>
    {
        public Vector3 Position;
        public float Weight;
        public bool Valid => Weight != 0 && Position != Vector3.zero;

        public int CompareTo(float other) => Weight.CompareTo(other);
    }

    public class CoverSpotQuery
    {
        /// <summary>
        /// Sorted By Travel Distance
        /// </summary>
        public CoverSpotEntity[] CoverSpots;
       
        private AgentController _controller;

        public CoverSpotQuery(AgentController controller)
        {
            _controller = controller;
            CoverSpots = AgentGlobalService.Instance.CoverEntities.Where(x => Vector3.Distance(x.transform.position, _controller.transform.position) < 20).ToArray();
            Array.Sort(CoverSpots, CompareByTravelDistance);
        }

    

        private int CompareByTravelDistance(CoverSpotEntity A, CoverSpotEntity B)
        {
                    
            NavMeshPath pathB = new NavMeshPath(), pathA = new NavMeshPath();
            _controller.NavMeshAgent.CalculatePath(A.transform.position, pathA);
            _controller.NavMeshAgent.CalculatePath(B.transform.position, pathB);
            return CalcultePathDistance(pathA).CompareTo(CalcultePathDistance(pathB));
        }

        private float CalcultePathDistance(NavMeshPath path)
        {
            float lng = 0.0f;
            Vector3[] corners = new Vector3[0];
            int Lenght = path.GetCornersNonAlloc(corners);

            if ((path.status != NavMeshPathStatus.PathInvalid) && (Lenght > 1))
            {
                for (int i = 1; i < Lenght; ++i)
                {
                    lng += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }
            }

            return lng;
        }
    }

    public static class AgentSpatialUtility
    {
        //Utilites
        //FIND BEST ATTACK POINT
        //UN PUNTO QUE => COSTO DE EXPOSICION SEA BAJO, PERO QUE TENGA L.O.S
        //FIND SAFEST COVER POINT WITHIN SQUAD
        //ESTO SIGNIFICA => ENCONTRAR UN PUNTO QUE NO ESTE LEJOS DE LA SQUAD, PERO QUE NO TENGA L.O.S CON EL ENEMIGO.
        // tambien considerar un DOT minimo para los agentes cercanos, evitar el LOS de compañeros de squad
        //FIND SAFEST COVER FROM EXPLOSIVE
        //ESTO SIGNIFICA => ENCONTRAR UN PUNTO QUE NO TENGA L.O.S CON EL EXPLOSIVO.
        //HAY QUE HACER COSTOS... *MORENO INTENSIFIES*
        //LOS COSTOS SE DETERMINAN EN BASE A: EXPOSICION, LINEA DE FUEGO
        //PODEMOS DECIR QUE LA MEJOR LINEA DE FUEGO ES AQUELLA QUE NO ESTA OBSTRUIDA, Y ESTA CERCA DE LA POSICION ACTUAL, Y TIENE LA MENOR EXPOSICION POSIBLE
        //LA MEJOR COVERTURA ES AQUELLA QUE NO TIENE LINE OF SIGHT, Y QUE ADEMAS

        //crear un array de puntos con puntos validos.
        public static NativeArray<SpatialData> CreateAttackArray(Vector2Int size, Vector3 center, Vector3 threat, LayerMask mask)
        {
            int _grid_x = size.x;
            int _grid_z = size.y;
            NavMeshPath path = new NavMeshPath();
            NativeArray<SpatialData> spatialData = new NativeArray<SpatialData>(_grid_x * _grid_z, Allocator.Temp);

            int index = 0;
            for (int x = 1; x < _grid_x; x++)
            {
                for (int z = 1; z < _grid_z; z++)
                {
                    Vector3 position = center + new Vector3(x - _grid_x / 2f, 0, z - _grid_z / 2f);
                    //is on navmesh
                    //too near
                    if (Vector3.Distance(position, center) < 2) continue;
                    if (Vector3.Distance(position, threat) < 2) continue;
                    if (IsInAgentDestination(position)) continue;

                    bool validPoint = NavMesh.SamplePosition(position, out NavMeshHit hit, 4, NavMesh.AllAreas);
                    if (!validPoint) continue;
                    //is reachable
                    bool validPath = NavMesh.CalculatePath(center, hit.position, NavMesh.AllAreas, path);
                    if (!validPath || path.status == NavMeshPathStatus.PathPartial) continue;
                    if (Physics.Linecast(hit.position + Vector3.up * 1.8f, threat, mask, QueryTriggerInteraction.Ignore)) continue;

                    SpatialData point = new SpatialData();
                    point.Position = hit.position;
                    //unsafe standing up

                    //unsafe crouching

                    point.Weight -= CalcultePathDistance(path) / (size.magnitude / 2f);
                    point.Weight -= Vector3.Distance(threat, point.Position) / (size.magnitude / 2f);

                    if (Physics.Linecast(hit.position + Vector3.up * 1f, threat, mask, QueryTriggerInteraction.Ignore))
                    {
                        point.Weight += 10f;
                    }

                    if (point.Position.y > threat.y) point.Weight += 1;
                    spatialData[index] = point;
                    index++;
                }
            }
            return spatialData;
        }

        public static NativeArray<SpatialData> CreateCoverArray(Vector2Int size, Vector3 center, Vector3 threat, LayerMask mask)
        {
            int _grid_x = size.x;
            int _grid_z = size.y;
            NavMeshPath path = new NavMeshPath();
            NativeArray<SpatialData> spatialData = new NativeArray<SpatialData>(_grid_x * _grid_z, Allocator.Temp);

            int index = 0;
            for (int x = 1; x < _grid_x; x++)
            {
                for (int z = 1; z < _grid_z; z++)
                {
                    Vector3 position = center + new Vector3(x - _grid_x / 2f, 0, z - _grid_z / 2f);
                    //is on navmesh
                    //too near
                    if (Vector3.Distance(position, center) < 2) continue;
                    if (Vector3.Distance(position, threat) < 2) continue;
                    if (IsInAgentDestination(position)) continue;

                    bool validPoint = NavMesh.SamplePosition(position, out NavMeshHit hit, 2, NavMesh.AllAreas);
                    if (!validPoint) continue;
                    //is reachable
                    bool validPath = NavMesh.CalculatePath(center, hit.position, NavMesh.AllAreas, path);
                    if (!validPath || path.status == NavMeshPathStatus.PathPartial) continue;

                    SpatialData point = new SpatialData();
                    point.Position = hit.position;
                    //unsafe standing up
                    //unsafe crouching
                    point.Weight -= CalcultePathDistance(path) / size.magnitude;
                    point.Weight -= Vector3.Distance(threat, point.Position) / size.magnitude;

                    if (Physics.Linecast(hit.position + Vector3.up * 1f, threat, mask, QueryTriggerInteraction.Ignore))
                    {
                        point.Weight += 5f;
                    }
                    if (Physics.Linecast(hit.position + Vector3.up * 1.8f, threat, mask, QueryTriggerInteraction.Ignore))
                    {
                        point.Weight += 10f;
                    }

                    spatialData[index] = point;
                    index++;
                }
            }
            return spatialData;
        }

        public static SpatialData? GetBestPoint(NativeArray<SpatialData> spatialData)
        {
            SpatialData? dataPoint = null;

            for (int i = 0; i < spatialData.Length; i++)
            {
                if (!spatialData[i].Valid) continue;
                if (dataPoint.HasValue)
                {
                    if (spatialData[i].Weight > dataPoint.Value.Weight) { dataPoint = spatialData[i]; }
                }
                else dataPoint = spatialData[i];
            }

            return dataPoint;
        }

        private static bool IsInAgentDestination(Vector3 hit)
        {
            //test:
            if (Application.isEditor) return false;

            foreach (AgentController c in AgentGlobalService.Instance.ActiveAgents)
            {
                if (Vector3.Distance(hit, c.Destination) < 2f) return true;
                if (Vector3.Distance(hit, c.transform.position) < 2f) return true;
            }
            return false;
        }

        private static float CalcultePathDistance(NavMeshPath path)
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
    }

    public class ActBusyQuery
    {
        /// <summary>
        /// Sorted By Travel Distance
        /// </summary>
        public ActBusySpotEntity[] ActBusySpots;

        private ActBusySpotEntity[] _entities;
        private AgentController _controller;

        public ActBusyQuery(AgentController controller)
        {
            _controller = controller;
            _entities = AgentGlobalService.Instance.ActBusyEntities.ToArray();
            ActBusySpots = _entities;
            Array.Sort(ActBusySpots, CompareByTaken);
        }

        private int CompareByTaken(ActBusySpotEntity A, ActBusySpotEntity B)
        {
            int i = 0;

            i += Vector3.Distance(A.transform.position, _controller.transform.position).CompareTo(Vector3.Distance(B.transform.position, _controller.transform.position));
            i += A.Taken.CompareTo(B.Taken); 

            return i;
        }
    }
}