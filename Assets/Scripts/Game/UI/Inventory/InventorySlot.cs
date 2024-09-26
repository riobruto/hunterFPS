
using Game.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Inventory
{
    public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private InventoryGrid _grid;
        private Image _background;

        private const int border_px = 6;
        private const int pad_px = 0;
        private const int slot_size_px_x = 85;
        private const int slot_size_px_y = 85;

        public int X;
        public int Y;

        public InventoryItem Item = null;

        public void Initialize(int x, int y, InventoryGrid grid)
        {
            X = x;
            Y = y;
            _grid = grid;
            _background = gameObject.AddComponent<Image>();
            _background.sprite = grid.BackgroundSprite;
            _background.rectTransform.SetParent(transform);
            _background.rectTransform.sizeDelta = new Vector2(slot_size_px_x, slot_size_px_y);
            _background.rectTransform.localScale = Vector3.one;
            _background.rectTransform.anchoredPosition = new Vector2((x * slot_size_px_x) + border_px + x * pad_px, (-y * slot_size_px_y) - border_px);
            _background.pixelsPerUnitMultiplier = 2;
            _background.type = Image.Type.Sliced;
            _background.color = new Color(1, 1, 1, .5f);
            _background.enabled = false;
        }

        public void Set(InventoryItem item)
        {
            _background.enabled = true;
            Item = item;
            /*Image image = transform.AddChild("ItemImage").gameObject.AddComponent<Image>();
            image.rectTransform.localScale = Vector3.one;
            image.sprite = item.Icon;
            image.rectTransform.sizeDelta = new Vector2(80, 80);*/
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            _grid.ReportItemClicked(this);

            Debug.Log($"Se apreto el sprite con {Item.name}");
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            _background.color = new Color(1, 1, 1, .5f);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            _background.color = new Color(1, 1, 1, 1f);
        }

        internal void Empty()
        {
            _background.enabled = false;
            Item = null;
            Destroy(transform.GetChild(0).gameObject);
        }
    }
}