using Core.Engine;
using Core.Weapon;
using Game.Player;
using Game.Player.Controllers;
using Game.Service;
using Game.UI;
using Game.Weapon;
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

        [SerializeField] private HUDHeartDisplay _heart;

        [SerializeField] private HUDWeaponSlot _mainWeaponSlot;
        [SerializeField] private HUDWeaponSlot _secondaryWeaponSlot;
        [SerializeField] private HUDGrenadeSlot _grenadeSlot;

        private PlayerHealth _health;
        private PlayerWeapons _weapon;
        private PlayerRigidbodyMovement _movement;

        private void Start()
        {
            _active = true;
            _health = Bootstrap.Resolve<PlayerService>().Player.GetComponent<PlayerHealth>();

            _heart.MaxHealth = 100;
            if (_health == null)
            {
                throw new UnityException("No Player Health in the scene");
            }

            _weapon = Bootstrap.Resolve<PlayerService>().Player.GetComponent<PlayerWeapons>();

            if (_weapon == null)
            {
                throw new UnityException("No Player Weapon in the scene");
            }

            _weapon.WeaponSlots[WeaponSlotType.MAIN].SlotWeaponAddedEvent += OnMainAddedWeapon;
            _weapon.WeaponSlots[WeaponSlotType.SECONDARY].SlotWeaponAddedEvent += OnSecondaryAddedWeapon;
            _weapon.WeaponSwapEvent += OnSwapWeapon;
            _weapon.WeaponGrenadeTypeChanged += OnSwapGrenade;
            _weapon.WeaponGrenadeStateEvent += OnGrenade;

            _movement = Bootstrap.Resolve<PlayerService>().Player.GetComponent<PlayerRigidbodyMovement>();
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
            if (type == WeaponSlotType.MAIN)
            {
                _mainWeaponSlot.Show(2f);
                _secondaryWeaponSlot.Hide();
            }

            if (type == WeaponSlotType.SECONDARY)
            {
                _secondaryWeaponSlot.Show(2f);
                _mainWeaponSlot.Hide();
            }
        }

        private void OnSecondaryAddedWeapon(PlayerWeaponSlot slot)
        {
            _secondaryWeaponSlot.Set(slot);
            _secondaryWeaponSlot.Show(5f);
        }

        private void OnMainAddedWeapon(PlayerWeaponSlot slot)
        {
            _mainWeaponSlot.Set(slot);
            _mainWeaponSlot.Show(5f);
        }

        private void LateUpdate()
        {
            _regenBar.value = _health.CurrentMaxRegenHealth / 100;
            _healthBar.value = _health.CurrentHealth / 100;
            _heart.Health = _health.CurrentHealth;
            bool hasWeapon = _weapon.WeaponEngine.Initialized;

            if (hasWeapon)
            {
                _currentAmmo.text = $"{_weapon.WeaponEngine.CurrentAmmo}";
                //Extraer del inventario;
                _inventoryAmmo.text = $"{Bootstrap.Resolve<InventoryService>().Instance.Ammunitions[_weapon.WeaponEngine.WeaponSettings.Ammo.Type]}";

                _ammoType.text = $"{_weapon.WeaponEngine.WeaponSettings.Ammo.Type.Name}";
            }

            _staminaBar.value = _movement.Stamina / _movement.MaxStamina;
        }
    }
}