using Game.Inventory;

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Inventory
{
    public class InventoryContextMenu : MonoBehaviour
    {
        [SerializeField] private Button _use;
        [SerializeField] private TMP_Text _useActionName;
        [SerializeField] private Button _drop;

        public event UnityAction Use;
        public event UnityAction Drop;

        private void OnEnable()
        {
            _use.onClick.AddListener(Use);
            _drop.onClick.AddListener(Drop);
        }

        public void Show(InventoryItem item)
        {
            gameObject.SetActive(true);
            _useActionName.text = ResolveItemText(item);
        }

        private string ResolveItemText(InventoryItem item)
        {
            switch (item)
            {
                case EquipableItem: return "Equip";
                case ConsumableItem: return "Consume";              
                case InventoryItem: return "NULL";
                case null: return "NULL";
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _use.onClick.RemoveListener(Use);
            _drop.onClick.RemoveListener(Drop);
        }
    }
}