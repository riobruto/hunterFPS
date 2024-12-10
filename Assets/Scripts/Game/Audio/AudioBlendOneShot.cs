using System.Collections;
using UnityEngine;

namespace Game.Audio
{
    public class AudioBlendOneShot : MonoBehaviour
    {
        private AudioSource near;
        private AudioSource far;
        public AudioSource Near { get => near; }
        public AudioSource Far { get => far; }

        public void Initialize()
        {
            near = gameObject.AddComponent<AudioSource>();
            far = gameObject.AddComponent<AudioSource>();
        }
    }
}