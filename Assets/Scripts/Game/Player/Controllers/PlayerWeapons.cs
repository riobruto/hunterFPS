using Core.Engine;
using Core.Weapon;
using Game.Player.Weapon;
using Game.Player.Weapon.Engines;
using Game.Service;
using Game.Weapon;
using Nomnom.RaycastVisualization;
using Player.Weapon.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.Player.Controllers
{
    public delegate void PlayerWeaponAimDelegate(bool state);

    public delegate void PlayerWeaponObstructedDelegate(bool state);

    public delegate void PlayerWeaponDrawDelegate(bool state);

    public delegate void PlayerWeaponSwapDelegate(WeaponSlotType type);

    public delegate void PlayerWeaponInstanceDelegate(PlayerWeaponInstance instance);

    public delegate void PlayerWeaponGrenadeState(GrenadeType type, GrenadeState state);

    public class PlayerWeapons : MonoBehaviour
    {
        #region PublicFields

        public event PlayerWeaponObstructedDelegate WeaponObstructedEvent;

        public event PlayerWeaponAimDelegate WeaponAimEvent;

        public event PlayerWeaponDrawDelegate WeaponDrawEvent;

        public event PlayerWeaponSwapDelegate WeaponSwapEvent;

        public event PlayerWeaponInstanceDelegate WeaponInstanceChangeEvent;

        public event PlayerWeaponGrenadeState WeaponGrenadeStateEvent;

        public event UnityAction<GrenadeType> WeaponGrenadeTypeChanged;

        public Vector2 MouseDelta => Mouse.current.delta.value * Time.deltaTime * 10f;
        public Vector2 MouseDirection => _mouseRayDirection;
        public Transform WeaponVisualElementHolder { get => _weaponVisualTransform; }
        public IWeapon WeaponEngine { get => _weaponEngine; }
        public Dictionary<WeaponSlotType, PlayerWeaponSlot> WeaponSlots => _weaponSlots;

        public bool AllowInput { get; set; }

        #endregion PublicFields

        [Header("Editor References")]
        [SerializeField] private Transform _head;

        [SerializeField] private Transform _weaponVisualTransform;
        [SerializeField] private bool _areWeaponSlotsAcummulative;

        private bool _aimInput;

        private Vector2 _mouseRayDirection;

        private bool _isObstructed;
        private bool _lastCheckForwardObstruction;
        private bool _isChangingSlot;
        private bool _isThrowingGranade;

        private bool _canUseWeapon => !_isChangingSlot && _weaponEngine != null && !_isThrowingGranade && AllowInput;
        private bool _canAim => !_playerMovementController.IsSprinting && !_playerMovementController.IsVaulting && !_playerMovementController.IsFalling && !_playerMovementController.IsFlying && !_weaponEngine.BoltOpen && !_weaponEngine.IsReloading && !_isThrowingGranade;
        private bool _isAiming => _aimInput && _canAim;
        private bool _lastIsAiming;

        private PlayerMovementController _playerMovementController;
        private bool _canChangeWeapons => !_weaponEngine.IsReloading && !_weaponEngine.IsShooting && !_isChangingSlot && !_weaponEngine.BoltOpen && !_isThrowingGranade;
        private bool _canThrowGrenade => !_weaponEngine.IsReloading && !_weaponEngine.IsShooting && !_isChangingSlot && !_weaponEngine.BoltOpen && !_isThrowingGranade;

        private IWeapon _weaponEngine;
        private IWeapon _gunWeaponEngine;
        private PlayerWeaponInstance _currentWeaponInstance;
        private Dictionary<WeaponSlotType, PlayerWeaponSlot> _weaponSlots;
        private InventorySystem _inventory;

        private void Start()
        {
            _playerMovementController = GetComponent<PlayerMovementController>();
            if (_weaponVisualTransform == null) { Debug.LogException(new UnityException("There is no weapon holder transform attached")); return; }

            CreateWeaponEngines();
            CreateWeaponSlots();

            foreach (IObserverFromPlayerWeapon observer in gameObject.GetComponentsInChildren<IObserverFromPlayerWeapon>())
            {
                observer.Initalize(this);
            }

            _inventory = Bootstrap.Resolve<InventoryService>().Instance;
        }

        private void Update()
        {
            if (!_weaponEngine.Initialized) return;

            ManageObstruction();
            ManageAim();

            if (!_canUseWeapon)
            {
                _weaponEngine.SetMovementDelta(Vector2.zero);
                return;
            }

            if (AllowInput)
            {
                _mouseRayDirection += Mouse.current.delta.value * Time.deltaTime * 10f;
                _mouseRayDirection = Vector2.ClampMagnitude(_mouseRayDirection, _isAiming ? .1f : 10f);
            }
            else _mouseRayDirection = Vector2.zero;

            _weaponEngine.SetMovementDelta(_mouseRayDirection);
            _weaponEngine.RayNoise = Noise() * (_isAiming ? .05f : 1);
        }

        private void CreateWeaponSlots()
        {
            _weaponSlots = new Dictionary<WeaponSlotType, PlayerWeaponSlot>();

            foreach (WeaponSlotType type in Enum.GetValues(typeof(WeaponSlotType)))
            {
                _weaponSlots.Add(type, new PlayerWeaponSlot(type, _areWeaponSlotsAcummulative));
            }
        }

        private void CreateWeaponEngines()

        {
            _gunWeaponEngine = _head.gameObject.AddComponent<WeaponEngine>();
            _weaponEngine = _gunWeaponEngine;
            _weaponEngine.IsOwnedByPlayer = true;
        }

        private void ManageAim()
        {
            if (_lastIsAiming != _isAiming)
            {
                WeaponAimEvent?.Invoke(_isAiming);
                _lastIsAiming = _isAiming;
            }
        }

        private void ManageObstruction()
        {
            if (_lastCheckForwardObstruction != CheckForwardObstruction())
            {
                _lastCheckForwardObstruction = CheckForwardObstruction();

                if (CheckForwardObstruction())
                {
                    WeaponObstructedEvent?.Invoke(true);
                    _isObstructed = true;
                    return;
                }

                WeaponObstructedEvent?.Invoke(false);
                _isObstructed = false;
            }
        }

        private void OnGUI()
        {
            GUILayout.Label($"Mouse: {_mouseRayDirection}");

            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                foreach (WeaponSlotType type in Enum.GetValues(typeof(WeaponSlotType)))
                {
                    if (!_weaponSlots.TryGetValue(type, out PlayerWeaponSlot slot)) continue;

                    if (slot.WeaponInstances.Count == 0) continue;

                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUI.color = Color.blue;
                        GUILayout.Label($"{type}");

                        foreach (PlayerWeaponInstance weaponInstance in slot.WeaponInstances)
                        {
                            if (weaponInstance == _currentWeaponInstance)
                            {
                                GUI.color = Color.red;
                                using (new GUILayout.VerticalScope(GUI.skin.box))
                                {
                                    GUILayout.Label($"{weaponInstance.Settings.name} with {weaponInstance.CurrentAmmo} rounds");
                                }
                            }
                            else
                            {
                                GUI.color = Color.white;
                                using (new GUILayout.VerticalScope(GUI.skin.box))
                                {
                                    GUILayout.Label($"{weaponInstance.Settings.name} with {weaponInstance.CurrentAmmo} rounds");
                                }
                            }
                        }
                    }
                }
            }
        }

        #region Input

        private void OnGrenade(InputValue value)
        {
            if (!AllowInput) return;

            if (value.isPressed)
            {
                if (_weaponEngine != null)
                {
                    if (!_canThrowGrenade) return;
                }

                if (!_isThrowingGranade) StartCoroutine(ThrowGrenade());
            }
        }

        private void OnAim(InputValue value)
        {
            if (!_canUseWeapon) return;
            _aimInput = value.isPressed;
        }

        private void OnBolt(InputValue value)
        {
            if (!_canUseWeapon) return;

            if (_weaponEngine.BoltOpen)
            {
                _weaponEngine.CloseBolt();
                return;
            }
            _weaponEngine.OpenBolt();
        }

        private void OnFire(InputValue value)
        {
            if (!_canUseWeapon) { return; }

            if (value.isPressed)
            {
                if (_weaponEngine.BoltOpen)
                {
                    if (_weaponEngine.WeaponSettings.Reload.FastReloadOnEmpty && _weaponEngine.CurrentAmmo == 0)
                    {
                        if (Bootstrap.Resolve<InventoryService>().Instance.Ammunitions[_weaponEngine.WeaponSettings.Ammo.Type] > _weaponEngine.WeaponSettings.Ammo.Size)
                        {
                            if (_weaponEngine.Insert()) { Bootstrap.Resolve<InventoryService>().Instance.Ammunitions[_weaponEngine.WeaponSettings.Ammo.Type] -= _weaponEngine.WeaponSettings.Ammo.Size; }
                            return;
                        }
                    }

                    if (Bootstrap.Resolve<InventoryService>().Instance.Ammunitions[_weaponEngine.WeaponSettings.Ammo.Type] > 0)
                    {
                        if (_weaponEngine.Insert()) { Bootstrap.Resolve<InventoryService>().Instance.Ammunitions[_weaponEngine.WeaponSettings.Ammo.Type] -= 1; }
                    }
                    return;
                }

                if (_playerMovementController.IsSprinting) return;
                if (_isObstructed) return;

                _weaponEngine.Fire();
            }

            if (!value.isPressed)
            {
                _weaponEngine.ReleaseFire();
            }

            //else the fire was released
        }

        private void OnReload()
        {
            if (!AllowInput) return;
            if (!_canUseWeapon) return;
            if (_weaponEngine.WeaponSettings == null) return;

            if (_weaponEngine.BoltOpen)
            {
                _weaponEngine.CloseBolt();
                return;
            }

            if (_weaponEngine.WeaponSettings.FireModes == WeaponFireModes.BOLT && _weaponEngine.CurrentAmmo > 0)
            {
                _weaponEngine.Reload(0);
                return;
            }

            if (_weaponEngine.IsReloading) return;
            if (_weaponEngine.CurrentAmmo >= _weaponEngine.MaxAmmo) return;
            if (_inventory.Ammunitions[_weaponEngine.WeaponSettings.Ammo.Type] > 0)
            {
                StartCoroutine(ManageReload());
            }
        }

        private IEnumerator ManageReload()
        {
            int remainingAmmo = Mathf.Clamp(_weaponEngine.CurrentAmmo, 0, _weaponEngine.MaxAmmo); ;
            int reloadAmmo = _inventory.Ammunitions[_weaponEngine.WeaponSettings.Ammo.Type];
            int reloadAmount = Mathf.Clamp(reloadAmmo, 0, _weaponEngine.MaxAmmo);
            _weaponEngine.Reload(reloadAmount);
            _inventory.Ammunitions[_weaponEngine.WeaponSettings.Ammo.Type] -= reloadAmount - remainingAmmo;

            yield return null;
        }

        private void OnMeeleeSlot(InputValue value) => ChangeWeaponBySlot(WeaponSlotType.MEELEE);

        private void OnPrimaryWeaponSlot(InputValue value) => ChangeWeaponBySlot(WeaponSlotType.MAIN);

        private void OnSecondaryWeaponSlot(InputValue value) => ChangeWeaponBySlot(WeaponSlotType.SECONDARY);

        private void OnGrenadeSlot(InputValue value)
        {
            if ((int)_currentType + 1 > Enum.GetValues(typeof(GrenadeType)).Length - 1)
            {
                _currentType = 0;
            }
            else _currentType++;

            WeaponGrenadeTypeChanged?.Invoke(_currentType);
        }

        #endregion Input

        public void Seathe()
        {
            if (_currentWeaponInstance == null) return;
            _aimInput = false;
            _weaponEngine.ReleaseFire();
            _weaponEngine.Deactivate();
            WeaponDrawEvent?.Invoke(false);
        }

        public void Draw()
        {
            if (_currentWeaponInstance == null) return;
            _aimInput = false;
            _weaponEngine.ReleaseFire();
            _weaponEngine.Activate();
            WeaponDrawEvent?.Invoke(true);
        }

        [SerializeField] private GameObject _HEGrenade;
        [SerializeField] private GameObject _GasGrenade;
        private bool _hasGrenadeInInventory => Bootstrap.Resolve<InventoryService>().Instance.Grenades[_currentType] > 0;

        private GrenadeType _currentType = GrenadeType.HE;

        private IEnumerator ThrowGrenade()
        {
            GrenadeType cachedType = _currentType;
            if (!_hasGrenadeInInventory) yield break;
            _isThrowingGranade = true;
            Seathe();
            yield return new WaitForSeconds(.5f);
            WeaponGrenadeStateEvent?.Invoke(cachedType, GrenadeState.CHARGE);
            yield return new WaitForSeconds(1f);
            Bootstrap.Resolve<InventoryService>().Instance.Grenades[cachedType] -= 1;
            InstanceGrenade();

            WeaponGrenadeStateEvent?.Invoke(cachedType, GrenadeState.THROW);
            yield return new WaitForSeconds(.5f);
            Draw();
            _isThrowingGranade = false;
            yield return null;
        }

        private GameObject GetGranadeGOFromType(GrenadeType type)
        {
            switch (type)
            {
                case GrenadeType.HE: return _HEGrenade;
                case GrenadeType.GAS: return _GasGrenade;
            }
            return null;
        }

        private void InstanceGrenade()
        {
            GameObject nade = Instantiate(GetGranadeGOFromType(_currentType));
            nade.transform.position = _head.position - _head.up * 0.08f + _head.forward * .5f;
            IGrenade granadeBehaviour = nade.GetComponent<IGrenade>();
            //HACK: Q CARAJO ES ESTO
            int seconds = UnityEngine.Random.Range(3, 5);

            Rigidbody rb = nade.GetComponent<Rigidbody>();

            if (granadeBehaviour != null) granadeBehaviour.Trigger(seconds);

            if (rb != null)
            {
                float force = 500;
                rb.AddForce((_head.forward.normalized * force));
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 5);
                //Todo: Conservar velocidad de movimiento
                //rb.AddForce(transform.TransformVector(GetComponent<PlayerMovementController>().RelativeVelocity), ForceMode.Impulse);
            }
        }

        private void SetCurrentWeaponInstance(PlayerWeaponInstance instance)
        {
            if (_currentWeaponInstance != null)
            {
                _currentWeaponInstance.CurrentAmmo = _weaponEngine.CurrentAmmo;
                _currentWeaponInstance.Cocked = _weaponEngine.Cocked;
            }

            _weaponEngine = GetEngineFromSlotType(instance.Settings.SlotType);
            _weaponEngine.Initialize(instance.Settings, instance.CurrentAmmo, instance.Cocked, true);
            _weaponEngine.SetHitScanMask(Bootstrap.Resolve<GameSettings>().RaycastConfiguration.PlayerGunLayers);
            _currentWeaponInstance = instance;
            WeaponInstanceChangeEvent?.Invoke(_currentWeaponInstance);
        }

        //TODO:  You should be left behind with you backwards ideas
        private IWeapon GetEngineFromSlotType(WeaponSlotType slotType)
        {
            switch (slotType)
            {
                case WeaponSlotType.MAIN:
                case WeaponSlotType.SECONDARY:
                    return _gunWeaponEngine;

                case WeaponSlotType.MEELEE:
                default:
                    return null;
            }
        }

        private void ChangeWeaponBySlot(WeaponSlotType type)
        {
            if (!AllowInput) return;

            if (_currentWeaponInstance != null)
            {
                if (_isChangingSlot) return;
                if (!_canChangeWeapons) return;
                if (_weaponSlots[type].WeaponInstances.Count == 0) return;

                if (_currentWeaponInstance.Settings.SlotType == type)
                {
                    int index = (int)Mathf.Repeat(_weaponSlots[type].WeaponInstances.IndexOf(_currentWeaponInstance) + 1, _weaponSlots[type].WeaponInstances.Count);

                    if (_currentWeaponInstance == _weaponSlots[type].WeaponInstances[index])
                    {
                        return;
                    }

                    WeaponSwapEvent?.Invoke(type);

                    StartCoroutine(IChangeWeaponToInstance(_weaponSlots[type].WeaponInstances[index]));
                    return;
                }
            }
            WeaponSwapEvent?.Invoke(type);

            if (_weaponSlots[type].WeaponInstances.Count > 0)
            {
                StartCoroutine(IChangeWeaponToInstance(_weaponSlots[type].WeaponInstances[0]));
            }
        }

        public bool TryGiveWeapon(WeaponSettings weapon, int currentAmmo)
        {
            foreach (PlayerWeaponInstance wInstance in _weaponSlots[weapon.SlotType].WeaponInstances)
            {
                if (wInstance.Settings == weapon)
                {
                    Debug.Log("You already have this weapon");
                    return false;
                }
            }

            PlayerWeaponInstance instance = new
           (

                 weapon.Ammo.Size,
                 true,
                 weapon
           );

            if (_weaponSlots[weapon.SlotType].TryAddWeapon(instance))
            {
                if (_currentWeaponInstance != null)
                {
                    if (!_weaponSlots[weapon.SlotType].HasMultipleWeapons && _currentWeaponInstance.Settings.SlotType == weapon.SlotType)
                    {
                        StartCoroutine(IReplaceWeaponToInstance(instance));
                    }
                }

                Debug.Log($"Player got {weapon.name} with {currentAmmo} rounds remaining");
                StartCoroutine(IChangeWeaponToInstance(instance));
                return true;
            }
            return false;
        }

        private IEnumerator IChangeWeaponToInstance(PlayerWeaponInstance instance)
        {
            _isChangingSlot = true;

            if (_currentWeaponInstance != null)
            {
                Seathe();
            }
            yield return new WaitForSeconds(1);
            SetCurrentWeaponInstance(instance);
            Draw();
            _isChangingSlot = false;
            yield return null;
        }

        private IEnumerator IReplaceWeaponToInstance(PlayerWeaponInstance instance)
        {
            if (_currentWeaponInstance != null)
            {
                _isChangingSlot = true;
                Seathe();
                DestroyWeaponInstance(_currentWeaponInstance);
                yield return new WaitForSeconds(.5f);
            }

            SetCurrentWeaponInstance(instance);

            Draw();
            yield return new WaitForSeconds(.5f);
            _isChangingSlot = false;
            yield return null;
        }

        private void DestroyWeaponInstance(PlayerWeaponInstance instance)
        {
        }

        private bool CheckForwardObstruction()
        {
            return VisualPhysics.Raycast(_head.position, _head.forward, 1f, gameObject.layer);
        }

        private Vector2 Noise()
        {
            Vector2 v = new Vector2(Mathf.PerlinNoise(Time.time * _weaponEngine.WeaponSettings.Sway.NoiseTime, 0f), Mathf.PerlinNoise(0f, Time.time * _weaponEngine.WeaponSettings.Sway.NoiseTime));
            v.x -= .5f;
            v.y -= .5f;
            v *= 2f;
            return v * _weaponEngine.WeaponSettings.Sway.NoiseMagnitude;
        }

        private void OnDestroy()
        {
            foreach (IObserverFromPlayerWeapon observer in GetComponentsInChildren<IObserverFromPlayerWeapon>())
            {
                observer.Detach(this);
            }
        }
    }

    public delegate void WeaponSlotDelegate(PlayerWeaponSlot slot);

    [Serializable]
    public class PlayerWeaponSlot
    {
        private WeaponSlotType _slotType;

        private List<PlayerWeaponInstance> _weapons = new List<PlayerWeaponInstance>();

        public PlayerWeaponSlot(WeaponSlotType slotType, bool multipleWeapons)
        {
            _slotType = slotType;
            HasMultipleWeapons = multipleWeapons;
        }

        public List<PlayerWeaponInstance> WeaponInstances { get => _weapons; }
        public WeaponSlotType SlotType { get => _slotType; }
        public bool HasMultipleWeapons { get; private set; }

        public event WeaponSlotDelegate SlotWeaponAddedEvent;

        public bool TryAddWeapon(PlayerWeaponInstance weapon)
        {
            //En desuso
            if (HasMultipleWeapons)
            {
                if (!_weapons.Contains(weapon))
                {
                    _weapons.Add(weapon);
                    SlotWeaponAddedEvent?.Invoke(this);
                    return true;
                }

                return false;
            }

            if (!_weapons.Contains(weapon))
            {
                _weapons.Clear();
                _weapons.Add(weapon);
                SlotWeaponAddedEvent?.Invoke(this);

                return true;
            }

            return false;
        }
    }

    [Serializable]
    public class PlayerWeaponInstance
    {
        public WeaponSettings Settings;
        public int CurrentAmmo;
        public bool Cocked;

        public PlayerWeaponInstance(int currentAmmo, bool isChambered, WeaponSettings settings)
        {
            CurrentAmmo = currentAmmo;
            Cocked = isChambered;
            Settings = settings;
        }
    }
}