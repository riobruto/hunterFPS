using Core.Engine;
using Game.Service;
using Life.Controllers;
using Nomnom.RaycastVisualization;
using System.Collections.Generic;

using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Life
{
    public class AgentCoverSensor : MonoBehaviour
    {
        internal const float DEFAULHEIGHT = 1.75f;

        [SerializeField] private float _detectionRadius = 20;
        [SerializeField] private int _detectionResolution = 250;
        [SerializeField] private int _forwardTiles = 16;
        [SerializeField] private int _rightTiles = 16;

        private float _height = 1.75f;
        private float _crouchHeight = .75f;
        private NavMeshAgent _agent;

        public float DetectionRadius { get => _detectionRadius; set => _detectionRadius = value; }
        public int DetectionResolution { get => _detectionResolution; set => _detectionResolution = value; }
        public float Height { get => _height; set => _height = value; }
        public float CrouchHeight { get => _crouchHeight; set => _crouchHeight = value; }

        public bool AvoidNearAgents = true;

        public List<AgentController> NearAgentControllers;
        private bool _useCircle;

        public void SetHeights(float standing, float crouched)
        {
            _height = standing;
            _crouchHeight = crouched;
        }

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        //now this points of data make a beautiful line

        private void OnDrawGizmos()
        {
        }

        public IEnumerable<SpatialDataPoint> GetCombatSpatialData(Vector3 position, Vector3 threat, float radius = 20, float safeDistance = 15)
        {
            if (_useCircle)
            {
                for (int i = 1; i < _detectionResolution; i++)
                {
                    float dist = Mathf.Pow(i / (_detectionResolution - 1f), 0.5f) * radius;
                    float angle = 2 * Mathf.PI * 1.618035f * i;
                    float x = dist * Mathf.Cos(angle);
                    float z = dist * Mathf.Sin(angle);

                    Vector3 point = new Vector3(x, 0, z);
                    bool validPoint = NavMesh.SamplePosition(position + point, out NavMeshHit hit, 5, NavMesh.AllAreas);
                    if (Vector3.Distance(hit.position, threat) < safeDistance) continue;
                    if (!validPoint) continue;
                    NavMeshPath path = new NavMeshPath();
                    bool validPath = _agent.CalculatePath(position + point, path);
                    if (!validPath || path.status == NavMeshPathStatus.PathPartial) continue;

                    if (AvoidNearAgents)
                    {
                        bool findedNearAgent = false;

                        foreach (AgentController controller in AgentGlobalService.Instance.ActiveAgents)
                        {
                            findedNearAgent = Vector3.Distance(hit.position, controller.transform.position) < 3;
                        }

                        if (findedNearAgent) continue;
                    }

                    yield return new(
                       hit.position,
                       CalculateOcclusionPoint(hit.position + Vector3.up * _height, threat),
                       Vector3.Distance(hit.position, threat),
                       Vector3.Distance(hit.position, position),
                       HasVisual(hit.position + Vector3.up * _height, threat),
                       HasVisual(hit.position + Vector3.up * _crouchHeight, threat),
                       threat.y - (hit.position.y + _height) > 1.5f,
                       path); ;
                }
            }

            for (int x = 1; x < _rightTiles; x++)
            {
                for (int z = 1; z < _forwardTiles; z++)
                {
                    Vector3 point = new Vector3(x - _rightTiles / 2, 0, z - _forwardTiles / 2);
                    bool validPoint = NavMesh.SamplePosition(position + point, out NavMeshHit hit, 5, NavMesh.AllAreas);
                    if (!validPoint) continue;

                    NavMeshPath path = new NavMeshPath();
                    bool validPath = _agent.CalculatePath(position + point, path);
                    if (!validPath || path.status == NavMeshPathStatus.PathPartial) continue;
                    if (Vector3.Distance(hit.position, threat) < safeDistance) continue;

                    if (AvoidNearAgents)
                    {
                        bool findedNearAgent = false;

                        foreach (AgentController controller in AgentGlobalService.Instance.ActiveAgents)
                        {
                            if (controller == GetComponent<AgentController>()) continue;
                            findedNearAgent = Vector3.Distance(hit.position, controller.transform.position) < 3;
                        }

                        if (findedNearAgent) continue;
                    }

                    yield return new(
                       hit.position,
                       CalculateOcclusionPoint(hit.position + Vector3.up * _height, threat),
                       Vector3.Distance(hit.position, threat),
                       Vector3.Distance(hit.position, position),
                       HasVisual(hit.position + Vector3.up * _height, threat),
                       HasVisual(hit.position + Vector3.up * _crouchHeight, threat),
                       threat.y - (hit.position.y + _height) > 1.5f,
                       path); ;
                }
            }

            yield break;
        }

        public SpatialDataPoint? GetCombatSpatialDataFromPoint(Vector3 position, Vector3 threat, float radius = 20, float safeDistance = 15)
        {
            bool validPoint = NavMesh.SamplePosition(position, out NavMeshHit hit, 5, NavMesh.AllAreas);
            if (!validPoint) return null;

            NavMeshPath path = new NavMeshPath();
            bool validPath = _agent.CalculatePath(position, path);
            if (!validPath || path.status == NavMeshPathStatus.PathPartial) return null;
            if (Vector3.Distance(hit.position, threat) < safeDistance) return null;

            Vector3 occlusionPoint = Vector3.zero;

            return new(
               hit.position,
               CalculateOcclusionPoint(hit.position + Vector3.up * _height, threat),
               Vector3.Distance(hit.position, threat),
               Vector3.Distance(hit.position, position),
               HasVisual(hit.position + Vector3.up * _height, threat),
               HasVisual(hit.position + Vector3.up * _crouchHeight, threat),
               threat.y - (hit.position.y + _height) > 1.5f,
               path); ;
        }

        public bool HasVisual(Vector3 point, Vector3 threat)
        {
            return VisualPhysics.Linecast(threat, point, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers);
        }

        public Vector3 CalculateOcclusionPoint(Vector3 from, Vector3 to)
        {
            Physics.Linecast(from, to, out RaycastHit hit, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.CoverLayers);
            return hit.point;
        }

        /// <summary>
        /// Checks if can peek the enemy
        /// </summary>
        /// <param name="enemy"> The enemy gameobject to check collision </param>
        /// <param name="useRight"> Use the right side of the agent, false if peeking left, STINKY </param>
        /// <returns> has peeking line of fire </returns>
        /*
        public bool CheckPeekingVectorFromEnemy(GameObject enemy, bool useRight)
        {
            Vector3 left = transform.position + transform.up * _height + -transform.right * _agent.radius;
            Vector3 right = transform.position + transform.up * _height + transform.right * _agent.radius;
            VisualPhysics.Linecast(useRight ? right : left, enemy.transform.position + enemy.transform.up * _height, out RaycastHit hitInfo, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.EnemyGunLayers);
            if (hitInfo.collider == null) return false;
            return hitInfo.collider.transform.root == enemy.transform.root;
        }*/
    }

}