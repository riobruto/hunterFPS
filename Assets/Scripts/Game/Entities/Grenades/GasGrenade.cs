using Core.Engine;
using Game.Enviroment;
using Game.Hit;

using Nomnom.RaycastVisualization;
using Player.Weapon.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Entities.Grenades
{
    public class GasGrenade : MonoBehaviour, IGrenade
    {
        private bool _canEmanate = false;
        private float _emanatingTime = 20;
        private float _time;

        private WindSystem _wind;
        private List<GasGrenadeHurtbox> _hurtList = new List<GasGrenadeHurtbox>();
        private ParticleSystem _particle;

        private float _tickTime;
        private float _timeBtwTicks = 0.1f;

        private IDamageableFromGas[] _gasDamageables;
        private Rigidbody _rigidbody;

        Rigidbody IGrenade.Rigidbody => _rigidbody;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        void IGrenade.Trigger(int secondsRemaining)
        {
            _particle = GetComponentInChildren<ParticleSystem>();
            Debug.Log("Started HE nade with: " + secondsRemaining);
            _time = 0;
            StartCoroutine(Explode(secondsRemaining));
            var gasDamageables = FindObjectsOfType<MonoBehaviour>().OfType<IDamageableFromGas>();
        }

        private IEnumerator Explode(int secondsRemaining)
        {
            yield return new WaitForSeconds(secondsRemaining);
            _particle.Play();
            Debug.Log("Exploding");
            _wind = Bootstrap.Resolve<WindService>().Instance;
            _canEmanate = true;
            StartCoroutine(CreateSpheres());
        }

        private void Update()
        {
            if (!_canEmanate) return;
            _time += Time.deltaTime;
            Debug.Log("Emanating");

            if (_time >= _emanatingTime)
            {
                _canEmanate = false;
                Debug.Log("Ended Emanating");
                _particle.Stop();
                Destroy(gameObject, 10);
            }

            _tickTime += Time.deltaTime;

            if (_tickTime > _timeBtwTicks)
            {
                UpdateNade();
                _tickTime = 0;
            }
        }

        private void UpdateNade()
        {
            foreach (GasGrenadeHurtbox hurt in _hurtList)
            {
                //Iterar Por Cada Hurtbox
                Collider[] colliders = VisualPhysics.OverlapSphere(hurt.Position, Mathf.Clamp(_time - hurt.StartTime, 1, 10));

                Vector3 colliderRedir = Vector3.zero;
                //TODO: Optimize this crappp
                if (colliders.Length > 0)
                {
                    foreach (Collider c in colliders)
                    {
                        if (c.gameObject.isStatic)
                        {
                            Vector3 closestPoint = c.ClosestPoint(hurt.Position);
                            Vector3 surfaceNormal = closestPoint - hurt.Position;
                            colliderRedir += surfaceNormal;
                        }

                        c.TryGetComponent(out IDamageableFromGas damageable);
                        if (damageable != null)
                        {
                            damageable.NotifyDamage(1);
                        }
                    }
                    Debug.DrawRay(transform.position, -colliderRedir.normalized);
                }

                hurt.Update((_wind.Direction * _wind.MainIntensity * 2) + -colliderRedir);
            }
        }

        private IEnumerator CreateSpheres()
        {
            while (_canEmanate)
            {
                _hurtList.Add(new GasGrenadeHurtbox(transform.position + Random.insideUnitSphere + Vector3.up, _time));
                Debug.Log("Creating");
                yield return new WaitForSeconds(1f);
            }
            yield break;
        }

        private class GasGrenadeHurtbox
        {
            private Vector3 _position;
            private float _startTime;

            public GasGrenadeHurtbox(Vector3 position, float lifetime)
            {
                _position = position;
                _startTime = lifetime;
            }

            public float StartTime { get => _startTime; }
            public Vector3 Position { get => _position; }

            public void Update(Vector3 direction)
            {
                _position = _position + (direction * Time.deltaTime);
            }
        }
    }
}