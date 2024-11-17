using System;
using UnityEngine;

namespace Game.Train.Visual
{
    public class BogieAnimation : MonoBehaviour
    {
        private Bogie _bogie;
        [SerializeField] private AnimationCurve _bounce;

        [SerializeField] private Transform _mesh;
        private float _animationSpeed = .4f;
        private float _animationTime;
        private Quaternion offset;

        private void Start()
        {
            _bogie = GetComponent<Bogie>();
            _bogie.RailJointEvent += OnJoint;
            offset = _mesh.localRotation;
        }

        private void OnJoint()
        {
            _animationTime = 0;
        }

        private void LateUpdate()
        {
            _animationTime += Time.deltaTime * _bogie.Velocity.magnitude;
            _mesh.localRotation = Quaternion.Euler(new Vector3(_bounce.Evaluate(_animationTime), 0, 0)) * offset;
        }
    }
}