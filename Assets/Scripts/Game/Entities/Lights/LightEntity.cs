using System;
using Core.Engine;
using Game.Service;
using UnityEngine;

namespace Game.Entities.Lights
{
    public class LightEntity : MonoBehaviour
    {
        [SerializeField] private Material _emmisiveSurface;
        [SerializeField] private MeshRenderer _rendeder;
        [SerializeField] private Light _light;
        [SerializeField] private float _intensity;
        [SerializeField] private bool _startState;
        private Camera _camera;

        private void Start()
        {
            if (PlayerService.Active) { _camera = Bootstrap.Resolve<PlayerService>().PlayerCamera; }
            else { PlayerService.PlayerSpawnEvent += OnPlayerSpawn; }


            _rendeder.material = new(_emmisiveSurface);
            if(!gameObject.isStatic) SetLightState(_startState);
        }

        private void OnPlayerSpawn(GameObject player){
            _camera = Bootstrap.Resolve<PlayerService>().PlayerCamera;
        }

        public void SetLightState(bool value)
        {
            if (value)
            {
                _rendeder.material.SetFloat("_Emission_Intensity", _intensity);
                _light.intensity = _intensity;
                return;
            }
            _rendeder.material.SetFloat("_Emission_Intensity", 0);
            _light.intensity = 0;
        }

        private void OnDestroy()
        {
            PlayerService.PlayerSpawnEvent -= OnPlayerSpawn;
        }
    }
}