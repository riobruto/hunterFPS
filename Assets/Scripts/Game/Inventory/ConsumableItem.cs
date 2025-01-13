using System;
using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(fileName = "New Consumable Item", menuName = "Inventory/ConsumableItem")]
    public class ConsumableItem : InventoryItem
    {
        [Header("Consumable Info")]
        [SerializeField] private ConsumableProperties _properties;

        [SerializeField] private GameObject _animationGameObject;

        [Tooltip("The name that is going to appear in the menu to use")]
        [SerializeField] private string _consumeActionName;

        public bool CanConsumeWithMask;
        public bool CanConsumeDog;
        public string ConsumeText { get => _consumeActionName; }
        public ConsumableProperties Properties { get => _properties; }
        public GameObject AnimationGameObject { get => _animationGameObject; }
    }

    [Serializable]
    public class ConsumableProperties
    {
        public float ConsumeTimeInSeconds;

        [Header("Properties")]
        public float HealthRecoverAmount;

        public float DurationInMinutes;
        public float DamageResistanceAmount;
        public float StaminaResistanceAmount;
    }
}