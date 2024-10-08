using Core.Weapon;
using Game.Inventory;
using Game.Player.Weapon;
using Game.Weapon;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Player.Controllers
{
    public class PlayerViewmodels : MonoBehaviour
    {
        private Dictionary<WeaponSettings, GameObject> _activeWeapons = new Dictionary<WeaponSettings, GameObject>();

        private PlayerWeapons _weapons;
        private PlayerInventoryController _inventory;
        private GameObject _currentWeapon;
        private Animator _animator;

        [SerializeField] private GameObject _grenadeHands;
        [SerializeField] private GameObject _cameraTracker;
        [SerializeField] private Transform _cameraTrackerBone;

        private bool _subscribedToWeaponEngine = false;
        private bool _firedDryFire;

        private void Start()
        {
            _weapons = FindObjectOfType<PlayerWeapons>();
            _inventory = FindObjectOfType<PlayerInventoryController>();
            _weapons.WeaponInstanceChangeEvent += OnWeaponInstanceChanged;
            _weapons.WeaponAimEvent += OnWeaponAim;
            _weapons.WeaponDrawEvent += OnWeaponDraw;
            _weapons.WeaponGrenadeStateEvent += OnGrenadeState;

            _inventory.ItemBeginConsumingEvent += OnBeginConsume;
            _inventory.ItemFinishConsumeEvent += OnEndConsume;
        }

        private void OnEndConsume(ConsumableItem item)
        {
        }

        private void OnBeginConsume(ConsumableItem item)
        {
            GameObject go = Instantiate(item.AnimationGameObject, transform, false);
            Destroy(go, 5);
        }

        private void OnGrenadeState(GrenadeType type, GrenadeState state)
        {
            _grenadeHands.GetComponent<Animator>().SetTrigger(state.ToString());
            _grenadeHands.GetComponent<Animator>().SetInteger("TYPE", (int)type);
        }

        private void OnWeaponDraw(bool state)
        {
            Debug.Log(state);
            _animator.SetTrigger(state ? "DRAW" : "SEATHE");
        }

        private void OnWeaponAim(bool state)
        {
            if (_weapons.WeaponEngine.IsReloading) return;

            _animator.SetTrigger("ACTION");
        }

        private void LateUpdate()
        {
            if (_cameraTrackerBone == null) return;

            _cameraTracker.transform.localRotation = Quaternion.LookRotation(_currentWeapon.transform.InverseTransformDirection(_cameraTrackerBone.forward));
        }

        //Called when the weapon engine emits an change in state
        private void OnWeaponChangeState(object sender, WeaponStateEventArgs e)
        {
            _animator.ResetTrigger("DRY");

            if (_currentWeapon != null)
            {
                if (e.State == WeaponState.FAIL_SHOOTING)
                {
                    _firedDryFire = true;
                }
                if (e.State == WeaponState.BEGIN_SHOOTING)
                {
                    _firedDryFire = false;
                }

                _animator.SetTrigger(e.State.ToString());

                _animator.SetBool("EMPTY", _weapons.WeaponEngine.CurrentAmmo == 0);
                _animator.SetBool("COCKED", _weapons.WeaponEngine.Cocked);
                _animator.SetBool("DRY", _firedDryFire);
            }
        }

        //Called when the weapon instance change in PlayerWeapons
        private void OnWeaponInstanceChanged(PlayerWeaponInstance instance)
        {
            //Procura estar subscrito al WeaponEngine
            if (!_subscribedToWeaponEngine)
            {
                _weapons.WeaponEngine.WeaponChangedState += OnWeaponChangeState;
                _subscribedToWeaponEngine = true;
            }
            if (!_activeWeapons.ContainsKey(instance.Settings))
            {
                GameObject weapon = Instantiate(instance.Settings.WeaponPrefab);
                _activeWeapons.Add(instance.Settings, weapon);
                weapon.transform.SetParent(transform, false);
            }

            _currentWeapon = _activeWeapons[instance.Settings];
            _animator = _currentWeapon.GetComponent<Animator>();

            ManageTrackerBoneCamera();
            ManageVisibility(instance);
        }

        private void ManageTrackerBoneCamera()
        {
            if (_currentWeapon.TryGetComponent(out TrackerBone tracker))
            {
                _cameraTrackerBone = tracker.Bone;
                return;
            }
            else
            {
                _cameraTracker.transform.localRotation = Quaternion.identity;
                _cameraTrackerBone = null;
            }
        }

        private void ManageVisibility(PlayerWeaponInstance instance)
        {
            foreach (GameObject go in _activeWeapons.Values)
            {
                if (go == _activeWeapons[instance.Settings])
                {
                    go.SetActive(true);
                    continue;
                }

                go.SetActive(false);
            }
        }
    }
}