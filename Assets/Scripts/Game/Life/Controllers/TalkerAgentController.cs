using Game.Entities;
using Game.Service;
using Life.StateMachines;
using UnityEngine;

namespace Life.Controllers
{
    public class TalkerAgentController : AgentController, IInteractable
    {
        private bool _avaliable;

        bool IInteractable.BeginInteraction(Vector3 position)
        {
            Interact();
            return true;
        }

        private void Interact()
        {
            SubtitleParameters subtitle = new SubtitleParameters();

            subtitle.Name = "John Salchichon";
            subtitle.Content = "Hola, este es un subtitulo de lo que esta diciendo este chadazo";
            subtitle.Location = transform.position + transform.up * 1.75f + transform.right * -0.125f;
            subtitle.Duration = 10f;
            UIService.CreateSubtitle(subtitle);
        }

        bool IInteractable.CanInteract() => _avaliable;

        bool IInteractable.IsDone(bool cancelRequest)
        {
            return true;
        }

        // Use this for initialization
        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
        }
    }

    public class TalkerReadLine : BaseState
    {
        private TalkerAgentController _talker;

        public TalkerReadLine(AgentController context) : base(context)
        {
            _talker = context as TalkerAgentController;
        }

        public override void DrawGizmos()
        {
            throw new System.NotImplementedException();
        }

        public override void End()
        {
            throw new System.NotImplementedException();
        }

        public override void Start()
        {
            throw new System.NotImplementedException();
        }

        public override void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}