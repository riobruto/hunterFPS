using Game.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Inventory
{
    public class UI_InventoryItem : MonoBehaviour
    {
        private InventoryItem _itemData;
        public InventoryItem ItemData => _itemData;
        public Vector2Int Position;

        [SerializeField] private Image _icon;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _number;

        private int _currentStackAmount;
        public int StackAmount => _currentStackAmount;

        internal void Set(InventoryItem itemData)
        {

            _itemData = itemData;
            _icon.sprite = itemData.Icon;
        }




        public void SetStackSize(int value)
        {
            _currentStackAmount = value;
            _number.text = _itemData.Stackeable ? $"{_currentStackAmount}" : "";
        }

        internal void Pick()
        {
            _background.color = new Color(1, 1, 1, 0);
            _icon.color = new Color(1, 1, 1, .25f);
        }

        internal void Place()
        {
            _background.color = new Color(1, 1, 1, .75f);
            _icon.color = new Color(1, 1, 1, 1);
        }
    }
}