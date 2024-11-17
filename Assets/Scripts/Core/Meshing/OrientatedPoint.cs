using UnityEngine;

namespace Core.Meshing
{
    public struct OrientatedPoint
    {
        public Vector3 position;
        public Vector3 forward;
        public Quaternion rotation;

        public OrientatedPoint(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
            this.forward = rotation * Vector3.forward;

        }

        public OrientatedPoint(Vector3 position, Vector3 forward)
        {
            this.position = position;
            this.rotation = Quaternion.LookRotation(forward);
            this.forward = forward;
        }
        public OrientatedPoint(Vector3 position, Vector3 forward, Vector3 up)
        {
            this.position = position;
            this.rotation = Quaternion.LookRotation(forward, up);
            this.forward = forward;
        }
        public Vector3 LocalToWorld(Vector3 point)
        {
            return position + rotation * point;
        }

        public Vector3 WorldToLocal(Vector3 point)
        {
            return Quaternion.Inverse(rotation) * (point - position);
        }
        public Vector3 LocalToWorldDirection(Vector3 direction)
        {
            return rotation * direction;
        }


    }
}