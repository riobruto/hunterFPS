using Game.Hit;
using System.Collections;
using UnityEngine;

namespace Life.Behaviors
{
    public class AgentMeeleeWeapon : MonoBehaviour
    {
        [SerializeField] private AnimationHurtbox _hurtbox;

        public void BeginScanFromAnimation(int animationFrames)
        {
            _hurtbox.StartScan(animationFrames);
        }
    }
}