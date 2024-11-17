using Core.Configuration;
using Core.Engine;
using Game.Inventory;
using Game.Player.Weapon;
using Game.Service;
using System.Collections;
using UnityEngine;

namespace Game.Player.Controllers
{
    public class PlayerManager : MonoBehaviour
    {
        private PlayerWeapons _weaponController;
        private PlayerMovementController _movementController;
        private PlayerInventoryController _inventoryController;
        private PlayerInteractionController _interactionController;
        private PlayerConfiguration.PlayerControlSettings _settings;
        private PlayerAbilitiesController _abilities;
        private PlayerTrainController _train;
        private PlayerHealth _health;
        private bool _inventoryOpen;
        private bool _subscribedtoWeaponState;
        private bool _inTrain;

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            _weaponController = GetComponent<PlayerWeapons>();
            _movementController = GetComponent<PlayerMovementController>();
            _inventoryController = GetComponent<PlayerInventoryController>();
            _interactionController = GetComponent<PlayerInteractionController>();
            _abilities = GetComponent<PlayerAbilitiesController>();
            _health = GetComponent<PlayerHealth>();
            _settings = Bootstrap.Resolve<GameSettings>().PlayerConfiguration.Settings;
            _train = GetComponent<PlayerTrainController>();

            _weaponController.AllowInput = true;
            _movementController.VaultMovement.AllowVault = false;
            _inventoryController.AllowInput = true;

            Bootstrap.Resolve<InventoryService>().Instance.ToggleInventoryEvent += OnInventoryOpen;
            _weaponController.WeaponAimEvent += OnAimWeapon;
            _weaponController.WeaponInstanceChangeEvent += InstanceChanged;
            _inventoryController.ItemBeginConsumingEvent += OnItemBeginConsume;
            _inventoryController.ItemFinishConsumeEvent += OnItemFinishConsume;
            _abilities.OpenRadialEvent += OnRadialOpen;
            _health.DeadEvent += OnDie;
            _train.PlayerEnterEvent += OnEnterTrain;
            _train.PlayerExitEvent += OnExitTrain;

            UIService.CreateMessage(new("Este es un mensaje generado por el juego", 5, Color.white, new Color(0, 0, 0, .5f)));
            UIService.CreateMessage(new("No puedes interactuar con este elemento", 5, Color.white, new Color(0, 0, 0, .5f)));
            UIService.CreateMessage(new("Tu inventario esta lleno", 5, Color.white, new Color(0, 0, 0, .5f)));

            yield return null;
        }

        private void OnExitTrain()
        {
            //_movementController.Controller.excludeLayers = 0;
            _inTrain = false;
            _movementController.GroundMovement.enabled = true;
            _movementController.AirMovement.enabled = true;
            //_movementController.LookMovement.AllowHorizontalLook = true;
            //_movementController.LookMovement.AllowVerticalLook = true;
            //_inventoryController.SetUIActive(false);
            _interactionController.AllowInteraction = true;
            _weaponController.AllowInput = true;
            _weaponController.Draw();
        }

        private void OnEnterTrain()
        {
            //_movementController.Controller.excludeLayers = 10;
            _inTrain = true;
            _movementController.GroundMovement.enabled = false;
            _movementController.AirMovement.enabled = false;
            //_movementController.Controller.enabled = false;
            //_movementController.LookMovement.AllowHorizontalLook = false;
            //_movementController.LookMovement.AllowVerticalLook = false;
            _inventoryController.SetUIActive(false);
            _interactionController.AllowInteraction = false;
            _weaponController.AllowInput = false;
            _weaponController.Seathe();
        }

        private void OnRadialOpen(bool state)
        {
            if (state)
            {
                _movementController.LookMovement.AllowHorizontalLook = false;
                _movementController.LookMovement.AllowVerticalLook = false;
                _weaponController.AllowInput = false;
                return;
            }
            _movementController.LookMovement.AllowHorizontalLook = true;
            _movementController.LookMovement.AllowVerticalLook = true;
            _weaponController.AllowInput = true;
        }

        private void InstanceChanged(PlayerWeaponInstance instance)
        {
            if (!_subscribedtoWeaponState)
            {
                _weaponController.WeaponEngine.WeaponChangedState += OnWeaponChangeState;
                _subscribedtoWeaponState = true;
            }
        }

        private void OnWeaponChangeState(object sender, WeaponStateEventArgs e)
        {
            if (e.State == Core.Weapon.WeaponState.BEGIN_SHOOTING && _movementController.IsFlying)
            {
                _movementController.AirMovement.Impulse(-transform.forward * Mathf.Abs(e.Sender.WeaponSettings.RecoilKick.x));
            }
        }

        private void OnItemBeginConsume(ConsumableItem item)
        {
            _movementController.GroundMovement.AllowSprint = false;
        }

        private void OnItemFinishConsume(ConsumableItem item)
        {
            _movementController.GroundMovement.AllowSprint = true;

            if (!_inventoryOpen)
            {
                _weaponController.Draw();
            }
        }

        private void OnAimWeapon(bool state)
        {
            _movementController.LookMovement.Sensitivity = state ? _settings.AimSensitivity : _settings.NormalSensitivity;
        }

        private void OnDie()
        {
            _inventoryController.SetUIActive(false);
            _movementController.SetMovementFlags(false);
            _weaponController.AllowInput = false;
            _interactionController.AllowInteraction = false;
            _inventoryController.AllowInput = false;

            if (!_inventoryOpen)
            {
                _weaponController.Seathe();
            }
        }

        private void OnInventoryOpen(bool state)
        {
            if (_health.Dead) return;

            _inventoryOpen = state;

            if (state)
            {
                _abilities.CanOpenRadial = false;
                _movementController.LookMovement.AllowHorizontalLook = false;
                _movementController.LookMovement.AllowVerticalLook = false;
                _movementController.SetAllowGroundMovement(false);

                if (!_inTrain)
                {
                    _interactionController.AllowInteraction = false;
                    _weaponController.Seathe();
                }
                return;
            }

            if (!_inventoryController.IsConsumingItem && !_inTrain)
            {
                _weaponController.Draw();
            }

            _abilities.CanOpenRadial = true;

            _movementController.LookMovement.AllowHorizontalLook = true;
            _movementController.LookMovement.AllowVerticalLook = true;
            _movementController.SetAllowGroundMovement(true);
            _interactionController.AllowInteraction = true;
        }
    }
}