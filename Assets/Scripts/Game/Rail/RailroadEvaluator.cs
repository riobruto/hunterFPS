using UnityEngine;

namespace Rail
{
    [ExecuteInEditMode]
    public class RailroadEvaluator : MonoBehaviour
    {
        [SerializeField] private Railroad _currentRail;
        [SerializeField] private Transform _mesh;
		private void Update()
		{
            if (!_mesh) return;
			RailData data = _currentRail.GetRailDataFromPoint(transform.position);
            _mesh.position = data.NearestPosition;

		}

		private void OnDrawGizmos()
        {
           
        }
    }
}