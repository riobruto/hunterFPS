using Core.Save;
using Game.Inventory;
using UnityEngine;

namespace Core.Weapon
{
    public class AttachmentSettings : SaveableScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        public string Name { get => _name; }
        public string Description { get => _description; }
    }
}