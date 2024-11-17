using Core.Engine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Service
{
    public delegate void MessageDelegate(MessageParameters parameters);

    public class UIService : SceneService
    {
        public static event MessageDelegate CreateMessageEvent;

        public static void CreateMessage(MessageParameters parameters)
        {
            CreateMessageEvent?.Invoke(parameters);
        }

        internal override void Initialize()
        {
            SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
        }

        internal override void End()
        {
            SceneManager.UnloadSceneAsync(1);
        }
    }

    public struct MessageParameters
    {
        public string Text;
        public float Duration;
        public Color Color;
        public Color BackgroundColor;

        public MessageParameters(string text)
        {
            Text = text;
            Duration = 5;
            Color = Color.white;
            BackgroundColor = Color.black;
        }

        public MessageParameters(string text, float duration)
        {
            Text = text;
            Duration = duration;
            Color = Color.white;
            BackgroundColor = Color.black;
        }

        public MessageParameters(string text, float duration, Color color) : this(text, duration)
        {
            Color = color;
        }

        public MessageParameters(string text, float duration, Color color, Color backgroundColor) : this(text, duration)
        {
            Color = color;
            BackgroundColor = backgroundColor;
        }
    }
}