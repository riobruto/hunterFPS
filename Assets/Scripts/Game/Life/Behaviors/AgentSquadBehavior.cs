using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Life
{
    public class AgentSquadBehavior : MonoBehaviour
    {
        private List<AgentSquadBehavior> _nearAgents;
        private int _squadID;

        private bool _isLeader = false;
        public bool IsLeader { get => _isLeader; }

        [SerializeField] private bool _StartAsLeader;

        private void Start()
        {
            _isLeader = _StartAsLeader;
            _nearAgents = FindObjectsOfType<AgentSquadBehavior>().ToList();
            _nearAgents.Remove(this);
        }

        private void MakeLeader()
        {
            _isLeader = true;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = _isLeader ? Color.red : Color.blue;

            foreach (AgentSquadBehavior agent in _nearAgents)
            {
                Gizmos.DrawLine(transform.position, agent.transform.position);
            }
        }



        internal void MakeNearestAgentLeader()
        {
            Array.Sort(_nearAgents.ToArray(), SortByDistance);
            _nearAgents[0].MakeLeader();
        }

        private int SortByDistance(AgentSquadBehavior A, AgentSquadBehavior B)
        {
            if (A == null && B != null) { return 1; }
            else if (A != null && B == null) { return -1; }
            else if (A == null && B == null) { return 0; }
            else return
                    Vector3.Distance(transform.position, A.transform.position).CompareTo(
                        Vector3.Distance(transform.position, B.transform.position));
        }
    }
}