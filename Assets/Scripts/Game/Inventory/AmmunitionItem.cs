using System.Collections;
using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(fileName = "New Ammo Item", menuName = "Inventory/AmmoItem")]

    public class AmmunitionItem : InventoryItem
    {
        [Header("Ammo Info")]

        [SerializeField] private float Damage;
        //penetration and reflexion
    }
}