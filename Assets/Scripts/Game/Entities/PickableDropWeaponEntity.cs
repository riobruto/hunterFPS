using Core.Engine;
using Core.Weapon;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public class PickableDropWeaponEntity : SimpleInteractable
    {
        [SerializeField] private WeaponSettings _weapon;

        private bool _taken = false;

        public override bool CanInteract => !Taken;
        public override bool Taken => _taken;

        public override event InteractableDelegate InteractEvent;

        private void Start()
        {
            gameObject.layer = 30;
            SetLayerAllChildren(this.transform, 30);
        }

        private void SetLayerAllChildren(Transform root, int layer)
        {
            var children = root.GetComponentsInChildren<Transform>(includeInactive: true);

            foreach (var child in children)
            {
                child.gameObject.layer = layer;
            }
        }

        internal void SetAsset(WeaponSettings weapon)
        {
            _weapon = weapon;
        }

        public override bool Interact()
        {
            if (Bootstrap.Resolve<InventoryService>().Instance.TryGiveAmmo(_weapon.Ammo.Type, _weapon.Ammo.Type.PickUpAmount))
            {
                _taken = true;
                InteractEvent?.Invoke();
                Destroy(gameObject);
                return true;
            }
            else return false;
        }
    }
}