using Game.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.UI.Messages
{
    internal class HUDMessageController : MonoBehaviour
    {
        [SerializeField] private GameObject _messagePrefab;
              
        private void OnEnable()
        {
            UIService.CreateMessageEvent += OnMessage;
        }

        private void OnMessage(MessageParameters parameters)
        {
            CreateMessage(parameters);
        }

        private void OnDisable()
        {
            UIService.CreateMessageEvent -= OnMessage;
        }

        private GameObject CreateMessage(MessageParameters parameter)
        {
            GameObject message = Instantiate(_messagePrefab, GetComponent<RectTransform>());
            message.GetComponent<HUDMessage>().Set(parameter);
            return message;
        }
    }
}