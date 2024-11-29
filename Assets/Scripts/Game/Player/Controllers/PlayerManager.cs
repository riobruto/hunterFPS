using Core.Configuration;
using Core.Engine;
using Game.Inventory;
using Game.Player.Movement;
using Game.Player.Weapon;
using Game.Service;
using System.Collections;
using UnityEngine;

namespace Game.Player.Controllers
{
    public class PlayerManager : MonoBehaviour
    {
        private PlayerWeapons _weaponController;
        private PlayerRigidbodyMovement _movementController;
        private PlayerInventoryController _inventoryController;
        private PlayerInteractionController _interactionController;
        private PlayerConfiguration.PlayerControlSettings _settings;
        private PlayerLeanMovement _lean;

        private PlayerTrainController _train;
        private PlayerHealth _health;
        private bool _inventoryOpen;
        private bool _subscribedtoWeaponState;
        private bool _inTrain;

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            _weaponController = GetComponent<PlayerWeapons>();
            _movementController = GetComponent<PlayerRigidbodyMovement>();
            _inventoryController = GetComponent<PlayerInventoryController>();
            _interactionController = GetComponent<PlayerInteractionController>();
            _lean = GetComponent<PlayerLeanMovement>();
            _health = GetComponent<PlayerHealth>();
            _settings = Bootstrap.Resolve<GameSettings>().PlayerConfiguration.Settings;
            _train = GetComponent<PlayerTrainController>();

            _weaponController.AllowInput = true;
            _inventoryController.AllowInput = true;

            Bootstrap.Resolve<InventoryService>().Instance.ToggleInventoryEvent += OnInventoryOpen;
            _weaponController.WeaponAimEvent += OnAimWeapon;
            _weaponController.WeaponInstanceChangeEvent += InstanceChanged;
            _inventoryController.ItemBeginConsumingEvent += OnItemBeginConsume;
            _inventoryController.ItemFinishConsumeEvent += OnItemFinishConsume;
            _movementController.PlayerFallEvent += OnFall;
            _health.DeadEvent += OnDie;
            _train.PlayerEnterEvent += OnEnterTrain;
            _train.PlayerExitEvent += OnExitTrain;

            _lean.AllowLean = true;

            UIService.CreateMessage(new MessageParameters("Este es un mensaje generado por el juego", 5, Color.white, new Color(0, 0, 0, .5f)));
            UIService.CreateMessage(new MessageParameters("No puedes interactuar con este elemento", 5, Color.white, new Color(0, 0, 0, .5f)));
            UIService.CreateMessage(new MessageParameters("Tu inventario esta lleno", 5, Color.white, new Color(0, 0, 0, .5f)));
            UIService.CreateMessage(new MessageParameters("Puto el que lee XDDD JIJOLINES", 5, Color.white, new Color(0, 0, 0, .5f)));

            yield return null;
        }

        private void OnFall(Vector3 start, Vector3 end)
        {
            float distance = Mathf.Abs(end.y - start.y);
            if (distance > 10) _health.Hurt(distance);
        }

        private void OnExitTrain()
        {
            //_movementController.Controller.excludeLayers = 0;
            _inTrain = false;
            _movementController.AllowMovement = true;

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
            _movementController.AllowMovement = false;

            //_movementController.Controller.enabled = false;
            //_movementController.LookMovement.AllowHorizontalLook = false;
            //_movementController.LookMovement.AllowVerticalLook = false;
            _inventoryController.SetUIActive(false);
            _interactionController.AllowInteraction = false;
            _weaponController.AllowInput = false;
            _weaponController.Seathe();
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
        }

        private void OnItemBeginConsume(ConsumableItem item)
        {
            _movementController.AllowSprint = false;
        }

        private void OnItemFinishConsume(ConsumableItem item)
        {
            _movementController.AllowSprint = true;

            if (!_inventoryOpen)
            {
                _weaponController.Draw();
            }
        }

        private void OnAimWeapon(bool state)
        {
            _movementController.Sensitivity = state ? _settings.AimSensitivity : _settings.NormalSensitivity;
        }

        private void OnDie()
        {
            _inventoryController.SetUIActive(false);
            _movementController.AllowMovement = false;
            _movementController.AllowJump = false;
            _movementController.AllowCrouch = false;
            _movementController.AllowSprint = false;
            _movementController.AllowLookMovement = false;

            _weaponController.AllowInput = false;
            _interactionController.AllowInteraction = false;
            _inventoryController.AllowInput = false;
            _lean.AllowLean = false;

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
                _movementController.AllowMovement = false;
                _movementController.AllowLookMovement = false;
                _lean.AllowLean = false;

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

            _movementController.AllowMovement = true;
            _movementController.AllowLookMovement = true;
            _lean.AllowLean = true;
            _interactionController.AllowInteraction = true;
        }
    }
}