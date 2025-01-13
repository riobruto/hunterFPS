using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(fileName = "New Inventory Item", menuName = "Inventory/InventoryItem")]
    public class InventoryItem : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Vector2Int _size = Vector2Int.one;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _stackeable;
        [SerializeField] private int _stackWhenPicked;
        [SerializeField] private int _stackLimit;
        [SerializeField] private GameObject _prefab;

        public string Name => _name;
        public string Description => _description;
        public int Width => _size.x;
        public int Height => _size.y;
        public Sprite Icon => _icon;
        public bool Stackeable => _stackeable;
        public int AmountPerObject => _stackWhenPicked;
        public int StackSize => _stackLimit;
        public GameObject Prefab => _prefab;
    }
}