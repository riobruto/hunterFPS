using Core.Engine;
using Game.Player.Controllers;
using Game.Service;
using Nomnom.RaycastVisualization;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Life
{
    public class AgentPlayerBehavior : MonoBehaviour
    {
        [SerializeField] private LayerMask _ignoreMask;
        [SerializeField] private Transform _head;
        [SerializeField] private float _rangeDistance = 20;

        private Vector3 _lastKnownPosition;
        private bool _lastPlayerDetected;
        private bool _playerDetected => IsPlayerInRange(_rangeDistance) && IsPlayerInViewAngle(0.8f) && IsPlayerVisible();
        private GameObject _player;
        private Camera _playerCamera;

        public Vector3 PlayerPosition => _player.transform.position;
        public Vector3 PlayerHeadPosition => _playerCamera.transform.position;

        public GameObject PlayerGameObject { get => _player; }
        public Vector3 LastKnownPosition;


        public UnityEvent<float, Vector3> HeardPlayerEvent;

        public bool PlayerDetected => _playerDetected;

        public bool HearedSteps { get; private set; }

        private PlayerSoundController _playerSound;

        private void Start()
        {
            HearedSteps = false;

            _player = Bootstrap.Resolve<PlayerService>().Player;
            _playerCamera = Bootstrap.Resolve<PlayerService>().PlayerCamera;
            _ignoreMask = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.IgnoreLayers;
            _playerSound = _player.GetComponentInChildren<PlayerSoundController>();

            _playerSound.StepSound += OnPlayerStep;
            _playerSound.GunSound += OnPlayerGun;
        }

        private void OnPlayerGun(Vector3 position, float radius)
        {
            if (Vector3.Distance(position, transform.position) <= radius)
            {
                HearedSteps = true;
                LastKnownPosition = position;
                HeardPlayerEvent?.Invoke(Time.time, position);
            }
        }

        private void OnPlayerStep(Vector3 position, float radius)
        {
            if (Vector3.Distance(position, transform.position) <= radius)
            {
                HearedSteps = true;
                LastKnownPosition = position;
                HeardPlayerEvent?.Invoke(Time.time, position);
            }
        }

        private void Update()
        {
            if (_playerDetected != _lastPlayerDetected)
            {
                _lastKnownPosition = _player.transform.position;
                _lastPlayerDetected = _playerDetected;
            }
        }

        public bool IsPlayerInRange(float distance)
        {
            return Vector3.Distance(transform.position, _playerCamera.transform.position) < distance;
        }

        public bool IsPlayerInViewAngle(float dotAngle)
        {
            return Vector3.Dot(transform.forward, _playerCamera.transform.position - transform.position) > dotAngle;
        }

        public bool IsPlayerVisible()
        {
            Debug.DrawLine(_playerCamera.transform.position, transform.position);

            if (VisualPhysics.Linecast(_playerCamera.transform.position, _head.position, out RaycastHit hit, _ignoreMask))
            {
                return hit.collider.gameObject.transform.root == transform;
            }

            return false;
        }
    }
}