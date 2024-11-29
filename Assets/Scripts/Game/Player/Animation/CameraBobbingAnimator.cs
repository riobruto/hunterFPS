using Core.Engine;
using Game.Player.Movement;
using UnityEngine;

namespace Game.Player.Animation
{
    public class CameraBobbingAnimator : MonoBehaviour, IObserverFromPlayerMovement
    {
        private PlayerRigidbodyMovement _controller;
        private GameSettings settings;

        public void Initalize(PlayerRigidbodyMovement controller)
        {
            settings = Bootstrap.Resolve<GameSettings>();
            _controller = controller;
        }

        public void Detach(PlayerRigidbodyMovement controller)
        {
            _controller = null;
        }

        private Vector3 _heading;
        [Header("Run and Walk Bobbing")]
        [SerializeField] private float _headingIntensity;
        [SerializeField] private float _intensity;
        [SerializeField] private float _frequency;
        [SerializeField] private float _amplitude;
        [SerializeField] private Vector3 _rotationMultiplier;
        [SerializeField] private Vector3 _positionMultiplier;
        [SerializeField][Range(0f, 10f)] private float _noiseMagnitude;
        [SerializeField][Range(0f, 10f)] private float _noiseTime;
        private float _rateOfWaveTime;

        private void LateUpdate()
        {
            Vector3 dir = _controller.RelativeVelocity;
         
            dir.y = 0;
            _rateOfWaveTime = dir.magnitude;

            Vector3 bob = BobbingWave() * _intensity;            
            if (_controller.CurrentState == PlayerMovementState.FALLING) bob = Vector3.zero;

            _heading = new Vector3(dir.z, 0, (dir.x * -1f));

            Vector3 rotationBob;
            rotationBob.x = bob.x * _rotationMultiplier.x;
            rotationBob.y = bob.y * _rotationMultiplier.y;
            rotationBob.z = bob.z * _rotationMultiplier.z;

            transform.localRotation = Quaternion.Euler((_heading * _headingIntensity) + rotationBob + new Vector3(Noise().x, Noise().y));
            //Moviendo el x a y para un movimiento mas natural

            Vector3 positionBob;
            positionBob.x = bob.y * _positionMultiplier.y;
            positionBob.y = bob.x * _positionMultiplier.x;
            positionBob.z = bob.z * _positionMultiplier.z;

            transform.localPosition = positionBob;
        }

        private float _waveTime;

        private Vector3 BobbingWave()
        {
            _waveTime += Time.deltaTime * _rateOfWaveTime;

            //if (!_controller.Controller.isGrounded) return Vector2.zero;
            float x = Mathf.Sin(_frequency * (_waveTime * 2f)) * _amplitude * 2f;
            float y = (Mathf.Cos(_frequency * _waveTime) * _amplitude);
            float z = (Mathf.Cos(_frequency * _waveTime) * _amplitude);

            return new Vector3(x, y, z);
        }

        private Vector2 Noise()
        {
            Vector2 v = new Vector2(Mathf.PerlinNoise(Time.time * _noiseTime, 0f), Mathf.PerlinNoise(0f, Time.time * _noiseTime));
            v.x = Remap(v.x, 1, 0, 1, -1);
            v.y = Remap(v.y, 1, 0, 1, -1);
            return v * _noiseMagnitude;
        }

        private float Remap(float v, float maxIn, float minIn, float maxOut, float minOut)
        {
            float t = Mathf.InverseLerp(minIn, maxIn, v);
            return Mathf.Lerp(minOut, maxOut, t);
        }
    }
}