using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerBaseMovement : MonoBehaviour
    {
        private PlayerMovementController _playerMovementManager;


        public PlayerMovementController Manager
        { get { return _playerMovementManager; } }


        public void SetManager(PlayerMovementController owner)
        {
            _playerMovementManager = owner;
        }

        private void Start()
        {
            _playerMovementManager = GetComponent<PlayerMovementController>();
        }

        internal void Initialize()
        {
            OnStart();
        }

        private void Update()
        {
            OnUpdate();
        }

        private void FixedUpdate()
        {
            OnFixedUpdate();
        }
        protected virtual void OnStart()
        {
        }

        protected virtual void OnUpdate()
        {
        }
        protected virtual void OnFixedUpdate()
        {

        }

    }
}