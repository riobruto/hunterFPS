using Game.Service;
using Game.UI.Life;
using UnityEngine;

namespace Game.UI.Messages
{
    internal class HUDTextController : MonoBehaviour
    {
        [SerializeField] private GameObject _messagePrefab;
        [SerializeField] private GameObject _subtitlePrefab;

        private void OnEnable()
        {
            UIService.CreateMessageEvent += OnMessage;
            UIService.CreateSubtitleEvent += OnSubtitle;
        }


        private void OnDestroy()
        {

            UIService.CreateMessageEvent -= OnMessage;
            UIService.CreateSubtitleEvent -= OnSubtitle;
        }
        private void OnSubtitle(SubtitleParameters parameters)
        {
            CreateSubtitle(parameters);
        }

        private void OnMessage(MessageParameters parameters)
        {
            CreateMessage(parameters);
        }
      

        private GameObject CreateMessage(MessageParameters parameter)
        {
            GameObject message = Instantiate(_messagePrefab, GetComponent<RectTransform>());
            message.GetComponent<HUDMessage>().Set(parameter);
            return message;
        }

        private GameObject CreateSubtitle(SubtitleParameters parameters)
        {
            Canvas canvas = transform.root.GetComponent<Canvas>();
            GameObject subtitle = Instantiate(_subtitlePrefab, canvas.transform);
            subtitle.GetComponent<HUDFloatingSubtitle>().Create(parameters, canvas);
            return subtitle;
        }
    }
}