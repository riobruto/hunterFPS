using Core.Engine;
using Game.Life.Entities;
using Game.Service;
using Life.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Life
{
   

    public struct SpatialDataPoint
    {
        public Vector3 Position { get; private set; }
        public Vector3 OcclusionPoint { get; private set; }
        public float DistanceFromThreat { get; private set; }
        public float DistanceFromCenter { get; private set; }
        public bool HasVisualStanding { get; private set; }
        public bool HasVisualCrouching { get; private set; }
        public bool HasHeightAdvantage { get; private set; }
        public NavMeshPath Path { get; private set; }
        public float TravelLenght { get; private set; }
        public bool IsNearWall { get; internal set; }

        public SpatialDataPoint(Vector3 position, Vector3 occlusion, float distanceFromThreat, float distanceFromPoint, bool safeFromStanding, bool safeFromCrouch, bool heightAdvantage, NavMeshPath path)
        {
            Position = position;
            OcclusionPoint = occlusion;
            DistanceFromThreat = distanceFromThreat;
            DistanceFromCenter = distanceFromPoint;
            HasVisualStanding = safeFromStanding;
            HasVisualCrouching = safeFromCrouch;
            HasHeightAdvantage = heightAdvantage;
            Path = path;
            IsNearWall = Vector3.Distance(position, occlusion) < 2;
            TravelLenght = 0;
            TravelLenght = CalcultePathDistance(path);
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
    }
    public enum SearchType
    {
        SAFE,
        UNSAFE,
        ANY
    }
    public struct SpatialQueryPrefs
    {
        public AgentController AgentController;
        public Vector3 Position;
        public Vector3 Threat;
        public float MinDistance;
        public SearchType QuerySearchType;
        public SpatialQueryPrefs(AgentController soldier, Vector3 position, Vector3 threat, float minDistance, SearchType querySearchType = SearchType.ANY) : this()
        { 
            AgentController = soldier;
            Position = position;
            Threat = threat;
            MinDistance = minDistance;
            QuerySearchType = querySearchType;
        }
    }

    public class SpatialDataQuery
    {
        public List<SpatialDataPoint> AllPoints;
        //Points que tienen visibilidad clara hacia el Enemigo
        public List<SpatialDataPoint> UnsafePoints;
        //Points que tienen visibilidad ocluida hacia el Enemigo
        public List<SpatialDataPoint> SafePoints;
        //Points que tienen visibilidad ocluida hacia el ENemigo a media altura, pero tienen visibilidad a altura completa
        public List<SpatialDataPoint> SafeCrouchPoints;
        //Points que tienen visibilidad ocluida hacia el Enemigo, y esta cerca de su fuente de oclusion(paredes o obstaculos)
        public List<SpatialDataPoint> WallCoverPoints;
        //Points que tienen visibilidad ocluida hacia el Enemigo a media altura, y esta cerca de su fuente de oclusion(paredes o obstaculos)
        public List<SpatialDataPoint> WallCoverCrouchedPoints;

        public const int X_RESOLUTION = 10, Z_RESOLUTION = 10;

        public SpatialDataQuery(SpatialQueryPrefs data)
        {
            Stopwatch w = Stopwatch.StartNew();
            //TODO: RESOLVER ALLOC
            //POSIBLEMENTE EL CALC PATH SEA LENTO.

            AllPoints = new List<SpatialDataPoint>();
            UnsafePoints = new List<SpatialDataPoint>();
            SafePoints = new List<SpatialDataPoint>();
            SafeCrouchPoints = new List<SpatialDataPoint>();
            WallCoverPoints = new List<SpatialDataPoint>();
            WallCoverCrouchedPoints = new List<SpatialDataPoint>();

            {
                PopulateListWithSpatialData(data.AgentController, data.Position, data.Threat, data.QuerySearchType, data.MinDistance);
            }

            UnsafePoints.Sort(SortByTravelDistance);
            SafePoints.Sort(SortByTravelDistance);
            SafeCrouchPoints.Sort(SortByTravelDistance);
            WallCoverPoints.Sort(SortByTravelDistance);
            WallCoverCrouchedPoints.Sort(SortByTravelDistance);
            UnityEngine.Debug.Log($"NEW QUERY: {w.Elapsed.TotalMilliseconds}");
            w.Stop();
        }



        // podriamos calcular la traveldistance aca, y crear un sistema de puntos para obtener el lugar mas conveniente
        //aparte, podria acotar mas los criterios de busqueda para achicar el stuttering

        public void PopulateListWithSpatialData(AgentController controller, Vector3 position, Vector3 threat, SearchType searchType = SearchType.ANY, float safeDistance = 15)
        {
            NavMeshPath path = new NavMeshPath();
            
            if (!controller.NavMeshAgent.isOnNavMesh)
            {
                throw new UnityException("Agent is not placed in a NavMesh");
            }

            for (int x = 1; x < X_RESOLUTION; x++)
            {
                for (int z = 1; z < Z_RESOLUTION; z++)
                {                

                    Vector3 point = new Vector3(x - X_RESOLUTION / 2, 0, z - Z_RESOLUTION / 2) * 2f;
                    bool validPoint = NavMesh.SamplePosition(position + point, out NavMeshHit hit, 20, NavMesh.AllAreas);
                    if (!validPoint) continue;
                    bool validPath = NavMesh.CalculatePath(controller.transform.position, position + point, NavMesh.AllAreas, path);
                    if (!validPath || path.status == NavMeshPathStatus.PathPartial) continue;
                    if (IsInAgentDestination(controller, hit.position)) continue;
                    if (Vector3.Distance(hit.position, threat) < safeDistance) continue;

                    //generamos el punto
                    SpatialDataPoint datapoint = new(
                       hit.position,
                       CalculateOcclusionPoint(hit.position + Vector3.up * controller.Height, threat),
                       Vector3.Distance(hit.position, threat),
                       Vector3.Distance(hit.position, position),
                       HasVisual(hit.position + Vector3.up * controller.Height, threat),
                       HasVisual(hit.position + Vector3.up * controller.CrouchHeight, threat),
                       threat.y - (hit.position.y + controller.Height) > 1.5f,
                       path);

                    AllPoints.Add(datapoint);

                    if (Vector3.Distance(datapoint.Position, datapoint.OcclusionPoint) < 1)
                    {
                        if (!datapoint.HasVisualStanding) { SafePoints.Add(datapoint); WallCoverPoints.Add(datapoint); continue; }
                        if (!datapoint.HasVisualCrouching) { SafeCrouchPoints.Add(datapoint); WallCoverCrouchedPoints.Add(datapoint); continue; }
                    }
                    else
                    {
                        if (!datapoint.HasVisualStanding) { SafePoints.Add(datapoint); continue; }
                        if (!datapoint.HasVisualCrouching) { SafeCrouchPoints.Add(datapoint); continue; }
                    }
                    UnsafePoints.Add(datapoint);
                    continue;
                }
            }
        }

        private bool IsInAgentDestination(AgentController controller, Vector3 hit)
        {
            foreach (AgentController c in AgentGlobalService.Instance.ActiveAgents)
            {
                if (Vector3.Distance(hit, c.Destination) < 1.5f) return true;
            }
            return false;
        }

        public bool HasVisual(Vector3 point, Vector3 threat)
        {
            return !Physics.Linecast(threat, point, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers);
        }

        public Vector3 CalculateOcclusionPoint(Vector3 from, Vector3 to)
        {
            Physics.Linecast(from, to, out RaycastHit hit, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers);
            return hit.point;
        }

        public int SortByTravelDistance(SpatialDataPoint A, SpatialDataPoint B) => A.TravelLenght.CompareTo(B.TravelLenght);
    }
}