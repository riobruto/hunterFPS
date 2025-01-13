using Core.Engine;
using Game.Life;
using Game.Life.Entities;
using Life.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Service
{
    public class AgentGlobalService : SceneService
    {
        public static AgentGlobalSystem Instance { get; private set; }
        public static bool AIDisabled { get; private set; }
        public static bool IgnorePlayer { get; private set; }

        public static void SetDisableAI(bool value)
        { AIDisabled = value; }

        internal static void SetIgnorePlayer(bool ignorePlayer)
        {
            IgnorePlayer = ignorePlayer;
        }

        internal override void Initialize()
        {
            Instance = new GameObject("AgentGlobalSystem").AddComponent<AgentGlobalSystem>();
            //GameObject.DontDestroyOnLoad(Instance);
            Instance.Initialize();
        }
    }

    public class AgentGlobalSystem : MonoBehaviour
    {
        public List<AgentController> ActiveAgents { get => _activeAgents; }
        public List<CoverSpotEntity> CoverEntities { get => _coverEntities; }

        private List<SoldierSquad> _activeSquads = new List<SoldierSquad>();
        private List<AgentController> _activeAgents = new List<AgentController>();
        private List<CoverSpotEntity> _coverEntities = new List<CoverSpotEntity>();
        private Vector3 _pToSort;

        internal void Initialize()
        {
        }

        public void RegisterAgent(AgentController controller)
        {
            _activeAgents.Add(controller);
            controller.PlayerPerceptionEvent += OnPlayerDetectedByAgent;
        }

        public void RegisterCoverSpot(CoverSpotEntity entity) => _coverEntities.Add(entity);

        public void UnregisterCoverSpot(CoverSpotEntity entity)
        {
            if (_coverEntities.Contains(entity))
                _coverEntities.Remove(entity);
        }

        public SoldierSquad CreateSquad(SoldierAgentController[] soldier)
        {
            if (_activeSquads.Count == 0)
            {
                SoldierSquad sq = new SoldierSquad(soldier);
                _activeSquads.Add(sq);
                return sq;
            }
            foreach (SoldierSquad soldierSquad in _activeSquads)
            {
                if (soldierSquad.MemberAmount + soldier.Length < SoldierSquad.MemberAmountLimit)
                {
                    soldierSquad.AddMembers(soldier);
                    return soldierSquad;
                }
            }

            SoldierSquad squad = new SoldierSquad(soldier);
            _activeSquads.Add(squad);

            return squad;
        }

        private void Update()
        {
            for (int i = 0; i < _activeSquads.Count; i++)
            {
                if (_activeSquads[i].MemberAmount == 0) _activeSquads.Remove(_activeSquads[i]);
            }
        }

        private void OnPlayerDetectedByAgent(AgentController controller, bool found)
        {
            if (!found) return;
            foreach (AgentController cntroller in _activeAgents)
            {
                if (cntroller.AgentGroup != Life.AgentGroup.AGGRO) return;
                cntroller.ForcePlayerPerception();
            }
        }

        public void DiscardAgent(AgentController controller)
        {
            _activeAgents.Remove(controller);
        }

        private void OnGUI()
        {
            foreach (SoldierSquad squad in _activeSquads)
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Is Engaged: {!squad.HasEngageTimeout}");
                    GUILayout.Label($"Members: {squad.MemberAmount}");
                    GUILayout.Label($"Att Slot: {squad.AttackingAgents.Count}");
                    GUILayout.Label($"Can Grenade Slot: {squad.CanThrowGrenade}");
                }
            }
        }

        private void OnDrawGizmos()
        {
            foreach (SoldierSquad squad in _activeSquads)
            {
                squad.DrawGizmos();
            }
        }
    }
}