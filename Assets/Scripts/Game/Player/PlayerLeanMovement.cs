using Game.Player.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    public class PlayerLeanMovement : MonoBehaviour
    {
        [SerializeField] private Transform _leanTransform;
        public float LeanVector { get; private set; }
        public bool AllowLean { get; internal set; }

      
        protected void Update()
        {
            float target;

            target = AllowLean ? -LeanVector * 22 : 0;

            if (CheckLeanCollision(LeanVector))
            {
                target = 0;
            }

            _leanTransform.localRotation = Quaternion.Slerp(_leanTransform.localRotation, Quaternion.Euler(0, 0, target), Time.deltaTime * 5f);
        }

        private bool CheckLeanCollision(float vector)
        {
            Debug.DrawRay(transform.position + transform.up * 2f, transform.right * vector);
            return Physics.Raycast(transform.position + transform.up * 2f, transform.right * vector, 1);
        }

        private void OnLean(InputValue value)
        {
            LeanVector = value.Get<float>();
        }
    }
}