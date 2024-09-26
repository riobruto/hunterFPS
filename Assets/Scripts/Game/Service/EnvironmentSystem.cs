using Core.Engine;
using UnityEngine;

namespace Game.Service
{
    public class EnvironmentService : SceneService
    {
        private EnvironmentSystem _system;

        internal override void Initialize()
        {
            _system = new GameObject("EnvironmentSystem").AddComponent<EnvironmentSystem>();
        }

        public EnvironmentSystem System => _system;
    }

    public class EnvironmentSystem : MonoBehaviour
    {
        private Material _skybox;
        private float _dayDurationInSeconds = 600;
        private float _currentDayTime;
        /*
        // Use this for initialization
        private void Start()
        {
            _currentDayTime = 1;
            _skybox = RenderSettings.skybox;
        }

        // Update is called once per frame
        private void Update()
        {
            _currentDayTime = Mathf.Sin(Time.realtimeSinceStartup / _dayDurationInSeconds) + 1 / 2;

            _skybox.SetFloat("_Exposure", (_currentDayTime / 4)+.01f);
            RenderSettings.ambientIntensity = _currentDayTime + .01f;
            RenderSettings.fogColor = Color.Lerp(Color.black,  Color.gray, _currentDayTime);
            Debug.Log(_currentDayTime);
        }*/
    }
}