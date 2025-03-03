using Life.Controllers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Life
{
    public delegate void SquadMemberDelegate(SoldierAgentController soldier);

    public delegate void SquadMemberGrenadeDelegate(SoldierAgentController soldier, Vector3 target);

    public delegate void SquadAttackingSlotChanged();

    //esto determina donde posicionarse en relacion al mundo y covertura
    //la idea es que si el goal es ASSAULT PLAYER, la squad va a ir a buscar puntos cercnos al player.
    //mientas que HOLD_LOCATION implica buscar lugares o posiciones sin abandonar la ubicacion.

    public class SoldierSquad
    {
        private List<SoldierAgentController> _soldiers;

        //Crear una jerarquia en base a la proximidad, riesgo y quizas, disponibilidad de covertura, y permitirle solo a los dos principales disparar
        private SoldierAgentController[] _attackingAgents;

        private Transform _goalHoldTransform;
        private float _holdLocationMaxDistance = 30;
        public const int MemberAmountLimit = 4;
        public const int AttackingSlots = 2;
        public const int FlankingSlots = 1;
        public const int GrenadeSlot = 1;
        public const int GrenadeCoolDown = 30;

        //detect this events in NPC NOW!
        public event SquadMemberDelegate SquadMemberDeceasedEvent;

        public event SquadMemberDelegate SquadMemberSearchingPlayer;

        public event SquadMemberDelegate SquadMemberTakingCover;

        public event SquadMemberDelegate SquadMemberTakingDamage;

        public event SquadMemberDelegate SquadMemberSawPlayer;

        public event SquadMemberGrenadeDelegate SquadMemberThrowGranadeToPlayer;

        //idealmente, una vez alertadra la escuadra no podra relajarse, seguira en estado de alerta hasta terminar su runtime.

        //GroupStealthData

        private float _contactTimeOut = 15f;
        private float _elapsedSinceAlerted = 0;
        private float _elapsedSinceContact = 0;
        private bool _squadCanLoseContact;
        private float _lastGrenadeTime;
        private bool _isAlerted;

        public bool SquadCanLoseContact
        {
            get
            {
                return _squadCanLoseContact;
            }
            set
            {
                foreach (SoldierAgentController soldier in _soldiers)
                {
                    soldier.CanLoseContact = value;
                }
                _squadCanLoseContact = value;
            }
        }

        public bool ShouldHoldPlayer { get; private set; } = false;
        public float MaxHoldRadius { get; private set; }

        public void SetGoalHold(Transform transform, float maxRadius)
        {
            _goalHoldTransform = transform;
            ShouldHoldPlayer = true;
            MaxHoldRadius = maxRadius;
        }

        public void SetGoalChase() => ShouldHoldPlayer = false;

        public void ForceEngage(int duration = 10) => _elapsedSinceContact = 0;

        public void UpdateContact() => _elapsedSinceContact = 0;

        public void UpdateAlert() => _elapsedSinceContact = 0;

        public Vector3 HoldPosition
        {
            get => _goalHoldTransform.position;
        }

        public Vector3 SquadCentroid
        {
            get
            {
                Vector3 centroid = Vector3.zero;
                for (int i = 0; i < _soldiers.Count; i++)
                {
                    centroid += _soldiers[i].transform.position;
                }
                centroid /= (_soldiers.Count + 1);
                return centroid;
            }
        }

        public List<SoldierAgentController> Members => _soldiers;
        public int MemberAmount => _soldiers.Count;

        //Stealth Data
        public bool HasLostContact { get => _elapsedSinceContact > _contactTimeOut; }

        public bool CanThrowGrenade { get => Time.time - _lastGrenadeTime > GrenadeCoolDown; }

        public float ElapsedTimeSinceContact { get => _elapsedSinceContact; }
        public float ElapsedTimeSinceAlert { get => _elapsedSinceAlerted; }
        public float TimeToLostContact { get => _contactTimeOut; }
        public bool IsAlert => _isAlerted;

        public SoldierAgentController[] AttackingAgents { get => _attackingAgents; }

        public SoldierSquad(SoldierAgentController[] soldiers)
        {
            _soldiers = new List<SoldierAgentController>();

            foreach (SoldierAgentController soldier in soldiers)
            {
                _soldiers.Add(soldier);
                soldier.DeadEvent += OnSoldierDead;
                soldier.PlayerPerceptionEvent += OnSoldierHavingPlayerVisual;
                soldier.SearchingPlayerEvent += OnSoldierSearchingPlayer;
                soldier.HeardGunshotsEvent += OnSoldierHearedCombat;
                soldier.HeardStepsEvent += OnSoldierHearedPlayer;
                soldier.TakingCoverEvent += OnSoldierTakingCover;
                soldier.TakingDamageEvent += OnSoldierTakingDamage;
                soldier.ThrowGrenadeEvent += OnSoldierThrowGrenade;
                soldier.AdvancingAttackEvent += OnSoldierAdvancingToPlayer;
                soldier.FlankingEvent += OnSoldierFlankingPlayer;
                soldier.SetSquad(this);
            }

            //set up timers
            _elapsedSinceContact = _contactTimeOut;
            _elapsedSinceAlerted = _contactTimeOut;
            SquadCanLoseContact = true;
        }

        private void OnSoldierThrowGrenade(SoldierAgentController csoldier, Vector3 targetPosition)
        {
            SquadMemberThrowGranadeToPlayer?.Invoke(csoldier, targetPosition);
            _lastGrenadeTime = Time.time;
        }

        private void OnSoldierSearchingPlayer(SoldierAgentController csoldier)
        {
        }

        private void OnSoldierHearedCombat(AgentController controller)
        {
            _elapsedSinceContact = Time.time;
            CreateTimedGizmo(Shape.SQUARE, controller.transform.position + Vector3.up * 1.5f, 3, Color.red + Color.blue);
        }

        private void OnSoldierHearedPlayer(AgentController controller)
        {
            _elapsedSinceAlerted = 0;

            CreateTimedGizmo(Shape.SQUARE, controller.transform.position + Vector3.up * 1.5f, 3, Color.green + Color.blue);
        }

        private void OnSoldierFlankingPlayer(SoldierAgentController csoldier)
        {
        }

        private void OnSoldierAdvancingToPlayer(SoldierAgentController csoldier)
        {
        }

        private void OnSoldierTakingDamage(SoldierAgentController csoldier)
        {
            //idealmente, el soldado no avisara a toda la escuadra inmediatamente, si no que primero debera ver al player.
            //_elapsedSinceContact = Time.time;
            CreateTimedGizmo(Shape.SPHERE, csoldier.Head.position + Vector3.up, 3, Color.red);
        }

        private void OnSoldierTakingCover(SoldierAgentController csoldier)
        {
        }

        private void OnSoldierHavingPlayerVisual(AgentController sender, bool visible)
        {
            if (!visible)
            {
                CreateTimedGizmo(Shape.SQUARE, sender.transform.position + Vector3.up * 2, 3, Color.green);
                return;
            }
            //hack: gives the ability to shoot reactively
            _elapsedSinceContact = 0;
            CreateTimedGizmo(Shape.SQUARE, sender.transform.position + Vector3.up * 2, 3, Color.red);
            SquadMemberSawPlayer?.Invoke(sender as SoldierAgentController);
        }

        private void OnSoldierDead(AgentController controller)
        {
            SoldierAgentController soldier = controller as SoldierAgentController;
            if (!_soldiers.Contains(soldier)) return;
            SquadMemberDeceasedEvent?.Invoke(soldier);
            soldier.DeadEvent -= OnSoldierDead;
            soldier.PlayerPerceptionEvent -= OnSoldierHavingPlayerVisual;
            soldier.HeardGunshotsEvent -= OnSoldierHearedCombat;
            soldier.HeardStepsEvent -= OnSoldierHearedPlayer;
            soldier.TakingCoverEvent -= OnSoldierTakingCover;
            soldier.TakingDamageEvent -= OnSoldierTakingDamage;
            soldier.AdvancingAttackEvent -= OnSoldierAdvancingToPlayer;
            soldier.SearchingPlayerEvent -= OnSoldierSearchingPlayer;
            soldier.ThrowGrenadeEvent -= OnSoldierThrowGrenade;
            soldier.FlankingEvent -= OnSoldierFlankingPlayer;
            soldier.SetSquad(null);

            _soldiers.Remove(soldier);
        }

        public bool TryTakeAttackSlot(SoldierAgentController controller)
        {
            if (_attackingAgents.Length < 2) return true;
            if (_attackingAgents[0] == controller) return true;
            if (_attackingAgents[1] == controller) return true;
            return false;
        }

        internal void AddMembers(SoldierAgentController[] soldiers)
        {
            foreach (SoldierAgentController soldier in soldiers)
            {
                _soldiers.Add(soldier);
                soldier.DeadEvent += OnSoldierDead;
                soldier.PlayerPerceptionEvent += OnSoldierHavingPlayerVisual;
                soldier.HeardGunshotsEvent += OnSoldierHearedCombat;
                soldier.HeardStepsEvent += OnSoldierHearedPlayer;
                soldier.TakingCoverEvent += OnSoldierTakingCover;
                soldier.TakingDamageEvent += OnSoldierTakingDamage;
                soldier.SearchingPlayerEvent += OnSoldierSearchingPlayer;
                soldier.AdvancingAttackEvent += OnSoldierAdvancingToPlayer;
                soldier.ThrowGrenadeEvent += OnSoldierThrowGrenade;
                soldier.FlankingEvent += OnSoldierFlankingPlayer;

                soldier.SetSquad(this);
            }
        }

        //sort by
        //has visual
        //distance
        //

        private int SortAttackingAgents(SoldierAgentController asoldier, SoldierAgentController bsoldier)
        {
            int i = 0;

            if (asoldier == null || bsoldier == null) return 0;
            if (asoldier == null) return -1;
            if (bsoldier == null) return 1;

            float aDis = Vector3.Distance(asoldier.transform.position, asoldier.PlayerGameObject.transform.position);
            float bDis = Vector3.Distance(bsoldier.transform.position, bsoldier.PlayerGameObject.transform.position);
            i += aDis.CompareTo(bDis);

            i += asoldier.Weapon.Empty.CompareTo(bsoldier.Weapon.Empty);
            i -= asoldier.IsPlayerVisible().CompareTo(bsoldier.IsPlayerVisible());
            i += asoldier.GetHealth().CompareTo(bsoldier.GetHealth());
            /*
            i += asoldier.IsDead.CompareTo(bsoldier.IsDead);*/

            return i;
        }

        public float _sortAttackSlotsInterval = 3f;
        private float _lastSortAttackingSlotsTime;

        public void UpdateSquad()
        {
            if (_soldiers.Count > 0){

                if (Time.time - _lastSortAttackingSlotsTime > _sortAttackSlotsInterval)
                {
                    _attackingAgents = _soldiers.ToArray();
                    Array.Sort(_attackingAgents, SortAttackingAgents);
                    _lastSortAttackingSlotsTime = Time.time;
                }
            } 

            _elapsedSinceContact = Mathf.Clamp(_elapsedSinceContact + Time.deltaTime, 0, float.MaxValue);
            if (HasLostContact)
            {
                _elapsedSinceAlerted = Mathf.Clamp(_elapsedSinceAlerted + Time.deltaTime, 0, float.MaxValue);
            }
            else _elapsedSinceAlerted = 0;
        }

        //GIZMO STUFF

        private List<TimedGizmo> _gizmos = new List<TimedGizmo>();

        private void CreateTimedGizmo(Shape shape, Vector3 pos, int duration, Color color)
        {
            _gizmos.Add(new TimedGizmo(duration, Time.time, pos, color, shape));
        }

        private class TimedGizmo
        {
            public int Duration;
            public float Time;
            public Vector3 Position;
            public Color Color;
            public Shape Shape;

            public TimedGizmo(int duration, float time, Vector3 position, Color color, Shape shape)
            {
                Duration = duration;
                Time = time;
                Position = position;
                Color = color;
                Shape = shape;
            }
        }

        private enum Shape
        { SPHERE, SQUARE, WIRESPHERE }

        internal void DrawGizmos()
        {
            for (int i = 0; i < _attackingAgents.Length; i++)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, i * 1f / (_attackingAgents.Length * 1f));
                Gizmos.DrawWireCube(_attackingAgents[i].transform.position + Vector3.up, Vector3.one + Vector3.up);
            }

            Gizmos.DrawSphere(SquadCentroid, 0.5f);

            for (int i = 0; i < _soldiers.Count; i++)
            {
                for (int j = 0; j < _soldiers.Count; j++)
                {
                    if (_soldiers[i] == _soldiers[j]) continue;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(_soldiers[i].transform.position, _soldiers[j].transform.position);
                }
            }

            for (int i = 0; i < _gizmos.Count; i++)
            {
                if (Time.time - _gizmos[i].Time > _gizmos[i].Duration) { _gizmos.Remove(_gizmos[i]); continue; }

                Gizmos.color = _gizmos[i].Color;
                switch (_gizmos[i].Shape)
                {
                    case Shape.SPHERE:
                        Gizmos.DrawSphere(_gizmos[i].Position, .15f);
                        break;

                    case Shape.SQUARE:
                        Gizmos.DrawCube(_gizmos[i].Position, Vector3.one * .30f);
                        break;

                    case Shape.WIRESPHERE:
                        Gizmos.DrawWireSphere(_gizmos[i].Position, .15f);
                        break;
                }
            }
        }
    }
}