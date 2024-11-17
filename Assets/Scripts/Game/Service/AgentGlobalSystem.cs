using Core.Engine;
using Game.Life;
using Life.Controllers;
using Life.StateMachines;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Service
{
    public class AgentGlobalService : SceneService
    {
        public AgentGlobalSystem Instance { get; private set; }

        internal override void Initialize()
        {
            Instance = new GameObject("AgentGlobalSystem").AddComponent<AgentGlobalSystem>();
            GameObject.DontDestroyOnLoad(Instance);
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