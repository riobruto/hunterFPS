using Life.Controllers;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] _prefabs;
    [SerializeField] private Transform[] _targets;

    public void SpawnSquad()
    {
        StartCoroutine(ISpawnSquad());
    }

    private IEnumerator ISpawnSquad()
    {
        int index = 0;
        foreach (GameObject enemy in _prefabs)
        {
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1, NavMesh.AllAreas);
            GameObject go = Instantiate(enemy, hit.position, Quaternion.identity);
            go.GetComponent<SoldierAgentController>().ForceGoToPoint(_targets[index].position);
            index++;
            yield return new WaitForSeconds(1);
        }
        yield break;
    }

    private void OnDrawGizmos()
    {
        foreach (Transform t in _targets)
        {
            Gizmos.DrawCube(t.position, Vector3.one + Vector3.up);
        }
    }
}