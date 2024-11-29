using Core.Engine;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Audio
{
    public enum AudioChannels
    {
        MASTER,
        PLAYER,
        TRAIN,
        AGENT,
        ENVIRONMENT,
        MUSIC,
        UI
    }

    public class AudioToolService : SceneService
    {
        private static AudioMixer _mixer;

        internal override void Initialize()
        {
            _mixer = Resources.Load("Audio/GameAudioMixer") as AudioMixer;
        }

        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f, AudioChannels channel = AudioChannels.MASTER)
        {
            if (clip == null) return;
            GameObject gameObject = new GameObject("One shot audio");
            gameObject.transform.position = position;
            AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
            audioSource.outputAudioMixerGroup = ResolveGroup(channel);
            audioSource.clip = clip;
            audioSource.spatialBlend = 1f;
            audioSource.volume = volume;
            audioSource.Play();
            Object.Destroy(gameObject, clip.length *
            (Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));
        }

        public static void PlayGunShot(AudioClip near, AudioClip far, Vector3 position, Vector3 listenerPosition, float blendDistance = 40, float volume = 1.0f, AudioChannels channel = AudioChannels.MASTER)
        {
            if (near == null) return;

            GameObject gameObject = new GameObject("One shot blend audio");
            gameObject.transform.position = position;

            AudioSource audioSourcenear = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
            audioSourcenear.outputAudioMixerGroup = ResolveGroup(channel);
            audioSourcenear.clip = near;
            audioSourcenear.minDistance = 1f;
            audioSourcenear.maxDistance = blendDistance;
            audioSourcenear.spatialBlend = 1f;
            audioSourcenear.volume = volume;
            audioSourcenear.pitch = Random.Range(0.9f,1.1f);
            audioSourcenear.Play();
            audioSourcenear.rolloffMode = AudioRolloffMode.Linear;

            float distance = Vector3.Distance(position, listenerPosition);

            if (distance > blendDistance / 2)
            {
                if (far == null) return;
                AudioSource audioSourcefar = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
                audioSourcefar.clip = far;
                audioSourcefar.outputAudioMixerGroup = ResolveGroup(channel);
                audioSourcefar.minDistance = blendDistance;
                audioSourcefar.maxDistance = blendDistance * 2f;
                audioSourcefar.spatialBlend = 1f;
                audioSourcefar.volume = volume / Mathf.InverseLerp(0, blendDistance, distance);
                audioSourcefar.pitch = Random.Range(0.9f,1.1f);
                audioSourcefar.rolloffMode = AudioRolloffMode.Linear;
                audioSourcefar.Play();
                Object.Destroy(gameObject, far.length *
                      (Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));
                return;
            }
            Object.Destroy(gameObject, near.length *
                     (Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));
        }

        private static AudioMixerGroup ResolveGroup(AudioChannels channel)
        {
            return _mixer.FindMatchingGroups(channel.ToString())[0];
        }

        public static AudioMixerGroup GetMixerGroup(AudioChannels channel) => ResolveGroup(channel);

        internal static void PlayUISound(AudioClip clip, float volume = 1)
        {
            GameObject gameObject = new GameObject("One UI audio");
            gameObject.transform.position = Vector3.zero;
            AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
            audioSource.clip = clip;
            audioSource.spatialBlend = 0;
            audioSource.volume = volume;
            audioSource.outputAudioMixerGroup = ResolveGroup(AudioChannels.UI);
            audioSource.Play();
            Object.Destroy(gameObject, clip.length *
            (Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));
        }
    }
}