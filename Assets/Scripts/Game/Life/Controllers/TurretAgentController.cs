using Core.Engine;
using Game.Audio;
using Game.Service;
using Life.Controllers;
using Life.StateMachines;

using UnityEngine;

namespace Game.Life.Controllers
{
    public class TurretAgentController : AgentController
    {
        private TurretAttack _attack;
        private TurretStandBy _standby;
        private TurretDead _dead;

        private AgentFireWeapon _weapon;

        [SerializeField] private Transform _pitch;
        [SerializeField] private Transform _yaw;
        [SerializeField] private Material _eye;
        [SerializeField] private Light _light;
        [SerializeField] private AudioClip _engageClip;
        [SerializeField] private AudioClip _standbyClip;

        public Material Eye { get => _eye; }
        public AgentFireWeapon Weapon { get => _weapon; }
        public bool AllowFire { get => _allowFire; set => _allowFire = value; }
        public Transform Yaw { get => _yaw; }
        public Transform Pitch { get => _pitch; }
        public Light Light { get => _light; }
        public AudioClip EngageClip { get => _engageClip; }
        public AudioClip StandbyClip { get => _standbyClip; }

        public void PlayClip(AudioClip clip)
        { AudioToolService.PlayClipAtPoint(clip, _pitch.position, 1, AudioChannels.AGENT); }

        public override void OnHurt(AgentHurtPayload value)
        {
            SetHealth(GetHealth() - value.Amount);
            if (Machine.CurrentState == _attack) return;

            Machine.ForceChangeToState(_attack);
        }

        public override void OnHeardCombat()
        {
            if (Machine.CurrentState == _attack) return;
            Machine.ForceChangeToState(_attack);
        }

        public override void OnStart()
        {
            NavMeshAgent.enabled = false;
            Animator.enabled = false;
            CreateStates();
            CreateTransitions();

            _weapon = GetComponent<AgentFireWeapon>();
            SetMaxHealth(100);
            SetHealth(100);
        }

        private void CreateTransitions()

        {
            Machine.AddTransition(_standby, _dead, new FuncPredicate(() => IsDead));
            Machine.AddTransition(_attack, _dead, new FuncPredicate(() => IsDead));

            Machine.AddTransition(_standby, _attack, new FuncPredicate(() => HasPlayerVisual));
            Machine.AddTransition(_attack, _standby, new FuncPredicate(() => !(HasPlayerVisual)));

            Machine.SetState(_standby);
        }

        private void CreateStates()
        {
            _attack = new TurretAttack(this);
            _standby = new TurretStandBy(this);
            _dead = new TurretDead(this);
        }

        public override void OnUpdate()
        {
            ManageWeapon();

            if (PlayerHeadPosition != Vector3.zero)
            {
                Weapon.SetFireTarget(PlayerHeadPosition);
            }
        }

        private bool _allowFire;
        private bool _shooting;

        private void ManageWeapon()
        {
            if (IsDead) return;

            if (_weapon.Empty)
            {
                _weapon.WeaponEngine.ReleaseFire();
                _shooting = false;
                _weapon.WeaponEngine.Reload();
                return;
            }

            if (!_allowFire)
            {
                _weapon.WeaponEngine.ReleaseFire();
                _shooting = false;
                return;
            }
            _shooting = true;

            if (_shooting) _weapon.WeaponEngine.Fire();
            else _weapon.WeaponEngine.ReleaseFire();
        }
    }

    public class TurretAttack : BaseState
    {
        private TurretAgentController _turret;
        private float _cooldown;

        public TurretAttack(AgentController context) : base(context)
        {
            _turret = context as TurretAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {            //TODO: Sound
            _cooldown = .02f;
            _turret.Light.intensity = 1570f;
            _turret.PlayClip(_turret.EngageClip);
        }

        public override void Update()
        {
            Vector3 dir = (_turret.PlayerHeadPosition - _turret.Pitch.position).normalized;
            Quaternion look = Quaternion.LookRotation(dir);
            Vector3 yaw = look.eulerAngles;
            yaw.x = 0;
            yaw.z = 0;
            _turret.Yaw.localRotation = Quaternion.RotateTowards(_turret.Yaw.localRotation, Quaternion.Euler(yaw), 5);
            Vector3 pitch = look.eulerAngles;
            pitch.y = 0;
            pitch.z = 0;
            _turret.Pitch.localRotation = Quaternion.RotateTowards(_turret.Pitch.localRotation, Quaternion.Euler(pitch), 5);

            if (_cooldown > 0) { _cooldown -= Time.deltaTime; return; }

            _turret.AllowFire = true;
        }
    }

    public class TurretStandBy : BaseState
    {
        private TurretAgentController _turret;
        private float _cooldown;

        public TurretStandBy(AgentController context) : base(context)
        {
            _turret = context as TurretAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _currentDir = _turret.transform.forward - _turret.transform.up;
            _cooldown = 2;
            _turret.Light.intensity = 1000;
            _turret.PlayClip(_turret.StandbyClip);
        }

        public override void Update()
        {
            if (_cooldown > 0)
            {
                _cooldown -= Time.deltaTime; return;
            }

            _turret.AllowFire = false;

            Vector3 dir = GetDirection();
            Quaternion look = Quaternion.LookRotation(dir);
            Vector3 yaw = look.eulerAngles;
            yaw.x = 0;
            yaw.z = 0;
            _turret.Yaw.localRotation = Quaternion.Slerp(_turret.Yaw.localRotation, Quaternion.Euler(yaw), Time.deltaTime);
            Vector3 pitch = look.eulerAngles;
            pitch.y = 0;
            pitch.z = 0;
            _turret.Pitch.localRotation = Quaternion.Slerp(_turret.Pitch.localRotation, Quaternion.Euler(pitch), Time.deltaTime);
        }

        private float _timeChange = .04F;
        private float _time;

        private Vector3 _currentDir;

        private Vector3 GetDirection()
        {
            _time += Time.deltaTime;
            if (_time > _timeChange)
            {
                _currentDir = Quaternion.Euler(0, 90, 0) * _currentDir;

                _time = 0;
            }
            _currentDir = _currentDir.normalized;

            return _currentDir;
        }
    }

    public class TurretDead : BaseState
    {
        private TurretAgentController _turret;

        public TurretDead(AgentController context) : base(context)
        {
            _turret = context as TurretAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _turret.AllowFire = false;
            Bootstrap.Resolve<ImpactService>().System.ExplosionAtPosition(_turret.Pitch.position);

            _turret.Light.intensity = 0;
        }

        public override void Update()
        {
            Vector3 dir = Vector3.down;
            Quaternion look = Quaternion.LookRotation(dir);
            Vector3 yaw = look.eulerAngles;
            yaw.x = 0;
            yaw.z = 0;
            _turret.Yaw.localRotation = Quaternion.Slerp(_turret.Yaw.localRotation, Quaternion.Euler(yaw), Time.deltaTime);
            Vector3 pitch = look.eulerAngles;
            pitch.y = 0;
            pitch.z = 0;
            _turret.Pitch.localRotation = Quaternion.Slerp(_turret.Pitch.localRotation, Quaternion.Euler(pitch), Time.deltaTime);
        }
    }
}