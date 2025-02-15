using Core.Engine;
using Game.Player;
using Game.Service;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    internal class HUDInteract : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        private PlayerInteractionController _controller;

        private void Start()
        {
            _controller = Bootstrap.Resolve<PlayerService>().GetPlayerComponent<PlayerInteractionController>();
        }

        private void LateUpdate()
        {
            _icon.enabled = _controller.CanInteract;
        }
    }
}