using Game.Service;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Game.Player.Controllers
{
    public class PlayerStunController : MonoBehaviour
    {
        [SerializeField] private Transform _flash;
        [SerializeField] private Material _stunMaterial;
        [SerializeField] private AnimationCurve _stunLevelCurve;
        [SerializeField] private AnimationCurve _stunLevelAmount;
        [SerializeField] private AnimationCurve _stunLevelDeafen;
        [SerializeField] private AudioClip _earRing;
        [SerializeField] private AudioMixer _mixer;
        private float _time;
        private float _shockTime;
        [SerializeField] private float _duration;
        private Vector2 _displacement;

        // Use this for initialization
        private void Start()
        {
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            _time = Mathf.Clamp(_time + (Time.deltaTime / _duration), -float.Epsilon, 1f);
            _shockTime = Mathf.Clamp(_shockTime + (Time.deltaTime / _duration), -float.Epsilon, 1f);

            _stunMaterial.SetFloat("_StunLevel", _stunLevelCurve.Evaluate(_time));
            _stunMaterial.SetFloat("_Amount", _stunLevelAmount.Evaluate(_time));
            _stunMaterial.SetVector("_Displacement", Vector2.Lerp(_displacement, Vector3.zero, _time));

            _mixer.SetFloat("3DCutoff", _stunLevelDeafen.Evaluate(_shockTime) * 22000f);
            _mixer.SetFloat("3DVolume", Mathf.Log10(_stunLevelDeafen.Evaluate(_shockTime)) * 20f);
        }

        [ContextMenu("Stun")]
        public void Stun(Vector3 from)
        {
            float distance = Vector3.Distance(transform.position, from);
            float time = Mathf.InverseLerp(5f, 25f, distance);
            _time = time;
        }

        [ContextMenu("Shock")]
        public void Shock(Vector3 from)
        {
            float distance = Vector3.Distance(transform.position, from);
            float time = Mathf.InverseLerp(2.5f, 10, distance);
            _shockTime = time;
            Debug.Log(_shockTime);
            AudioToolService.PlayUISound(_earRing, Mathf.Clamp01((_shockTime-1)*-1f));
        }
    }
}