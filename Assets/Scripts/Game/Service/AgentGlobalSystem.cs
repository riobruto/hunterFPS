using Core.Engine;
using Life.Controllers;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Service
{
    public class AgentGlobalService : SceneService
    {
        public static AgentGlobalSystem Instance { get; private set; }

        internal override void Initialize()
        {
            Instance = new GameObject("AgentGlobalSystem").AddComponent<AgentGlobalSystem>();
            //GameObject.DontDestroyOnLoad(Instance);
            Instance.Initialize();
        }
    }

    public class AgentGlobalSystem : MonoBehaviour
    {
        private List<AgentController> _activeAgents = new List<AgentController>();

        //GroupStealthData
        private float _elapsedSincePlayerFound = 0;

        private float _elapsedSinceAlerted = 0;
        private float _timeToCalm = 13f;
        private bool _playerRevealed;

        internal void Initialize()
        {

        }
        public int _attackSlots = 2;
        public List<AgentController> _attackingAgents = new List<AgentController>();


        public bool TryTakeAttackSlot(AgentController controller)
        {
            if (_attackingAgents.Count == _attackSlots) return false;
            else _attackingAgents.Add(controller);
            return true;
        }       

        public void TakeAttackSlotForce(AgentController controller)
        {
            _attackingAgents.Remove(_attackingAgents[0]);
            _attackingAgents.Add(controller);
        }

        public void ReleaseAttackSlot(AgentController controller)
        {
            if (_attackingAgents.Contains(controller)) _attackingAgents.Remove(controller);
        }

        public void RegisterAgent(AgentController controller)
        {
            _activeAgents.Add(controller);
            controller.PlayerPerceptionEvent += OnPlayerDetectedByAgent;
        }

        private void Update()
        {
        }

        private void OnPlayerDetectedByAgent(bool found)
        {
            if (!found) return;
            ResetStealthStatus();

            foreach (AgentController controller in _activeAgents)
            {
                if (controller.AgentGroup != Life.AgentGroup.AGGRO) return;
                controller.ForcePlayerPerception();
            }
        }

        private void ResetStealthStatus()
        {
            _elapsedSincePlayerFound = 0;
            _elapsedSinceAlerted = 0;
        }

        public void DiscardAgent(AgentController controller)
        {
            _activeAgents.Remove(controller);
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Space(180);

                foreach (AgentController controller in _activeAgents)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(controller.name);
                    GUILayout.Label(controller.CurrentState.ToString());
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}