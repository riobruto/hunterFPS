using Core.Engine;
using Game.Inventory;
using Game.Service;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.Player.Controllers
{
    public class PlayerModifier
    {
        public PlayerModifier(ConsumableItem item)

        {
            TimeSinceAdded = Time.time;
            Duration = item.Properties.DurationInMinutes * 60;
            HealthRecover = item.Properties.HealthRecoverAmount;
            StaminaResistance = item.Properties.StaminaResistanceAmount;
            DamageResistance = item.Properties.DamageResistanceAmount;
        }

        public float TimeSinceAdded { get; private set; }
        public float Duration { get; private set; }
        public float HealthRecover { get; private set; }
        public float StaminaResistance { get; private set; }
        public float DamageResistance { get; private set; }
    }

    public class PlayerInventoryController : MonoBehaviour
    {
        private InventorySystem _system;

        private bool _openRequest;
        private bool _canConsumeItem => !_isConsumingItem;
        private bool _hasGasmaskOn;
        private bool _isConsumingItem;

        private PlayerHealth _health;
        private PlayerRigidbodyMovement _movement;

        public event UnityAction<ConsumableItem> ItemBeginConsumingEvent;

        public event UnityAction<ConsumableItem> ItemFinishConsumeEvent;

        public bool AllowInput;
        public bool IsConsumingItem { get => _isConsumingItem; }
        private List<PlayerModifier> _activeModifiers = new List<PlayerModifier>();

        private void Start()
        {
            _system = Bootstrap.Resolve<InventoryService>().Instance;
            _system.UseConsumableEvent += OnConsumeItem;
            _health = GetComponent<PlayerHealth>();
            _movement = GetComponent<PlayerRigidbodyMovement>();
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
            yield return new WaitForSeconds(item.Properties.ConsumeTimeInSeconds);
            _health.Heal(item.Properties.HealthRecoverAmount);
            _activeModifiers.Add(new(item));
            _isConsumingItem = false;
            ItemFinishConsumeEvent?.Invoke(item);
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

        private void Update()
        {
            if (_activeModifiers.Count == 0) return;

            UpdatePlayerModifiers();
        }

        public void UpdatePlayerModifiers()
        {
            CheckOutdatedModifiers();

            float damageResistance = 0;
            float staminaResistance = 0;

            foreach (PlayerModifier modifier in _activeModifiers)
            {
                staminaResistance += Mathf.Clamp01(modifier.StaminaResistance);
                damageResistance += Mathf.Clamp01(modifier.DamageResistance);
            }
            Debug.Log($"SR:{staminaResistance}; DR:{damageResistance}");
            _movement.StaminaResistance = staminaResistance;
            _health.SetDamageResistanceModifier(damageResistance);
        }

        private void CheckOutdatedModifiers() => _activeModifiers = _activeModifiers.Where(x => Time.time - x.TimeSinceAdded < x.Duration).ToList();

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