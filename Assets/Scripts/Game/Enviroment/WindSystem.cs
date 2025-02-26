using Core.Engine;
using System.Collections;
using UnityEngine;

namespace Game.Enviroment
{
    public class WindService : GameGlobalService
    {
        private WindSystem _wind;
        public WindSystem Instance => _wind;

        internal override void Initialize()
        {
            _wind = new GameObject("WindSystem").AddComponent<WindSystem>();
        }
    }

    public class WindSystem : MonoBehaviour
    {
        private WindZone _zone;

        private void Start()
        {
            _zone = gameObject.AddComponent<WindZone>();
        }

        public Vector3 Direction => transform.forward;
        public float MainIntensity => _zone.windMain;

        private Quaternion _target;
        private float _resetTime = 10f;
        private float _time;

        private void Update()
        {
            _time += Time.deltaTime;
            {
                if (_time > _resetTime)
                {
                    _target = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    _time = 0;
                }
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, _target, Time.deltaTime * .5f);
        }
    }
}