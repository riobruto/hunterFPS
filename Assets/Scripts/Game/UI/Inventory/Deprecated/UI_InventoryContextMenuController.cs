using Game.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Inventory
{
    public class UI_InventoryContextMenuController : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Button _buttonEmpty;
        [SerializeField] private TMP_Text _buttonEmptyText;
        [SerializeField] private TMP_Text _itemName;
        [SerializeField] private Button _buttonDrop;
        [SerializeField] private Button _buttonDescription;
        public UnityAction FunctionButtonAction;
        public UnityAction DropButtonAction;
        public UI_InventoryItem SelectedItem { get; private set; }
        public bool Active { get; private set; }

        private void Start()
        {
            _buttonEmpty.onClick.AddListener(FunctionButtonAction);
            _buttonEmpty.onClick.AddListener(OnButtonClick);
            _buttonDrop.onClick.AddListener(DropButtonAction);
            _buttonDrop.onClick.AddListener(OnButtonClick);
            _buttonDescription.onClick.AddListener(OnDescriptionClick);
        }

        private void OnDescriptionClick()
        {
            CreateDescriptionMenu(SelectedItem);
            Close();
        }

        private void CreateDescriptionMenu(UI_InventoryItem selectedItem)
        {
            GameObject go = Instantiate(Resources.Load("UI/UI_Details") as GameObject);

            go.GetComponent<RectTransform>().transform.SetParent(transform);
            go.GetComponent<RectTransform>().anchoredPosition = new Vector2(-0, 0);
            go.GetComponent<UI_Details>().Initialize(selectedItem.ItemData);
            //esto es de vago nomas
            FindObjectOfType<UI_Cursor>().transform.SetAsLastSibling();
        }

        private void OnButtonClick()
        {
            Close();
        }

        public void Open(UI_InventoryItem item, Vector2 position)
        {
            _rect.position = position;
            _rect.gameObject.SetActive(true);
            _itemName.text = item.ItemData.Name;
            SelectedItem = item;
            Active = true;

            switch (item.ItemData)
            {
                case ConsumableItem:
                    _buttonEmpty.gameObject.SetActive(true);
                    _buttonEmptyText.text = (item.ItemData as ConsumableItem).ConsumeText;
                    _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 128);
                    _buttonDescription.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -64);
                    _buttonDrop.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -96);

                    break;

                case EquipableItem:
                    _buttonEmpty.gameObject.SetActive(true);
                    _buttonEmptyText.text = "Equip";
                    _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 128);
                    _buttonDescription.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -64);
                    _buttonDrop.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -96);

                    break;                                
            }
        }

        public void Close()
        {
            //Debug.Log("close");
            _rect.gameObject.SetActive(false);
            SelectedItem = null;
            Active = false;
        }

        private void OnDestroy()
        {
            _buttonEmpty.onClick.RemoveListener(FunctionButtonAction);
            _buttonDrop.onClick.RemoveListener(DropButtonAction);
        }
    }
}