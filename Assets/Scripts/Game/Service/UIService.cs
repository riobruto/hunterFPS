using Core.Engine;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Service
{
    public delegate void MessageDelegate(MessageParameters parameters);

    public delegate void SubtitleDelegate(SubtitleParameters parameters);

    public class UIService : GameGlobalService
    {
        private GameObject _ui;
        public GameObject UIGameObject;
        public static event MessageDelegate CreateMessageEvent;
        public static event SubtitleDelegate CreateSubtitleEvent;

        public static void CreateMessage(MessageParameters parameters)
        {
            CreateMessageEvent?.Invoke(parameters);
        }

        internal static void CreateMessage(string text)
        {
            MessageParameters parameters = new MessageParameters(text, 4, Color.white, new Color(0, 0, 0, .5f));
            CreateMessageEvent?.Invoke(parameters);
        }

        internal static void CreateMessage(string text, float duration)
        {
            MessageParameters parameters = new MessageParameters(text, duration, Color.white, new Color(0, 0, 0, .5f));

            CreateMessageEvent?.Invoke(parameters);
        }

        public static void CreateSubtitle(SubtitleParameters parameters)
        {
            CreateSubtitleEvent?.Invoke(parameters);
        }

        internal override void Initialize()
        {
            PlayerService.PlayerSpawnEvent += OnPlayerSpawn;
            ///SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
        }

        private void OnPlayerSpawn(GameObject player)
        {
            //USE ADDRESSABLES
            Addressables.InstantiateAsync("Assets/Prefabs/UI/UI_Complete.prefab").Completed += (x) =>
            {
                _ui = x.Result;
                GameObject.DontDestroyOnLoad(_ui);
            };           
           
        }

        internal override void End()
        {
            GameObject.Destroy(_ui);
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

    public struct SubtitleParameters
    {
        public string Name;
        public string Content;
        public float Duration;
        public Vector3 Location;
        public bool FollowTransform;
        public Transform Transform;
        public Vector3 Offset;
    }
}