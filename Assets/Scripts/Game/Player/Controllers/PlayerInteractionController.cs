using Core.Configuration;
using Core.Engine;
using Game.Entities;
using Nomnom.RaycastVisualization;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.Player
{
    public delegate void PlayerInteractDelegate(InteractableState state);

    public class PlayerInteractionController : MonoBehaviour
    {
        public event PlayerInteractDelegate PlayerInteractEvent;

        private RaycastConfiguration _raycastConfiguration;
        private IInteractable _interactable;

        [SerializeField] private bool _debug;
        [SerializeField] private int _interactDistance;
        [SerializeField] private Transform _head;
        public bool AllowInteraction;

        private void Start()
        {
            _raycastConfiguration = Bootstrap.Resolve<GameSettings>().RaycastConfiguration;
        }

        private bool _interact;

        private void OnInteract(InputValue value)
        {
            _interact = value.isPressed;
        }

        private void Update()
        {
            if (!AllowInteraction) return;

            IInteractable currentInteractable = FetchCurrentInteractable();
            bool hasInteraction = currentInteractable != default;

            if (Keyboard.current.fKey.wasReleasedThisFrame) hasInteraction = false;
            //TODO: pasar a inputACTION
            if (_interactable != default)
            {
                NotifyState(InteractableState.INTERACTING);

                if (_interactable.IsDone(!hasInteraction))
                {
                    _interactable = default;
                    NotifyState(InteractableState.END_INTERACTION);
                    
                    return;
                }
            }
            if (hasInteraction && Keyboard.current.fKey.wasPressedThisFrame && currentInteractable.BeginInteraction())
            {
                _interactable = currentInteractable;
                NotifyState(InteractableState.BEGIN_INTERACTION);
            }
        }

        private IInteractable FetchCurrentInteractable()
        {
            RaycastHit hitInfo;
            Ray ray = new Ray(_head.position, _head.forward);
            if (!VisualPhysics.SphereCast(ray, .25f, out hitInfo, _interactDistance, _raycastConfiguration.InteractableLayer)) return default;

            return FindInteractableInFamily(hitInfo.transform);
        }

        private IInteractable FindInteractableInFamily(Transform t)
        {
            if (t == null) return default;
            if (t.TryGetComponent(out IInteractable i)) return i;
            return FindInteractableInFamily(t.parent);
        }

        private InteractableState _currentState { get; set; }

        private void NotifyState(InteractableState state)
        {
            if (_currentState == state) return;
            PlayerInteractEvent?.Invoke(state);
            _currentState = state;
        }
    }

    public enum InteractableState
    {
        BEGIN_INTERACTION,
        INTERACTING,
        END_INTERACTION
    }
}