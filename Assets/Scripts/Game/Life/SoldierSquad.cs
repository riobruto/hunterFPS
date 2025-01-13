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
    //

    public class SoldierSquad
    {
        //Crear una jerarquia en base a la proximidad, riesgo y quizas, disponibilidad de covertura, y permitirle solo a los dos principales disparar
        public List<SoldierAgentController> AttackingAgents = new List<SoldierAgentController>();

        private List<SoldierAgentController> _soldiers;

        private Transform _goalHoldTransform;
        private float _holdLocationMaxDistance = 30;

        //detect this events in NPC NOW!
        public event SquadMemberDelegate SquadMemberDeceasedEvent;

        public event SquadMemberDelegate SquadMemberSearchingPlayer;

        public event SquadMemberDelegate SquadMemberTakingCover;

        public event SquadMemberDelegate SquadMemberTakingDamage;

        public event SquadMemberDelegate SquadMemberSawPlayer;

        public event SquadMemberGrenadeDelegate SquadMemberThrowGranadeToPlayer;

        public int MemberAmount => _soldiers.Count;
        public bool HasEngageTimeout { get => Time.time - _timeSincePlayerFound > _timeToCalm; }

        public const int MemberAmountLimit = 5;
        public const int AttackingSlots = 2;
        public const int FlankingSlots = 1;
        public const int GrenadeSlot = 1;
        public const int GrenadeCoolDown = 30;

        //GroupStealthData
        private float _timeSincePlayerFound = 0;

        private float _timeToCalm = 15f;
        private float _elapsedSinceAlerted = 0;

        //allows npcs to reduce the time since they spotted the player, so they can relax

        private bool _canLoosePlayer = true;
        public bool ShouldHoldPlayer { get; private set; } = false;
        public void SetGoalHold(Transform transform)
        {
            _goalHoldTransform = transform;
            ShouldHoldPlayer = true;
        }
        public void SetGoalChase()
        {
            ShouldHoldPlayer = false;
        }

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

        public bool CanThrowGrenade { get => Time.time - _lastGrenadeTime > GrenadeCoolDown; }

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
            _timeSincePlayerFound = Time.time;
            CreateTimedGizmo(Shape.SQUARE, controller.transform.position + Vector3.up * 1.5f, 3, Color.red + Color.blue);
        }

        private void OnSoldierHearedPlayer(AgentController controller)
        {
            _timeSincePlayerFound = Time.time;




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
            _timeSincePlayerFound = Time.time;

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

            _timeSincePlayerFound = Time.time;
            CreateTimedGizmo(Shape.SQUARE, sender.transform.position + Vector3.up * 2, 3, Color.red);
            SquadMemberSawPlayer?.Invoke(sender as SoldierAgentController);
        }

        private void OnSoldierDead(AgentController controller)
        {
            SoldierAgentController soldier = controller as SoldierAgentController;

            if (!_soldiers.Contains(soldier)) return;
            SquadMemberDeceasedEvent?.Invoke(soldier);

            _soldiers.Remove(soldier);
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
            ReleaseAttackSlot(soldier);
        }

        public bool TryTakeAttackSlot(SoldierAgentController controller)
        {
            if (AttackingAgents.Contains(controller)) return true;
            if (AttackingAgents.Count == AttackingSlots) return false;
            else AttackingAgents.Add(controller); return true;
        }

        public void TakeAttackSlotForce(SoldierAgentController controller)
        {
            if (AttackingAgents.Contains(controller)) return;
            if (AttackingAgents.Count == AttackingSlots)
            {
                AttackingAgents.Remove(AttackingAgents[0]);
            }
            AttackingAgents.Add(controller);
        }

        public void ReleaseAttackSlot(SoldierAgentController controller)
        {
            if (AttackingAgents.Contains(controller)) AttackingAgents.Remove(controller);
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

        //GIZMO STUFF

        private List<TimedGizmo> GIZMOS = new List<TimedGizmo>();
        private float _lastGrenadeTime;

        private void CreateTimedGizmo(Shape shape, Vector3 pos, int duration, Color color)
        {
            GIZMOS.Add(new TimedGizmo(duration, Time.time, pos, color, shape));
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
            foreach (SoldierAgentController controller in AttackingAgents)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(controller.transform.position + Vector3.up, Vector3.one + Vector3.up);
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

            for (int i = 0; i < GIZMOS.Count; i++)
            {
                if (Time.time - GIZMOS[i].Time > GIZMOS[i].Duration) { GIZMOS.Remove(GIZMOS[i]); continue; }

                Gizmos.color = GIZMOS[i].Color;
                switch (GIZMOS[i].Shape)
                {
                    case Shape.SPHERE:
                        Gizmos.DrawSphere(GIZMOS[i].Position, .15f);
                        break;

                    case Shape.SQUARE:
                        Gizmos.DrawCube(GIZMOS[i].Position, Vector3.one * .30f);
                        break;

                    case Shape.WIRESPHERE:
                        Gizmos.DrawWireSphere(GIZMOS[i].Position, .15f);
                        break;
                }
            }
        }

        internal bool HasAttackSlot(SoldierAgentController soldierAgentController)
        {
            return AttackingAgents.Contains(soldierAgentController);
        }
    }
}