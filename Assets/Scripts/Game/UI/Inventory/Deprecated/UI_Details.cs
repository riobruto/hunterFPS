using Game.Inventory;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Inventory
{
    public class UI_Details : MonoBehaviour
    {
        [SerializeField] private TMP_Text _descriptionField;
        [SerializeField] private TMP_Text _nameField;
        [SerializeField] private TMP_Text _statisticsField;
        [SerializeField] private Button _close;

        public void Initialize(InventoryItem item)
        {
            _nameField.text = item.Name;
            _descriptionField.text = item.Description;
            //_statisticsField.text;
            _close.onClick.AddListener(OnClose);
        }

        private void OnClose()
        {
            Destroy(gameObject);

        }
    }
}