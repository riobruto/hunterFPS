using Core.Engine;
using System;
using System.Collections.Generic;

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Life
{
    public enum CoverSearchType
    {
        NEAREST_FROM_AGENT,
        FARTEST_FROM_AGENT,
        FARTEST_FROM_PLAYER,
        NEAREST_FROM_PLAYER,
    }

    public class AgentCoverSensor : MonoBehaviour
    {
        internal const float DEFAULHEIGHT = 1.75f;
        private Vector3 _debugPos, _debugThreat;

        [SerializeField] private float _detectionRadius = 20;
        [SerializeField] private int _detectionResolution = 50;

        private float _height = 1.75f;
        private float _crouchHeight = .75f;
        private NavMeshAgent _agent;

        public float DetectionRadius { get => _detectionRadius; set => _detectionRadius = value; }
        public int DetectionResolution { get => _detectionResolution; set => _detectionResolution = value; }
        public float Height { get => _height; set => _height = value; }
        public float CrouchHeight { get => _crouchHeight; set => _crouchHeight = value; }

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        //now this points of data make a beautiful line

        public IEnumerable<SpatialDataPoint> GetCombatSpatialData(Vector3 position, Vector3 threat, float radius = 20, float safeDistance = 15)
        {
            _debugPos = position;
            _debugThreat = threat;

            for (int i = 1; i < _detectionResolution; i++)
            {
                float dist = Mathf.Pow(i / (_detectionResolution - 1f), 0.5f) * radius;
                float angle = 2 * Mathf.PI * 1.618035f * i;
                float x = dist * Mathf.Cos(angle);
                float z = dist * Mathf.Sin(angle);

                Vector3 point = new Vector3(x, 0, z);
                bool validPoint = NavMesh.SamplePosition(position + point, out NavMeshHit hit, 5, NavMesh.AllAreas);
                if (!validPoint) continue;

                NavMeshPath path = new NavMeshPath();
                bool validPath = _agent.CalculatePath(position + point, path);
                if (!validPath || path.status == NavMeshPathStatus.PathPartial) continue;
                if (Vector3.Distance(hit.position, threat) < safeDistance) continue;

                yield return new(
                   hit.position,
                   Vector3.Distance(hit.position, threat),
                   Vector3.Distance(hit.position, position),
                   IsSafeForCover(hit.position + Vector3.up * _height, threat),
                   IsSafeForCover(hit.position + Vector3.up * _crouchHeight, threat),
                   threat.y - (hit.position.y + _height) > 1.5f,
                   path);
            }

            yield break;
        }

        public bool IsSafeForCover(Vector3 point, Vector3 threat)
        {
            return Physics.Linecast(threat, point, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers);
        }

        private void OnDrawGizmos()
        {
            return;
            Gizmos.DrawWireSphere(_debugPos, _detectionRadius);

            foreach (SpatialDataPoint point in GetCombatSpatialData(_debugPos, _debugThreat).ToArray())
            {
                if (point.SafeFromStanding) Gizmos.color = Color.green;
                if (point.SafeFromCrouch && !point.SafeFromStanding) Gizmos.color = Color.yellow;
                if (!point.SafeFromCrouch && !point.SafeFromStanding) Gizmos.color = Color.red;

                if (point.HasHeightAdvantage)
                {
                    Gizmos.DrawWireCube(point.Position + Vector3.up, Vector3.one / 2f);
                }
                Gizmos.DrawWireSphere(point.Position, .25f);
                Handles.Label(point.Position + Vector3.up, $"Dist: {point.PathLength}");
            }
        }

        internal object GetCombatSpatialData(Vector3 combatPoint, Vector3 playerHeadPosition, object detectionRange, float v)
        {
            throw new NotImplementedException();
        }
    }

    public struct SpatialDataPoint
    {
        public Vector3 Position { get; private set; }
        public float DistanceFromThreat { get; private set; }
        public float DistanceFromCenter { get; private set; }
        public bool SafeFromStanding { get; private set; }
        public bool SafeFromCrouch { get; private set; }
        public bool HasHeightAdvantage { get; private set; }
        public NavMeshPath Path { get; private set; }
        public float PathLength { get; private set; }

        public SpatialDataPoint(Vector3 position, float distanceFromThreat, float distanceFromPoint, bool safeFromStanding, bool safeFromCrouch, bool heightAdvantage, NavMeshPath path)
        {
            Position = position;
            DistanceFromThreat = distanceFromThreat;
            DistanceFromCenter = distanceFromPoint;
            SafeFromStanding = safeFromStanding;
            SafeFromCrouch = safeFromCrouch;
            HasHeightAdvantage = heightAdvantage;
            Path = path;
            PathLength = 0;
            PathLength = CalcultePathDistance(path);
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
}