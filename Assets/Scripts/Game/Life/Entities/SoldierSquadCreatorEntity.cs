using Game.Service;
using Life.Controllers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;

namespace Game.Life.Entities
{
    public class SoldierSquadCreatorEntity : MonoBehaviour
    {
        [SerializeField] private GameObject[] _soldierPrefabs;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _spawnRadius = 5f;
        [SerializeField] private SquadBeginState _beginState;

        [SerializeField] private bool _createOnStart;

        [Header("Squad Goal")]
        [SerializeField] private bool _holdPosition;

        [SerializeField] private Transform _holdTransform;

        [SerializeField] private bool _squadDetectsPlayerAlways;
        [SerializeField] private bool _keepSpawningOnSquadWiped;
        private SoldierSquad _lastSquad;

        private void Start(){
            if (_createOnStart){
                CreateSquad();
            }
            AgentGlobalService.Instance.SquadRemovedEvent += OnSquadRemoved;
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
            List<SoldierAgentController> soldiers = new List<SoldierAgentController>();
            int priority = 0;
            //first crear gameobjects
            foreach (GameObject obj in _soldierPrefabs)
            {
                SoldierAgentController controller = Instantiate(obj).GetComponent<SoldierAgentController>();
                soldiers.Add(controller);
                //overriding el class member porque no lo encuentra por el orden de ejecucion todo violado este
                controller.GetComponent<NavMeshAgent>().Warp(_spawnPoint.position + new Vector3(Random.insideUnitCircle.x, 0, Random.insideUnitCircle.y) * Random.Range(0, _spawnRadius));
                controller.GetComponent<NavMeshAgent>().avoidancePriority = priority;

                priority++;
             
                //Set Target State
            }
            //Crear logic squad
            SoldierSquad squad = AgentGlobalService.Instance.CreateSquad(soldiers.ToArray());
            _lastSquad = squad;
            StartCoroutine(SetState(squad, _beginState));
            squad.SquadDetectPlayerAlways = _squadDetectsPlayerAlways;
            if (_holdPosition)
            {
                squad.SetGoalHold(_holdTransform, _spawnRadius);
            }
            else squad.SetGoalChase();

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