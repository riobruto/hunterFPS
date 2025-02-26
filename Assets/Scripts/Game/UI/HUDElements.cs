using Core.Engine;
using Core.Weapon;
using Game.Player;
using Game.Player.Controllers;
using Game.Service;
using Game.UI;
using Game.Weapon;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUDElements : MonoBehaviour
    {
        private bool _active;

        [SerializeField] private Slider _healthBar;
        [SerializeField] private Slider _regenBar;
        [SerializeField] private Slider _staminaBar;
        [SerializeField] private Slider _powerBar;

        [SerializeField] private TMP_Text _currentAmmo;
        [SerializeField] private TMP_Text _inventoryAmmo;
        [SerializeField] private TMP_Text _ammoType;
        [SerializeField] private TMP_Text _respawnText;

        [SerializeField] private HUDHeartDisplay _heart;
        [SerializeField] private HUDGrenadeSlot _grenadeSlot;        
        private PlayerHealth _health;
        private PlayerWeapons _weapon;
        private PlayerRigidbodyMovement _movement;
        [SerializeField] private HUDWeaponSlot[] _weaponSlots;
            
        private void Start()
        {
            _active = true;
            _health = Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerHealth>();

            _heart.MaxHealth = 100;
            if (_health == null)
            {
                throw new UnityException("No Player Health in the scene");
            }

            _weapon = Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerWeapons>();

            if (_weapon == null)
            {
                throw new UnityException("No Player Weapon in the scene");
            }

            foreach (PlayerWeaponSlot slot in _weapon.WeaponSlots.Values)
            {
                slot.SlotWeaponAddedEvent += OnWeaponAdded;
            }

            _weapon.WeaponSwapEvent += OnSwapWeapon;
            _weapon.WeaponGrenadeTypeChanged += OnSwapGrenade;
            _weapon.WeaponGrenadeStateEvent += OnGrenade;

            _movement = Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerRigidbodyMovement>();
        }

        private void OnGrenade(GrenadeType type, GrenadeState state)
        {
            _grenadeSlot.Set(type);
        }

        private void OnSwapGrenade(GrenadeType type)
        {
            _grenadeSlot.Set(type);
            _grenadeSlot.Show(5f);
        }

        private void OnSwapWeapon(WeaponSlotType type)
        {
            foreach (HUDWeaponSlot slot in _weaponSlots)
            {
                if (slot.Type == type) { slot.Show(2); }
                else slot.Hide();
            }
        }
        private void OnWeaponAdded(PlayerWeaponSlot slot)
        {
            foreach (HUDWeaponSlot hudslot in _weaponSlots)
            {
                if (hudslot.Type == slot.SlotType)
                {
                    hudslot.Set(slot);
                    //hudslot.Show();
                }
                else hudslot.Hide();
            }
        }

        private void LateUpdate()
        {
            _regenBar.value = _health.CurrentMaxRegenHealth / 100;
            _healthBar.value = _health.CurrentHealth / 100;
            _heart.Health = _health.CurrentHealth;
            bool hasWeapon = _weapon.WeaponEngine.Initialized;

            if (hasWeapon)
            {
                DoWeaponAmmo();
                DoAvaliableAmmo();
            }

            _staminaBar.value = _movement.Stamina / _movement.MaxStamina;
        }

        private void DoWeaponAmmo()
        {
            string weaponAmmo = _weapon.WeaponEngine.CurrentAmmo > _weapon.WeaponEngine.MaxAmmo ? $"{_weapon.WeaponEngine.CurrentAmmo - 1}+1 " : $"{_weapon.WeaponEngine.CurrentAmmo}";
            //red effect when low ammo
            if (_weapon.WeaponEngine.CurrentAmmo < _weapon.WeaponEngine.MaxAmmo / 3)
            {
                weaponAmmo = $"<color=red>{weaponAmmo}</color>";
            }
            _currentAmmo.text = weaponAmmo;
        }

        private void DoAvaliableAmmo()
        {
            int avaliabeAmmo = InventoryService.Instance.Ammunitions[_weapon.WeaponEngine.WeaponSettings.Ammo.Type];
            string avaliableAmmoString = $"{avaliabeAmmo}";

            if (avaliabeAmmo <= _weapon.WeaponEngine.MaxAmmo)
            {
                avaliableAmmoString = $"<color=red>{avaliableAmmoString}</color>";
            }
            //Extraer del inventario;
            _inventoryAmmo.text = avaliableAmmoString;
            _ammoType.text = $"{_weapon.WeaponEngine.WeaponSettings.Ammo.Type.Name}";
        }

        internal void ShowRespawnText(bool v) => _respawnText.gameObject.SetActive(v);
       
    }
}