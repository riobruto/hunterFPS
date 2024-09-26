using System.Collections;
using UnityEngine;

namespace Game.Player.Controllers
{
    public class PlayerCold : MonoBehaviour
    {
        private float _currentFrostLevel = 0;
        private float _frostingMultiplier = 1;
        private float _frostLevelMax = 100;
        private float _speedFrost = .3f;

        private void Update()
        {
            if (_currentFrostLevel > _frostLevelMax)
            {

                return;
            }
            _currentFrostLevel = Mathf.Clamp(_currentFrostLevel + (Time.deltaTime * _frostingMultiplier * _speedFrost), 0, _frostLevelMax + 1f);
        }

        public void SetFrostState(bool state)
        {
            _frostingMultiplier = state ? 1 : -1;
        }
    }
}