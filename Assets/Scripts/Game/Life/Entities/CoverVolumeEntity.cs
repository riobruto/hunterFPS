using UnityEngine;

namespace Game.Entities
{
    public class CoverVolumeEntity : MonoBehaviour
    {
        private BoxCollider colliderVolume;
        [SerializeField] private Vector3 _size = Vector3.one;
        [SerializeField] private Vector3 _center = Vector3.zero;   
        public BoxCollider Collider => colliderVolume;

        private void Start()
        {
            GameObject go = new GameObject("Cover Volume");
            go.layer = 6;
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = _size;
            collider.center = _center;
            collider.excludeLayers = -1;

            colliderVolume = collider;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1, 0, 1, .33f);
            Gizmos.DrawCube(_center, _size);
        }
    }
}