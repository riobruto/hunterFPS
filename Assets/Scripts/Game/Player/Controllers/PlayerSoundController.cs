using Game.Player.Sound;
using Game.Player.Weapon;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player.Controllers
{
    [RequireComponent(typeof(AudioSource))]
    public class PlayerSoundController : MonoBehaviour, IObserverFromPlayerWeapon
    {
        public event UnityAction<Vector3, float> StepSound;
        public event UnityAction<Vector3, float> GunSound;

        [SerializeField] private AudioClipCompendium _walkClips;
        [SerializeField] private AudioClipCompendium _runClips;
        [SerializeField] private float _timeBetweenFootstep = 2.33f;

        private float _time;
       
        private AudioSource _audioSource;
        private PlayerRigidbodyMovement _controller;
        private PlayerWeapons _weapons;

        // Start is called before the first frame update
        private void Start()
        {
          
            _controller = transform.root.GetComponent<PlayerRigidbodyMovement>();
            _audioSource = GetComponent<AudioSource>();
            _audioSource.volume = 0.33f;
        }

        // Update is called once per frame
        private void Update()
        {
            ManageFootsteps();
        }

        private void ManageFootsteps()
        {

            _time += (_controller.RelativeVelocity.magnitude) * Time.deltaTime;

            if (_time > _timeBetweenFootstep)
            {
                PlayAudio();
                _time = 0;
            }
        }

        private void PlayAudio()
        {
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            StepSound?.Invoke(transform.position, _controller.CurrentState == PlayerMovementState.SPRINT ? 5 : 10);
            DrawDebug(_controller.CurrentState == PlayerMovementState.SPRINT ? 5 : 10);
            AudioClipCompendium current = _controller.CurrentState == PlayerMovementState.SPRINT ? _walkClips : _runClips;
            _audioSource.PlayOneShot(current.GetRandom());
        }

        void IObserverFromPlayerWeapon.Initalize(PlayerWeapons controller)
        {
            _weapons = controller;
            _weapons.WeaponEngine.WeaponChangedState += OnWeaponChange;
        }

        private void OnWeaponChange(object sender, WeaponStateEventArgs e)
        {
            if (e.State == Core.Weapon.WeaponState.BEGIN_SHOOTING)
            {
                GunSound?.Invoke(transform.position, 40);
                DrawDebug(40f);
            }

            if (e.State == Core.Weapon.WeaponState.BEGIN_RELOADING)
            {
                GunSound?.Invoke(transform.position, 10);
                DrawDebug(10f);
            }
        }

        private void DrawDebug(float v)
        {
            DrawLine(new Vector3(1, 0, 0) * v);
            DrawLine(new Vector3(-1, 0, 0) * v);
            DrawLine(new Vector3(0, 1, 0) * v);
            DrawLine(new Vector3(0, -1, 0) * v);
            DrawLine(new Vector3(0, 0, 1) * v);
            DrawLine(new Vector3(0, 0, -1) * v);
        }

        private void DrawLine(Vector3 direction)
        {
            Debug.DrawLine(transform.position, transform.position + direction, Color.blue, 1);
        }

        void IObserverFromPlayerWeapon.Detach(PlayerWeapons controller)
        {
        }
    }
}