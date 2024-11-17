using Core.Engine;
using Nomnom.RaycastVisualization;
using System.Collections.Generic;

using System.Linq;
using System.Threading;
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

        public float DetectionRadius { get => _detectionRadius; set => _detectionRadius = value; }
        public int DetectionResolution { get => _detectionResolution; set => _detectionResolution = value; }
        public float Height { get => _height; set => _height = value; }
        public float CrouchHeight { get => _crouchHeight; set => _crouchHeight = value; }

        //now this points of data make a beautiful line
        public IEnumerable<SpatialDataPoint> GetCombatSpatialData(Vector3 position, Vector3 threat)
        {
            _debugPos = position;
            _debugThreat = threat;

            for (int i = 1; i < _detectionResolution; i++)
            {
                float dist = Mathf.Pow(i / (_detectionResolution - 1f), 0.5f) * _detectionRadius;
                float angle = 2 * Mathf.PI * 1.618035f * i;
                float x = dist * Mathf.Cos(angle);
                float z = dist * Mathf.Sin(angle);

                Vector3 point = new Vector3(x, 0, z);
                bool validPoint = NavMesh.SamplePosition(position + point, out NavMeshHit hit, 5, NavMesh.AllAreas);

                if (!validPoint) continue;

                yield return new(
                   hit.position,
                   Vector3.Distance(hit.position, threat),
                   Vector3.Distance(hit.position, position),
                   IsSafeForCover(hit.position + Vector3.up * _height, threat),
                   IsSafeForCover(hit.position + Vector3.up * _crouchHeight, threat),
                   threat.y - (hit.position.y + _height) > 1.5f);
            }
            yield break;
        }

        public bool IsSafeForCover(Vector3 point, Vector3 threat)
        {
            return Physics.Linecast(threat, point, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers);
        }

        private void OnDrawGizmos()
        {
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
            }
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

        public SpatialDataPoint(Vector3 position, float distanceFromThreat, float distanceFromPoint, bool safeFromStanding, bool safeFromCrouch, bool heightAdvantage)
        {
            Position = position;
            DistanceFromThreat = distanceFromThreat;
            DistanceFromCenter = distanceFromPoint;
            SafeFromStanding = safeFromStanding;
            SafeFromCrouch = safeFromCrouch;
            HasHeightAdvantage = heightAdvantage;
        }
    }

    public static class SpatialDataUtils
    {
        public static SpatialDataPoint ReevaluatePoint(this SpatialDataPoint point, Vector3 threat, float height = 1.75f, float crouchHeight = .75f)
        {
            NavMesh.SamplePosition(point.Position, out NavMeshHit hit, 5, NavMesh.AllAreas);
            return
           new(hit.position,
           Vector3.Distance(hit.position, threat),
           Vector3.Distance(hit.position, point.Position),
           IsSafeForCover(hit.position + Vector3.up * height, threat),
           IsSafeForCover(hit.position + Vector3.up * crouchHeight, threat),
           threat.y - (hit.position.y + height) > 1.5f);
        }

        private static bool IsSafeForCover(Vector3 point, Vector3 threat)
        {
            return Physics.Linecast(threat, point, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers);
        }
    }
}