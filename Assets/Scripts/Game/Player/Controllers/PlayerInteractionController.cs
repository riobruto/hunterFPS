using Core.Configuration;
using Core.Engine;
using Game.Entities;
using Nomnom.RaycastVisualization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public delegate void PlayerInteractDelegate(InteractableState state);

    public class PlayerInteractionController : MonoBehaviour
    {
        public event PlayerInteractDelegate PlayerInteractEvent;

        private RaycastConfiguration _raycastConfiguration;
        private IInteractable _interactable;
        private Vector3 _interactHit;
        private bool _hasInteraction;

        [SerializeField] private bool _debug;
        [SerializeField] private int _interactDistance;
        [SerializeField] private Transform _head;

        public bool AllowInteraction;

        public bool CanInteract
        {
            get
            {
                if (_interactable != default && !_interactable.CanInteract()) return false;
                return AllowInteraction && _hasInteraction;
            }
        }

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
            _hasInteraction = currentInteractable != default;

            if (Keyboard.current.fKey.wasReleasedThisFrame) _hasInteraction = false;
            //TODO: pasar a inputACTION
            if (_interactable != default)
            {
                NotifyState(InteractableState.INTERACTING);

                if (_interactable.IsDone(!_hasInteraction))
                {
                    _interactable = default;
                    NotifyState(InteractableState.END_INTERACTION);

                    return;
                }
            }
            if (_hasInteraction && Keyboard.current.fKey.wasPressedThisFrame && currentInteractable.BeginInteraction(_interactHit))
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
            _interactHit = hitInfo.point;
            return FindInteractableInFamily(hitInfo.collider.gameObject.transform);
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