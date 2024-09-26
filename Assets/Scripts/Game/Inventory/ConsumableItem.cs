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
        [SerializeField] private GameObject _worldGameObject;

        [Tooltip("The name that is going to appear in the menu to use")]
        [SerializeField] private string _consumeActionName;

        public bool CanConsumeWithMask;
        public string ConsumeText { get => _consumeActionName; }
        public ConsumableProperties Properties { get => _properties; }
        public GameObject AnimationGameObject { get => _animationGameObject; }
        public GameObject WorldGameObject { get => _worldGameObject; }
    }

    [Serializable]
    public class ConsumableProperties
    {
        public float ConsumeTime;
        public PlayerModificatorProperty Health;
        public PlayerModificatorProperty Stamina;
        public PlayerModificatorProperty Acurracy;
        public PlayerModificatorProperty Speed;
    }

    [Serializable]
    public class PlayerModificatorProperty
    {
        [Tooltip("The amount by frame that is going to apply during a period of time")]
        public float AmountOverTime;

        public float Duration;
    }
}