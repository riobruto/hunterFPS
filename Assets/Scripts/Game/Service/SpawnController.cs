using UnityEngine;

namespace Game.Service
{
    internal class SpawnController : MonoBehaviour
    {
        [SerializeField] private Transform[] _spawnPoints;

        public Transform[] SpawnPoints { get => _spawnPoints; }

        private void OnDrawGizmos()
        {
            foreach (Transform t in _spawnPoints)
            {
                DrawSpawnGizmo(t);
            }
        }

        private void DrawSpawnGizmo(Transform transform)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(Vector3.up * 1.75f, 0.25f);
            Gizmos.DrawCube(Vector3.up * 1.125f, new Vector3(.5f, .75f, .25f));
            Gizmos.DrawCube(Vector3.up * .375f + Vector3.right * .125f, new Vector3(.15f, .75f, .15f));
            Gizmos.DrawCube(Vector3.up * .375f + Vector3.right * -.125f, new Vector3(.15f, .75f, .15f));
            Gizmos.color = Color.green;
            Gizmos.DrawCube(Vector3.up * 1.75f + Vector3.forward * 0.25f, new Vector3(.35f, .15f, .15f));
        }
    }
}