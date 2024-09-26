using System;
using UnityEngine;

namespace Game.Animation
{
    [CreateAssetMenu(fileName = "New TransformAnimationClip", menuName = "CurveAnimation/AnimationTransformCurve")]
    public class AnimationTransformCurve : ScriptableObject
    {
        [SerializeField] private AnimationVector3 _position;
        [SerializeField] private AnimationVector3 _rotation;

        public AnimationVector3 Position { get => _position; }
        public AnimationVector3 EulerRotation { get => _rotation; }
        public float PositionScaleMultiplier { get => _positionScaleMultiplier; }
        public float RotationScaleMultiplier { get => _rotationScaleMultiplier; }

        public void Evaluate(float time, out Vector3 position, out Vector3 eulerRotation)
        {
            position = _position.Evaluate(time) * _positionScaleMultiplier;
            eulerRotation = _rotation.Evaluate(time) * _rotationScaleMultiplier;
        }

        public void Evaluate(float time, out Vector3 position, out Quaternion rotation)
        {
            position = _position.Evaluate(time) * _positionScaleMultiplier;
            rotation = Quaternion.Euler(_rotation.Evaluate(time) * _rotationScaleMultiplier);
        }

        [SerializeField] private float _positionScaleMultiplier = 1f;
        [SerializeField] private float _rotationScaleMultiplier = 1f;
    }

    [Serializable]
    public class AnimationVector3
    {
        [SerializeField] private AnimationCurve _x;
        [SerializeField] private AnimationCurve _y;
        [SerializeField] private AnimationCurve _z;

        public AnimationCurve x => _x;
        public AnimationCurve y => _y;
        public AnimationCurve z => _z;

        public Vector3 Evaluate(float time)
        {
            Vector3 v;
            v.x = x.Evaluate(time);
            v.y = y.Evaluate(time);
            v.z = z.Evaluate(time);
            return v;
        }
    }

    public class AnimationVector2
    {
        [SerializeField] private AnimationCurve _x;
        [SerializeField] private AnimationCurve _y;

        public AnimationCurve x => _x;
        public AnimationCurve y => _y;

        public Vector2 Evaluate(float time)
        {
            Vector2 v;
            v.x = x.Evaluate(time);
            v.y = y.Evaluate(time);

            return v;
        }
    }
}