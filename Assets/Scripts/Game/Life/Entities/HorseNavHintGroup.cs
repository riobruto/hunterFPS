using System;
using System.Collections.Generic;
using System.Linq;
using Game.Life.WaypointPath;
using UnityEditor;
using UnityEngine;

namespace Life.Entities
{
    internal enum HorseNavHitType
    {
        COMBAT,
        IDLE,
        EXIT
    }

    public class HorseNavHint
    {
        private HorseNavHint[] _neightbours;
        public Vector3 position;

        public HorseNavHint[] Neightbours { get => _neightbours; }

        public void SetNeightbours(HorseNavHint[] neightbours)
        {
            _neightbours = neightbours;
        }
    }

    public class HorseNavHintGroup : MonoBehaviour
    {
        [SerializeField] private HorseNavHitType _type;
        [SerializeField] private float _mergeDistance = 5;
        private Transform[] _combatHints;

        private HorseNavHint[] _hints;

        private void Start()
        {
            _combatHints = transform.Cast<Transform>().ToArray();
            BuildHints();
        }

        [ContextMenu("Build")]
        private void BuildHints()
        {
            _hints = new HorseNavHint[_combatHints.Length];

            for (int i = 0; i < _hints.Length; i++)
            {
                _hints[i] = new HorseNavHint();
                _hints[i].position = _combatHints[i].position;
            }
            //build neightbours
            for (int i = 0; i < _hints.Length; i++)
            {
                List<HorseNavHint> neightbours = new List<HorseNavHint>();
                //add by distance
                foreach (HorseNavHint n in _hints)
                {
                    if (n == _hints[i]) { continue; }

                    if (Vector3.Distance(n.position, _hints[i].position) < _mergeDistance)
                    {
                        neightbours.Add(n);
                    }
                }
                _hints[i].SetNeightbours(neightbours.ToArray());
            }
        }

        public Transform NearestTransformFromPoint(Vector3 point)
        {
            pos = point;
            Transform[] hints = _combatHints;
            Array.Sort(hints, CompareDistance);
            return hints[0];
        }

        public HorseNavHint NearestHintFromPoint(Vector3 point)
        {
            pos = point;
            HorseNavHint[] hints = _hints;
            Array.Sort(hints, CompareDistance);
            return hints[0];
        }

        public HorseNavHint[] FindPath(HorseNavHint from, HorseNavHint to)
        {
            if (from == to)
            {
                Debug.LogError("target and origin are the same PELOTUDAZO");
                return null;
            }
            foreach (HorseNavHint hint in from.Neightbours)
            {
                if (to == hint)
                {
                    Debug.LogWarning("path is too short!");
                    return new HorseNavHint[] { from, to };
                }
            }
            return SolvePath(from, to);
        }

        private HorseNavHint[] SolvePath(HorseNavHint from, HorseNavHint to)
        {
            Queue<HorseNavHint> BFSQueue = new();
            Dictionary<HorseNavHint, HorseNavHint> visited = new Dictionary<HorseNavHint, HorseNavHint>();
            BFSQueue.Enqueue(from);
            HorseNavHint[] previous = new HorseNavHint[_hints.Length];
            visited.Add(from, null);
            bool pathFound = false;

            while (BFSQueue.Count > 0)
            {
                HorseNavHint h = BFSQueue.Dequeue();
                HorseNavHint[] neightbours = h.Neightbours;

                if (h == to)
                {
                    //found it
                    pathFound = true;
                    break;
                }

                for (int i = 0; i < neightbours.Length; i++)
                {
                    if (!visited.ContainsKey(neightbours[i]))
                    {
                        visited.Add(neightbours[i], h);
                        BFSQueue.Enqueue(neightbours[i]);
                        previous[i] = h;
                    }
                }
            }
            if (pathFound)
            {
                List<HorseNavHint> path = new List<HorseNavHint>();
                HorseNavHint current = to;

                while (current != null)
                {
                    path.Add(current);
                    current = visited[current];
                }

                path.Reverse(); // Reverse to get start->end order
                return path.ToArray();
            }
            else return null;
        }

      
        public Transform[] CombatHints => _combatHints;

        private Vector3 pos;
        public HorseNavHint[] Hints { get => _hints; }

        public int CompareDistance(Transform hintA, Transform hintB)
        {
            return Vector3.Distance(hintA.transform.position, pos).CompareTo(Vector3.Distance(hintB.transform.position, pos));
        }

        public int CompareDistance(HorseNavHint hintA, HorseNavHint hintB)
        {
            return Vector3.Distance(hintA.position, pos).CompareTo(Vector3.Distance(hintB.position, pos));
        }

        [SerializeField] private Transform _endTest;
        [SerializeField] private Transform _startTest;
        [SerializeField] private bool _debugPath;

        private void OnDrawGizmos()
        {
         
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.red;
                List<Transform> children = transform.Cast<Transform>().ToList();

                foreach (Transform child in children)
                {
                    foreach (Transform childx in children)
                    {
                        if (childx == child) { continue; }
                        if (Vector3.Distance(childx.position, child.position) < _mergeDistance)
                        {
                            Gizmos.DrawLine(childx.position, child.position);
                        }
                    }
                    Gizmos.DrawCube(child.position, Vector3.one * 0.25f);
                }
                return;
            }

            foreach (HorseNavHint child in _hints)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(child.position, Vector3.one * 0.25f);

                foreach (HorseNavHint n in child.Neightbours)
                {
                    Gizmos.DrawLine(child.position, n.position);
                }
            }
        }
    }
}