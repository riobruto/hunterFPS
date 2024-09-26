using System;
using UnityEngine;

namespace Core.Configuration
{
    [CreateAssetMenu(menuName = nameof(PlayerConfiguration), fileName = "Player Configuration Asset", order = 1)]
    public class PlayerConfiguration : ScriptableObject
    {
        [SerializeField] private PlayerControlSettings _settings;

        public PlayerControlSettings Settings => _settings;

        [Serializable]
        public class PlayerControlSettings
        {
            [Header("Head")]
            [Range(1f, 100f)]
            [SerializeField] private float _sensitivity;

            [Range(1f, 100f)]
            [SerializeField] private float _aimSensitivity;

            [Header("Movement")]
            [SerializeField] private float _walkSpeed;

            [SerializeField] private float _gravity;

            [SerializeField] private float _crouchSpeed;
            [SerializeField] private float _runSpeed;
            [SerializeField] private float _flySpeed;
            [SerializeField] private float _jumpHeight;

            [SerializeField] private float _leanAngle;

            [SerializeField] private float _staminaDecreaseRate;
            [SerializeField] private float _staminaIncreaseRate;

            [Header("Health")]
            [SerializeField] private float _maxHealth;

            [SerializeField] private float _healthIncreaseRate;

            [Header("Camera")]
            [SerializeField] private float _FOVGround;
            [SerializeField] private float _FOVFlying;
            

            public float NormalSensitivity { get => _sensitivity; }
            public float AimSensitivity { get => _aimSensitivity; }
            public float Gravity { get => _gravity; }

            public float WalkSpeed { get => _walkSpeed; }
            public float FlySpeed { get => _flySpeed; }
            public float CrouchSpeed { get => _crouchSpeed; }
            public float RunSpeed { get => _runSpeed; }
            public float JumpHeight { get => _jumpHeight; }
            public float LeanMaxAngle { get => _leanAngle; }
            public float StaminaDecreaseRate { get => _staminaDecreaseRate; }
            public float StaminaIncreaseRate { get => _staminaIncreaseRate; }
            public float MaxHealth { get => _maxHealth; }
            public float HealthIncreaseRate { get => _healthIncreaseRate; }

            public float FOVGround { get => _FOVGround; }
            public float FOVFlying { get => _FOVFlying; }
        }
    }
}