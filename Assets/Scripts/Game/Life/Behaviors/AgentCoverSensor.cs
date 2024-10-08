using Core.Engine;
using System.Collections.Generic;
using System.Linq;
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
        private Transform _playerCamera;

        [SerializeField] private float _detectionRadius = 20;
        [SerializeField] private int _detectionResolution = 50;

        private Vector3 _debugPos, _debugThreat;

        public CoverData FindNearestCover(Vector3 position, Vector3 threat)
        {
            _debugPos = position;
            _debugThreat = threat;

            for (int i = 0; i < _detectionResolution; i++)
            {
                float dist = Mathf.Pow(i / (_detectionResolution - 1f), 0.5f) * _detectionRadius;
                float angle = 2 * Mathf.PI * 1.618035f * i;
                float x = dist * Mathf.Cos(angle);
                float z = dist * Mathf.Sin(angle);

                Vector3 point = new Vector3(x, 0, z);

                if (NavMesh.SamplePosition(position + point, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    if (IsSafeForCover(hit.position + Vector3.up * 1.75f, threat)) return new CoverData(hit.position, false);

                    if (IsSafeForCover(hit.position + Vector3.up, threat)) return new CoverData(hit.position, true);

                    continue;
                }
            }
            return new CoverData(Vector3.zero, false);
        }

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
                   IsSafeForCover(hit.position + Vector3.up * 1.75f, threat),
                   IsSafeForCover(hit.position + Vector3.up * 1, threat),
                   threat.y - (hit.position.y + 1.75f) > 1.5f
                   );
            }
        }

        public bool IsSafeForCover(Vector3 point, Vector3 threat)
        {
            return (Physics.Linecast(threat, point, out RaycastHit hit, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers));
        }

        private void OnDrawGizmos()
        {
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

    public struct CoverData
    {
        public Vector3 Position;
        public bool NeedCrouch;

        public CoverData(Vector3 position, bool needCrouch)
        {
            Position = position;
            NeedCrouch = needCrouch;
        }
    }
}