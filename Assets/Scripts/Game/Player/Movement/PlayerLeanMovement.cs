using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    public class PlayerLeanMovement : PlayerBaseMovement
    {
        [SerializeField] private Transform _leanTransform;
        public float LeanVector { get; private set; }
        public bool AllowLean { get; internal set; }


        protected override void OnUpdate()
        {
            float target;

            target = AllowLean ? -LeanVector * Manager.Settings.LeanMaxAngle : 0;

            if (CheckLeanCollision(LeanVector))
            {
                target = 0;
            }
            //TODO: fijar rotacion en un punto orientado a la camara
            //stalker type rotation
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