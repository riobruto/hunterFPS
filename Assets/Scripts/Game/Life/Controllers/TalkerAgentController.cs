using Game.Entities;
using Game.Objectives;
using Game.Service;
using Life.StateMachines;
using System.Collections;
using UnityEngine;

namespace Life.Controllers
{
    public class TalkerAgentController : AgentController, IInteractable
    {
        private bool _avaliable = true;

        [SerializeField] private AgentDialog _dialog;

        bool IInteractable.BeginInteraction(Vector3 position)
        {
            if (!_avaliable) return false;
            Interact();
            return true;
        }

        private void Interact()
        {
            RunDialog();
            _avaliable = false;
        }

        private void RunDialog()
        {
            StartCoroutine(ReadDialog(0));
        }

        private IEnumerator ReadDialog(int v)
        {
            int index = v;

            SubtitleParameters subtitle = new SubtitleParameters();
            subtitle.Name = _dialog.Name;
            subtitle.Content = _dialog.Entries[index].Content;
            subtitle.Location = transform.position + transform.up * 1.75f + transform.right * -0.125f;
            subtitle.Duration = _dialog.Entries[index].Duration;
            UIService.CreateSubtitle(subtitle);
            yield return new WaitForSeconds(_dialog.Entries[index].Duration);

            if (_dialog.Entries.Length - 1 > index) { StartCoroutine(ReadDialog(index + 1)); }
            yield return null;
        }

        bool IInteractable.CanInteract() => _avaliable;

        bool IInteractable.IsDone(bool cancelRequest)
        {
            return true;
        }

        public override void OnStart()
        {

            TalkerReadLine state = new TalkerReadLine(this);
            Machine.AddAnyTransition(state, new FuncPredicate(() => false));
            Machine.SetState(state);
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
        }

        public override void End()
        {
        }

        public override void Start()
        {
        }

        public override void Think()
        {
        }
    }
}