using Core.Engine;
using Game.Impact;
using Game.Service;
using System.Collections;
using UnityEngine;

namespace Game.Entities
{
    public class ExplosionSoundEntity : MonoBehaviour
    {
        private ImpactObject _impactObject;
        public void Set(ImpactObject impactObject) => _impactObject = impactObject;
        public void Play()
        {
            Vector3 playerPos = Bootstrap.Resolve<PlayerService>().PlayerCamera.transform.position;
            AudioToolService.PlayGunShot(_impactObject.Sound.GetRandom(), _impactObject.SoundFar.GetRandom(), transform.position, playerPos, 50, 1, AudioChannels.ENVIRONMENT);
        }
    }
}