using Core.Engine;
using Game.Service;
using Life.Controllers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;
using Game.Life.Entities;
using System;

namespace Game.Life
{
    public class CoverSpotQuery
    {
        /// <summary>
        /// Sorted By Travel Distance
        /// </summary>
        public CoverSpotEntity[] CoverSpots;

        private CoverSpotEntity[] _entities;
        private AgentController _controller;

        public CoverSpotQuery(AgentController controller)
        {
            _controller = controller;
            _entities = AgentGlobalService.Instance.CoverEntities.ToArray();
            CoverSpots = _entities;
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

    public struct SpatialQueryData
    {
        public AgentController AgentController;
        public Vector3 Position;
        public Vector3 Threat;
        public float MinDistance;

        public SpatialQueryData(AgentController soldier, Vector3 position, Vector3 threat, float minDistance) : this()
        {
            AgentController = soldier;
            Position = position;
            Threat = threat;
            MinDistance = minDistance;
        }
    }

    public class SpatialDataQuery
    {
        public List<SpatialDataPoint> AllPoints;

        //Points que tienen visibilidad clara hacia el Enemigo
        public List<SpatialDataPoint> UnsafePoints;

        //Points que tienen visibilidad ocluida hacia el Enemigo
        public List<SpatialDataPoint> SafePoints;

        //Points que tienen visibilidad ocluida hacia el Enemigo a media altura, pero tienen visibilidad a altura completa
        public List<SpatialDataPoint> SafeCrouchPoints;

        //Points que tienen visibilidad ocluida hacia el Enemigo, y esta cerca de su fuente de oclusion(paredes o obstaculos)
        public List<SpatialDataPoint> WallCoverPoints;

        //Points que tienen visibilidad ocluida hacia el Enemigo a media altura, y esta cerca de su fuente de oclusion(paredes o obstaculos)
        public List<SpatialDataPoint> WallCoverCrouchedPoints;

        public const int X_RESOLUTION = 30, Z_RESOLUTION = 30;
        public bool AvoidNearAgents { get; private set; }

        public SpatialDataQuery(SpatialQueryData data)
        {
            Debug.Log("NEW QUERY");
            SpatialDataPoint[] points = GetCombatSpatialData(data.AgentController, data.Position, data.Threat, data.MinDistance).ToArray();
            //probar try catch

            //fix: ESTO ES NEFASTO CREA MAS ALLOC Q LA PUTA DE TU MAMA CUANDO SE ALOCA CON MI CHALAMPI : 7/1/25
            AllPoints = points.OrderBy(x => x.TravelLenght).ToList();

            UnsafePoints = points.Where(x => x.HasVisualStanding && x.HasVisualCrouching && Vector3.Distance(x.Position, x.OcclusionPoint) > 2).OrderBy(x => x.TravelLenght).ToList();

            SafePoints = points.Where(x => !x.HasVisualStanding && !x.HasVisualCrouching && Vector3.Distance(x.Position, x.OcclusionPoint) > 2).OrderBy(x => x.TravelLenght).ToList();
            SafeCrouchPoints = points.Where(x => x.HasVisualStanding && !x.HasVisualCrouching && Vector3.Distance(x.Position, x.OcclusionPoint) > 2).OrderBy(x => x.TravelLenght).ToList();

            WallCoverPoints = points.Where(x => !x.HasVisualStanding && !x.HasVisualCrouching && Vector3.Distance(x.Position, x.OcclusionPoint) < 2).OrderBy(x => x.TravelLenght).ToList();
            WallCoverCrouchedPoints = points.Where(x => x.HasVisualStanding && !x.HasVisualCrouching && Vector3.Distance(x.Position, x.OcclusionPoint) < 2).OrderBy(x => x.TravelLenght).ToList();
        }

        public IEnumerable<SpatialDataPoint> GetCombatSpatialData(AgentController controller, Vector3 position, Vector3 threat, float safeDistance = 15)
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
                    Vector3 point = new Vector3(x - X_RESOLUTION / 2, 0, z - Z_RESOLUTION / 2);
                    bool validPoint = NavMesh.SamplePosition(position + point, out NavMeshHit hit, 20, NavMesh.AllAreas);
                    if (!validPoint) continue;
                    //checks if gets in the way of an agent.
                    if (IsInAgentDestination(controller, hit.position)) continue;

                    bool validPath = controller.NavMeshAgent.CalculatePath(position + point, path);
                    if (!validPath || path.status == NavMeshPathStatus.PathPartial) continue;
                    if (Vector3.Distance(hit.position, threat) < safeDistance) continue;

                    yield return new(
                       hit.position,
                       CalculateOcclusionPoint(hit.position + Vector3.up * controller.Height, threat),
                       Vector3.Distance(hit.position, threat),
                       Vector3.Distance(hit.position, position),
                       HasVisual(hit.position + Vector3.up * controller.Height, threat),
                       HasVisual(hit.position + Vector3.up * controller.CrouchHeight, threat),
                       threat.y - (hit.position.y + controller.Height) > 1.5f,
                       path); ;
                }
            }

            yield break;
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
    }
}