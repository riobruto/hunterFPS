using Nomnom.RaycastVisualization;
using UnityEngine;

namespace Game.Life.Animation
{
    public class IKFootSolver : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private float _footSpacing;
        [SerializeField] private float _stepDistance;
        [SerializeField] private LayerMask _layerMask;

        private Vector3 _currentPosition;
        private float _lerp;
        private Vector3 _newPosition;
        [SerializeField] private float _stepHeight;
        [SerializeField] private float _lerpSpeed;
        private Vector3 _oldPosition;
  
        private void LateUpdate()
        {
            transform.position = _currentPosition;

            Ray ray = new Ray(body.position + (body.right * _footSpacing), Vector3.down);

            if (VisualPhysics.Raycast(ray, out RaycastHit hit, 10, _layerMask))
            {
                if (Vector3.Distance(_newPosition, hit.point) > _stepDistance)
                {
                    _lerp = 0;
                    _newPosition = hit.point;
                }
            }

            if (_lerp < 1)
            {
                Vector3 footPos = Vector3.Lerp(_oldPosition, _newPosition, _lerp);
                footPos.y += Mathf.Sin(_lerp * Mathf.PI) * _stepHeight;
                _currentPosition = footPos;
                _lerp += Time.deltaTime * _lerpSpeed;
            }
            else
            {
                _oldPosition = _newPosition;
            }
        }
    }
}