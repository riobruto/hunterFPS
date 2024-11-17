using Core.Engine;
using Game.Inventory;
using Game.Service;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.Player.Controllers
{
    public class PlayerInventoryController : MonoBehaviour
    {
        private InventorySystem _system;

        private bool _openRequest;
        private bool _canConsumeItem => !_isConsumingItem;

        public bool IsConsumingItem { get => _isConsumingItem; }

        private bool _isConsumingItem;
        private bool _hasGasmaskOn;

        private PlayerHealth _health;
        private PlayerMovementController _manager;

        public event UnityAction<ConsumableItem> ItemBeginConsumingEvent;

        public event UnityAction<ConsumableItem> ItemFinishConsumeEvent;

        public bool AllowInput;

        private void Start()
        {
            _system = Bootstrap.Resolve<InventoryService>().Instance;
            _system.UseConsumableEvent += OnConsumeItem;
            _health = GetComponent<PlayerHealth>();
            _manager = GetComponent<PlayerMovementController>();
        }

        private void OnConsumeItem(ConsumableItem item)
        {
            if (!_canConsumeItem) return;

            if (item.CanConsumeWithMask == _hasGasmaskOn)
            {
                StartCoroutine(IConsumeItem(item));
                _system.TryRemoveItem(item);
                return;
            }

            Debug.Log("GasMaskOn");
        }

        private IEnumerator IConsumeItem(ConsumableItem item)
        {
            _isConsumingItem = true;
            ItemBeginConsumingEvent?.Invoke(item);
            yield return new WaitForSeconds(5);
            _isConsumingItem = false;
            ItemFinishConsumeEvent?.Invoke(item);
            StartCoroutine(ApplyHealthModifiers(item.Properties));
            StartCoroutine(ApplyStaminaModifiers(item.Properties));
        }

        private IEnumerator ApplyHealthModifiers(ConsumableProperties properties)
        {
            float time = 0;

            while (time < properties.Health.Duration)
            {
                _health.Heal(properties.Health.AmountOverTime);
                time += Time.deltaTime;
                yield return null;
            }

            yield return null;
        }

        private IEnumerator ApplyStaminaModifiers(ConsumableProperties properties)
        {
            float time = 0;
            while (time < properties.Stamina.Duration)
            {
                _manager.Stamina += properties.Stamina.AmountOverTime;
                time += Time.deltaTime;
                yield return null;
            }

            yield return null;
        }

        public void SetUIActive(bool active)
        {
            if (active)
            {
                _system.ShowInventoryUI();
                return;
            }
            _system.HideInventoryUI();
        }

        private void OnInventory(InputValue value)
        {
            if (!AllowInput) return;

            //HACK: con caquita, CON CAQUITA

            if (GetComponent<PlayerWeapons>().WeaponEngine != null)
            {
                if (GetComponent<PlayerWeapons>().WeaponEngine.BoltOpen)
                {
                    Debug.Log("Can Show UI, Bolt is Open");
                    return;
                }
                if (GetComponent<PlayerWeapons>().WeaponEngine.IsReloading)
                {
                    Debug.Log("Can Show UI, Weapon Is Reloading");
                    return;
                }
            }

            if (_manager.IsFlying)
            {
                Debug.Log("Can Show UI, Player Is Flying");
                return;
            }

            _openRequest = !_openRequest;

            if (_openRequest)
            {
                _system.ShowInventoryUI();
            }
            if (!_openRequest)
            {
                _system.HideInventoryUI();
            }
        }
    }
}