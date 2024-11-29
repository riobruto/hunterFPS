using Core.Weapon;
using Game.Player.Controllers;
using UnityEngine;

namespace Game.Entities
{
    public class PickableWeaponEntity : SimpleInteractable
    {
        [SerializeField] private WeaponSettings _weapon;
        [SerializeField] private int _currentAmmo = 0;
        private bool _taken = false;

        public override bool CanInteract => !Taken;

        public override bool Taken => _taken;

        public override event InteractableDelegate InteractEvent;

        private void Start()
        {
            gameObject.layer = 30;
            SetLayerAllChildren(this.transform, 30);
            _currentAmmo = _weapon.Ammo.Size;
        }

        private void SetLayerAllChildren(Transform root, int layer)
        {
            var children = root.GetComponentsInChildren<Transform>(includeInactive: true);

            foreach (var child in children)
            {
                child.gameObject.layer = layer;
            }
        }

        private bool GiveItem()
        {
            if (FindObjectOfType<PlayerWeapons>().TryGiveWeapon(_weapon, _currentAmmo))
            {
                _taken = true;
                InteractEvent?.Invoke();
                Destroy(gameObject);
                return true;
            }
            return false;
        }

        internal void SetAsset(WeaponSettings weapon)
        {
            _weapon = weapon;
        }

        public override bool Interact()
        {
            return GiveItem();
        }
    }
}