using UnityEngine;

namespace Game.Player.Controllers
{
    internal class TrackerBone : MonoBehaviour
    {
        [SerializeField] private Transform _bone;
        public Transform Bone => _bone;
    }
}