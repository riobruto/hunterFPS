using Game.Player.Sound;
using System.Collections;
using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(fileName = "New Ammo Item", menuName = "Inventory/AmmoItem")]
    public class AmmunitionItem : ScriptableObject
    {
        [Header("Ammo Info")]
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _image;
        [Header("Ammo properties")]
        [SerializeField] private float _damageToFlesh;
        [SerializeField] private float _damageToMetal;
        [SerializeField] private float _damageToWood;
        [SerializeField] private int _playerLimit;
        [SerializeField] private int _dropAmount;

        [SerializeField] private AudioClipGroup _shellImpact;


        public string Name { get => _name; }
        public string Description { get => _description; }
        public Sprite Image { get => _image; }
        public float DamageToFlesh { get => _damageToFlesh; }
        public float DamageToMetal { get => _damageToMetal; }
        public float DamageToWood { get => _damageToWood; }
        public int PlayerLimit { get => _playerLimit; }
        public int PickUpAmount { get => _dropAmount; }
        public AudioClipGroup ShellImpact { get => _shellImpact;  }

        //penetration and reflexion
    }
}