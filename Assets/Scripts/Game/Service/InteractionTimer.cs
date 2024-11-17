using Core.Engine;
using System;
using UnityEngine;

namespace Game.Service
{
    internal class InteractionTimerService : SceneService
    {
        public InteractionTimer Instance;

        internal override void Initialize()
        {
            GameObject gameObject = new GameObject("InteractionTimer");
            Instance = gameObject.AddComponent<InteractionTimer>();
            Instance.Initialize();
        }

        internal override void End()
        {
            Instance.Discard();
        }
    }

    public class InteractionTimer : MonoBehaviour
    {
        private GameObject _timer;
        private MeshRenderer _renderer;

        public void Initialize()
        {
            _timer = Instantiate(Resources.Load("VisualItems/InteractionTimerObject")) as GameObject;
            _renderer = _timer.GetComponent<MeshRenderer>();
        }

        public void SetTimer(Vector3 position)
        {
            _timer.transform.position = position;
            _timer.hideFlags = HideFlags.HideInHierarchy;
        }

        public void HideTimer()
        {
            _renderer.material.SetFloat("_alpha", 0);
            _renderer.material.SetFloat("_tvalue", 0);
        }

        public void UpdateTimer(float time, float maxTime, bool display)
        {
            _renderer.material.SetFloat("_alpha", Mathf.Lerp(_renderer.material.GetFloat("_alpha"), display ? 1 : 0, Time.deltaTime * 10f));
            _renderer.material.SetFloat("_tvalue", time / maxTime);
        }
        public void UpdateTimer(float time, float maxTime, bool display, Vector3 position)
        {
            _renderer.material.SetFloat("_alpha", Mathf.Lerp(_renderer.material.GetFloat("_alpha"), display ? 1 : 0, Time.deltaTime * 10f));
            _renderer.material.SetFloat("_tvalue", time / maxTime);
            _timer.transform.position = position;
        }
        internal void Discard()
        {
            Destroy(_timer);
            Destroy(gameObject);
        }
    }
}