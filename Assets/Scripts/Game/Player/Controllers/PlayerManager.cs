using Core.Configuration;
using Core.Engine;
using Game.Inventory;
using Game.Player.Movement;
using Game.Player.Weapon;
using Game.Service;
using Game.UI;
using Life.Controllers;
using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;

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
        private PlayerKick _kick;

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
            _kick = GetComponent<PlayerKick>();
            _kick.KickStartEvent += OnKickStart;
            _kick.KickFinishEvent += OnKickEnd;
            _kick.AllowKick = true;

            _weaponController.AllowInput = true;
            _inventoryController.AllowInput = true;

            InventoryService.Instance.ToggleInventoryEvent += OnInventoryOpen;
            _weaponController.WeaponAimEvent += OnAimWeapon;
            _weaponController.WeaponInstanceChangeEvent += OnWeaponInstanceChanged;
            _inventoryController.ItemBeginConsumingEvent += OnItemBeginConsume;
            _inventoryController.ItemFinishConsumeEvent += OnItemFinishConsume;
            _movementController.PlayerFallEvent += OnFall;
            _health.DeadEvent += OnDie;
            _train.PlayerEnterEvent += OnEnterTrain;
            _train.PlayerExitEvent += OnExitTrain;
            _lean.AllowLean = true;

            //UIService.CreateMessage(new MessageParameters("Este es un mensaje generado por el juego", 5, Color.white, new Color(0, 0, 0, .5f)));
            //UIService.CreateMessage(new MessageParameters("No puedes interactuar con este elemento", 5, Color.white, new Color(0, 0, 0, .5f)));
            //UIService.CreateMessage(new MessageParameters("Tu inventario esta lleno", 5, Color.white, new Color(0, 0, 0, .5f)));
            //UIService.CreateMessage(new MessageParameters("Puto el que lee XDDD JIJOLINES", 5, Color.white, new Color(0, 0, 0, .5f)));
            yield return null;
        }

        private void OnKickEnd()
        {
            _movementController.AllowSprint = true;
        }

        private void OnKickStart()
        {
            _movementController.AllowSprint = false;
        }

        private void OnFall(Vector3 start, Vector3 end)
        {
            float distance = Mathf.Abs(end.y - start.y);
            //todo: calcular bien daño por caida no seas roñoso
            if (distance > 5) _health.Hurt(distance * 5, Vector3.one);
        }

        private void OnExitTrain()
        {
            //_movementController.Controller.excludeLayers = 0;
            _inTrain = false;
            _movementController.AllowMovement = true;
            _interactionController.AllowInteraction = true;
            _weaponController.AllowInput = true;
            _weaponController.Draw();
            _kick.AllowKick = true;
        }

        private void OnEnterTrain()
        {
            _inTrain = true;
            _movementController.AllowMovement = false;
            _inventoryController.SetUIActive(false);
            _interactionController.AllowInteraction = false;
            _weaponController.AllowInput = false;
            _weaponController.Seathe();
            _kick.AllowKick = false;
        }

        private void OnWeaponInstanceChanged(PlayerWeaponInstance instance)
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
            _kick.AllowKick = !state;
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
            _kick.AllowKick = false;
            _movementController.Die();

            if (!_inventoryOpen)
            {
                _weaponController.Seathe();
            }
            StartCoroutine(RespawnSequence());
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
                _kick.AllowKick = false;
                return;
            }

            if (!_inventoryController.IsConsumingItem && !_inTrain)
            {
                _weaponController.Draw();
            }
            _kick.AllowKick = true;

            _movementController.AllowMovement = true;
            _movementController.AllowLookMovement = true;
            _lean.AllowLean = true;
            _interactionController.AllowInteraction = true;
        }

        private IEnumerator RespawnSequence()
        {
           
            yield return new WaitForSeconds(3);              
            //show text
            //allow click respawn
            FindObjectOfType<HUDElements>().ShowRespawnText(true);
            yield return new WaitUntil(() => Mouse.current.leftButton.wasPressedThisFrame);
            FindObjectOfType<HUDElements>().ShowRespawnText(false);           
            yield return new WaitForSeconds(2);
            Bootstrap.Resolve<PlayerService>().Respawn();
           
          
            yield break;
        }

        [ContextMenu("Restore")]
        public void RestorePlayer()
        {
            _inventoryController.SetUIActive(false);
            _movementController.AllowMovement = true;
            _movementController.AllowJump = true;
            _movementController.AllowCrouch = true;
            _movementController.AllowSprint = true;
            _movementController.AllowLookMovement = true;
            _weaponController.AllowInput = true;
            _interactionController.AllowInteraction = true;
            _inventoryController.AllowInput = true;
            _lean.AllowLean = true;
            _kick.AllowKick = true;
            _movementController.Restore();
            _health.Restore();
            _weaponController.Draw();
        
        }
    }
}