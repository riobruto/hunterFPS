using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI.Inventory
{
    [RequireComponent(typeof(UI_ItemGrid))]
    public class UI_GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private UI_GridInventoryController _inventoryController;
        private UI_ItemGrid _grid;

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            _inventoryController.CurrentGrid = _grid;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            _inventoryController.CurrentGrid = null;
        }

        private void Awake()
        {
            _inventoryController = FindObjectOfType<UI_GridInventoryController>();
            _grid = GetComponent<UI_ItemGrid>();
        }


    }
}