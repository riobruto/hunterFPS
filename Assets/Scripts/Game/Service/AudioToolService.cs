using Core.Engine;
using Game.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Service
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

    public class AudioToolService : GameGlobalService
    {
        private static AudioMixer _mixer;

        public static RingBuffer<AudioBlendOneShot> OneShotsBuffer { get; private set; }
        public static RingBuffer<AudioSource> AudioSourceBuffer { get; private set; }

        internal override void Initialize()
        {
            _mixer = Resources.Load("Audio/GameAudioMixer") as AudioMixer;
            CreateOneshotBuffer();
            CreateSimpleBuffer();
        }

        private void CreateSimpleBuffer(){
            AudioSource[] oneShots = new AudioSource[32];
            AudioSourceBuffer = new RingBuffer<AudioSource>(oneShots);
            for (int i = 0; i < oneShots.Length; i++)
            {
                oneShots[i] = new GameObject("One shot audio").AddComponent<AudioSource>();
                oneShots[i].hideFlags = HideFlags.HideInHierarchy;
                GameObject.DontDestroyOnLoad(oneShots[i]);
            }
        }

        private void CreateOneshotBuffer() {
            AudioBlendOneShot[] oneShots = new AudioBlendOneShot[32];
            OneShotsBuffer = new RingBuffer<AudioBlendOneShot>(oneShots);
            for (int i = 0; i < oneShots.Length; i++)
            {
                oneShots[i] = new GameObject("Blend one shot audio").AddComponent<AudioBlendOneShot>();
                oneShots[i].Initialize();
                oneShots[i].hideFlags = HideFlags.HideInHierarchy;
                GameObject.DontDestroyOnLoad(oneShots[i]);
            }
        }

        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f, AudioChannels channel = AudioChannels.MASTER, float maxDistance = 30) {
            if (clip == null) return;
            AudioSource source = AudioSourceBuffer.GetNext();        
            source.transform.position = position;
            if (source.isPlaying) source.Stop();
            source.outputAudioMixerGroup = ResolveGroup(channel);
            source.clip = clip;
            source.spatialBlend = 1f;
            source.volume = volume;
            source.Play();
            source.minDistance = 1;
            source.maxDistance = maxDistance;
        }

        public static void PlayGunShot(AudioClip near, AudioClip far, Vector3 position, Vector3 listenerPosition, float blendDistance = 40, float volume = 1.0f, AudioChannels channel = AudioChannels.MASTER)
        {
            if (near == null) return;

            AudioBlendOneShot shot = OneShotsBuffer.GetNext();

            shot.transform.position = position;

            AudioSource audioSourcenear = shot.Near;
            audioSourcenear.outputAudioMixerGroup = ResolveGroup(channel);
            audioSourcenear.clip = near;
            audioSourcenear.minDistance = 1f;
            audioSourcenear.maxDistance = blendDistance;
            audioSourcenear.spatialBlend = 1f;
            audioSourcenear.volume = volume;
            audioSourcenear.pitch = Random.Range(0.9f, 1.1f);
            audioSourcenear.Play();
            audioSourcenear.rolloffMode = AudioRolloffMode.Linear;

            float distance = Vector3.Distance(position, listenerPosition);

            if (distance > blendDistance / 2)
            {
                if (far == null) return;

                AudioSource audioSourcefar = shot.Far;
                audioSourcefar.clip = far;
                audioSourcefar.outputAudioMixerGroup = ResolveGroup(channel);
                audioSourcefar.minDistance = blendDistance;
                audioSourcefar.maxDistance = distance * 2f;
                audioSourcefar.spatialBlend = 1f;
                audioSourcefar.volume = volume / Mathf.InverseLerp(0, blendDistance, distance);
                audioSourcefar.pitch = Random.Range(0.9f, 1.1f);
                audioSourcefar.rolloffMode = AudioRolloffMode.Linear;
                audioSourcefar.Play();

                return;
            }
        }

        private static AudioMixerGroup ResolveGroup(AudioChannels channel)
        {
            return _mixer.FindMatchingGroups(channel.ToString())[0];
        }

        public static AudioMixerGroup GetMixerGroup(AudioChannels channel) => ResolveGroup(channel);

        internal static void PlayUISound(AudioClip clip, float volume = 1){
            if (clip == null) return;
            AudioSource source = AudioSourceBuffer.GetNext();
            source.transform.position = Vector3.zero;            
            source.clip = clip;
            source.spatialBlend = 0;
            source.volume = volume;
            source.outputAudioMixerGroup = ResolveGroup(AudioChannels.UI);
            source.Play();
        }

        internal static void PlayPlayerSound(AudioClip clip, float volume = 1, float pitchFluctuation = 0){
            if (clip == null) return;            
            AudioSource audioSource = AudioSourceBuffer.GetNext();
            audioSource.clip = clip;
            audioSource.transform.position = Vector3.zero;
            audioSource.spatialBlend = 0;
            audioSource.volume = volume;
            audioSource.outputAudioMixerGroup = ResolveGroup(AudioChannels.PLAYER);
            audioSource.pitch = Random.Range(1f - pitchFluctuation, 1f + pitchFluctuation);
            audioSource.Play();
        }
    }
}