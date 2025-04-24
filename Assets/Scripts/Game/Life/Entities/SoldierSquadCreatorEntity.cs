using Core.Engine;
using Game.Service;
using Life.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Life.Entities
{
    [Serializable]
    public class AgentSpawnPoint
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _spawnRadius;
        public Vector3 Position => _spawnPoint.position;
        public float Radius => _spawnRadius;

        public float GetDistance()
        {
            if (!PlayerService.Active) return 0;
            return Vector3.Distance(_spawnPoint.position, Bootstrap.Resolve<PlayerService>().PlayerCamera.transform.position);
        }

        public bool IsVisible()
        {
            if (!PlayerService.Active) return false;
            return !Physics.Raycast(_spawnPoint.position + Vector3.up * 2f, Bootstrap.Resolve<PlayerService>().PlayerCamera.transform.position, default);
        }
    }

    public class SoldierSquadCreatorEntity : MonoBehaviour
    {
        [SerializeField] private GameObject[] _soldierPrefabs;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _spawnRadius = 5f;
        [SerializeField] private SquadBeginState _beginState;
        [SerializeField] private bool _createOnPlayerSpawn;

        [Header("Squad Goal")]
        [SerializeField] private bool _holdPosition;

        [SerializeField] private Transform _holdTransform;
        [SerializeField] private bool _canLoseContact;
        [SerializeField] private bool _keepSpawningOnSquadWiped;
        private SoldierSquad _lastSquad;
        [SerializeField] private AgentSpawnPoint[] _spawnPoints;

        private void Start()
        {
            if (_createOnPlayerSpawn)
            {
                PlayerService.PlayerSpawnEvent += OnPlayerSpawn;
                PlayerService.PlayerRespawnEvent += OnPlayerSpawn;
            }
            AgentGlobalService.Instance.SquadRemovedEvent += OnSquadRemoved;
        }

        private void OnPlayerSpawn(GameObject player)
        {
            StartCoroutine(ICreateSquad());
        }

        private IEnumerator ICreateSquad()
        {
            yield return new WaitForEndOfFrame();
            CreateSquad();
            yield return null;
        }

        private void OnDestroy()
        {
            PlayerService.PlayerSpawnEvent -= OnPlayerSpawn;
            PlayerService.PlayerRespawnEvent -= OnPlayerSpawn;
        }

        private void OnSquadRemoved(SoldierSquad squad)
        {
            if (_keepSpawningOnSquadWiped && _lastSquad == squad)
            {
                CreateSquad();
            }
        }

        [ContextMenu("SpawnSquad")]
        public void CreateSquad()
        {
            AgentSpawnPoint[] points = _spawnPoints;
            Array.Sort(points, SortPoints);
            AgentSpawnPoint selected = points[0];
            List<SoldierAgentController> soldiers = new List<SoldierAgentController>();
            int priority = 0;
            //first crear gameobjects
            foreach (GameObject obj in _soldierPrefabs)
            {
                SoldierAgentController controller = Instantiate(obj).GetComponent<SoldierAgentController>();
                soldiers.Add(controller);
                //overriding el class member porque no lo encuentra por el orden de ejecucion todo violado este
                controller.GetComponent<NavMeshAgent>().Warp(selected.Position + new Vector3(UnityEngine.Random.insideUnitCircle.x, 0, UnityEngine.Random.insideUnitCircle.y) * UnityEngine.Random.Range(0, selected.Radius));
                controller.GetComponent<NavMeshAgent>().avoidancePriority = priority;

                priority++;

                //Set Target State
            }
            //Crear logic squad
            SoldierSquad squad = AgentGlobalService.Instance.CreateSquad(soldiers.ToArray());
            _lastSquad = squad;
            StartCoroutine(SetState(squad, _beginState));
            squad.SquadCanLoseContact = _canLoseContact;
            if (_holdPosition)
            {
                squad.SetGoalHold(_holdTransform, _spawnRadius);
            }
            else squad.SetGoalChase();
        }

        private int SortPoints(AgentSpawnPoint x, AgentSpawnPoint y)
        {
            int compare = 0;
            compare += x.IsVisible().CompareTo(y.IsVisible());
            compare -= x.GetDistance().CompareTo(y.GetDistance());
            return compare;
        }

        private IEnumerator SetState(SoldierSquad squad, SquadBeginState beginState)
        {
            yield return new WaitForSeconds(1);

            switch (beginState)
            {
                case SquadBeginState.ACTBUSY:

                    yield break;
                case SquadBeginState.RUSH:
                    squad.ForceEngage();
                    yield break;
                case SquadBeginState.COVER:

                    yield break;
            }
        }

        private void OnDrawGizmos()
        {
            foreach (AgentSpawnPoint spawnPoint in _spawnPoints)
            {
                Gizmos.DrawWireSphere(spawnPoint.Position, spawnPoint.Radius);
            }

            if (_holdTransform)
            {
                Gizmos.color = (Color.yellow + Color.red) / 2;
                Gizmos.DrawLine(transform.position, _holdTransform.position);
            }
        }

        private enum SquadBeginState
        {
            ACTBUSY,
            RUSH,
            COVER
        }
    }
}