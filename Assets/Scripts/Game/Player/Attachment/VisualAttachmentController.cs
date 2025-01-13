using Core.Weapon;
using Game.Service;
using System;

using UnityEngine;

namespace Game.Player.Attachments
{
    public class VisualAttachmentController : MonoBehaviour
    {
        [SerializeField] private VisualAttachment[] _attachments;
        private InventorySystem _inventory;

        private void Start()
        {
            _inventory = InventoryService.Instance;
            _inventory.AttachmentAddedEvent += OnAttachmentGained;

            foreach (VisualAttachment attachment in _attachments)
            {
                if (!_inventory.CurrentAttachments.Contains(attachment.Settings))
                {
                    attachment.Activate(false);
                }
            }
        }

        private void OnAttachmentGained(AttachmentSettings item)
        {
            foreach (VisualAttachment attachment in _attachments)
            {
                if (attachment.Settings == item)
                {
                    attachment.Activate(true);
                }
            }
        }
    }

    [Serializable]
    public class VisualAttachment
    {
        [SerializeField] private GameObject[] showGameObjects;
        [SerializeField] private GameObject[] hideGameObject;
        [SerializeField] private AttachmentSettings attachmentSettings;
        public AttachmentSettings Settings { get => attachmentSettings; }

        internal void Activate(bool v)
        {
            foreach (var attachment in showGameObjects)
            {
                attachment.SetActive(v);
            }
            foreach (var attachment in hideGameObject)
            {
                attachment.SetActive(!v);
            }
        }
    }
}