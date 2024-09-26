using Core.Engine;
using Game.Entities;
using Game.Service;
using Nomnom.RaycastVisualization;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Life
{
    public class AgentCoverBehavior : MonoBehaviour
    {
        private Vector3 _currentPosition;
        private Vector3 _lastCoverPosition;
        private GameObject _player;

        private CoverVolumeEntity[] _coverVolumes;

        [SerializeField] private float _detectionRadius = 10;
        [SerializeField] private LayerMask _hidableLayers;
        [SerializeField] private float _minTargetDistance = 1;
        [SerializeField] private float _hideSensitivity = 0;
        [SerializeField] private float _sampleDistance = 2;

        private void Start()
        {
            _player = Bootstrap.Resolve<PlayerService>().Player;

            _coverVolumes = FindObjectsByType<CoverVolumeEntity>(FindObjectsSortMode.None);
        }

        private void Update()
        {
            _currentPosition = transform.position;
        }

        public CoverData FindCover()
        {
            return GetCoverPositionFromPlayer();
        }

        private CoverData GetCoverPositionFromPlayer()
        {
            Array.Sort(_coverVolumes, SortNearestFromAgent);

            for (int i = 0; i < _coverVolumes.Length; i++)
            {
                if (NavMesh.SamplePosition(_coverVolumes[i].transform.position, out NavMeshHit hit, _sampleDistance, NavMesh.AllAreas))
                {
                    Vector3 positionToCollider = _coverVolumes[i].transform.position - _player.transform.position;
                    if (positionToCollider.magnitude < _minTargetDistance) continue;
                    Vector3 otherSide = _coverVolumes[i].transform.position + positionToCollider;
                    Vector3 farthersPoint = _coverVolumes[i].Collider.ClosestPointOnBounds(otherSide);

                    if (Vector3.Distance(farthersPoint, _player.transform.position) < _minTargetDistance) continue;
                    return new CoverData(_coverVolumes[i], farthersPoint);
                }
            }
            return new CoverData(null, Vector3.zero); ;
        }

        private int SortNearestFromPlayer(CoverVolumeEntity A, CoverVolumeEntity B)
        {
            if (A == null && B != null) { return 1; }
            else if (A != null && B == null) { return -1; }
            else if (A == null && B == null) { return 0; }
            else return
                    Vector3.Distance(_player.transform.position, A.transform.position).CompareTo(
                        Vector3.Distance(_player.transform.position, B.transform.position));
        }

        private int SortNearestFromAgent(CoverVolumeEntity A, CoverVolumeEntity B)
        {
            if (A == null && B != null) { return 1; }
            else if (A != null && B == null) { return -1; }
            else if (A == null && B == null) { return 0; }
            else return
                    Vector3.Distance(_currentPosition, A.transform.position).CompareTo(
                        Vector3.Distance(_currentPosition, B.transform.position));
        }

        public FlankData GetFlankVectors()
        {
            FlankData data = new FlankData();

            data.StartPoint = GetCoverPositionFromPlayer().Position;

            CoverVolumeEntity player = GetNearestCoverFromPlayer();

            if (player != null)
            {
                if (NavMesh.SamplePosition(GetNearestCoverFromPlayer().transform.position, out NavMeshHit attackHit, _sampleDistance, NavMesh.AllAreas))
                    data.AttackPoint = attackHit.position;
            }

            CoverVolumeEntity blind = GetBlindCoverPoint();

            if (blind != null)
            {
                if (NavMesh.SamplePosition(blind.transform.position, out NavMeshHit blindHit, _sampleDistance, NavMesh.AllAreas))
                    data.BlindPoint = blindHit.position;
            }

            return data;
        }

        private CoverVolumeEntity GetNearestCoverFromPlayer()
        {
            CoverVolumeEntity[] covers = _coverVolumes;
            Array.Sort(covers, SortNearestFromPlayer);
            return covers[0];
        }

        private CoverVolumeEntity GetBlindCoverPoint()
        {
            CoverVolumeEntity[] covers = _coverVolumes;

            Array.Sort(covers, SortNearestFromPlayer);

            foreach (CoverVolumeEntity cover in covers)
            {
                if (VisualPhysics.Linecast(_currentPosition + (Vector3.up * 2f), cover.transform.position + (Vector3.up * 2f), out RaycastHit EnemyHit, GetIgnoreLayers()))
                {
                    if (EnemyHit.collider.gameObject.layer == 6) continue;
                }
                if (VisualPhysics.Linecast(_player.transform.position + (Vector3.up * 2f), cover.transform.position + (Vector3.up * 2f), out RaycastHit PlayerHit, GetIgnoreLayers()))
                {
                    if (PlayerHit.collider.gameObject.layer == 6) continue;
                }
                return cover;
            }
            return null;
        }

        private LayerMask GetIgnoreLayers()
        {

            //getting the ignorelayers and substracting the cover value
            LayerMask layers = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.IgnoreLayers;
            return layers &= ~(1 << 6);
        }
    }

    public struct CoverData
    {
        public CoverVolumeEntity Collider;
        public Vector3 Position;

        public CoverData(CoverVolumeEntity collider, Vector3 farthersPoint) : this()
        {
            Collider = collider;
            Position = farthersPoint;
        }
    }

    public struct FlankData
    {
        public Vector3 StartPoint;
        public Vector3 BlindPoint;
        public Vector3 AttackPoint;

        public FlankData(Vector3 start, Vector3 blind, Vector3 attack) : this()
        {
            StartPoint = start;
            BlindPoint = blind;
            AttackPoint = attack;
        }
    }
}