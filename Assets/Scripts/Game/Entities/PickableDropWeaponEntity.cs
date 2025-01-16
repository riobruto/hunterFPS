using Core.Engine;
using Core.Weapon;
using Game.Audio;
using Game.Player.Controllers;
using Game.Player.Sound;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public class PickableDropWeaponEntity : SimpleInteractable
    {
        [SerializeField] private WeaponSettings _weapon;
        [SerializeField] private AudioClipGroup _drop;
        private bool _taken = false;

        [SerializeField] private bool _isPlayerWeapon;

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
            if (_isPlayerWeapon)
            {
                if (Bootstrap.Resolve<PlayerService>().Player.GetComponent<PlayerWeapons>().TryGiveWeapon(_weapon, _weapon.Ammo.Size))
                {
                    InventoryService.Instance.TryGiveAmmo(_weapon.Ammo.Type, _weapon.Ammo.Type.PickUpAmount);
                    _taken = true;
                    InteractEvent?.Invoke();
                    Destroy(gameObject);
                    return true;
                }
            }
            

            if (InventoryService.Instance.TryGiveAmmo(_weapon.Ammo.Type, _weapon.Ammo.Type.PickUpAmount))
            {
                _taken = true;
                InteractEvent?.Invoke();
                Destroy(gameObject);
                return true;
            }
            else return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude > 1)
            {
                AudioToolService.PlayClipAtPoint(_drop.GetRandom(), transform.position, 1, AudioChannels.ENVIRONMENT, 5);
            }
        }
    }
}