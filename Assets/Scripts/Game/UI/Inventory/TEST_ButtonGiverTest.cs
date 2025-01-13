using Core.Engine;
using Game.Inventory;
using Game.Service;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Inventory
{
    public class TEST_ButtonGiverTest : MonoBehaviour
    {
        [SerializeField] private InventoryItem[] _item;

        [SerializeField] private Button _button;

        private void Start()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (InventoryService.Instance.TryAddItem(_item[Random.Range(0, _item.Length)]))
            {
                Debug.Log("Item Given");
                return;
            }
            Debug.Log("Item Failed, Probably Full Inventory");
        }
    }
}