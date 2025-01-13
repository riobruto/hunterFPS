using Core.Engine;
using Game.Audio;
using Game.Hit;
using Game.Player.Sound;
using Game.Service;
using Nomnom.RaycastVisualization;
using Player.Weapon.Interfaces;
using System.Collections;
using UnityEngine;

namespace Game.Entities.Grenades
{
    public class HEGrenade : MonoBehaviour, IGrenade, IDamagableFromHurtbox
    {
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private MeshRenderer _mesh;

        [SerializeField] private AudioClipGroup _explosion;
        [SerializeField] private AudioClipGroup _explosionFar;
        [SerializeField] private AudioClipGroup _bounce;
        private Rigidbody _rigidbody;

        Rigidbody IGrenade.Rigidbody => _rigidbody;

        void IGrenade.Trigger(int secondsRemaining)
        {
            _rigidbody = GetComponent<Rigidbody>();

            Debug.Log("Started HE nade with: " + secondsRemaining);
            StartCoroutine(Explode(secondsRemaining));
        }

        private IEnumerator Explode(int secondsRemaining)
        {
            yield return new WaitForSeconds(secondsRemaining);
            Vector3 explosionPos = transform.position;
            LayerMask mask = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.GrenadeHitLayers;

            Collider[] colliders = Physics.OverlapSphere(explosionPos, 10, mask);

            foreach (Collider hit in colliders)
            {
                if (hit.gameObject.isStatic) continue;

                if (VisualPhysics.Linecast(hit.ClosestPoint(transform.position), transform.position, mask)) continue;

                hit.TryGetComponent(out IDamageableFromExplosive damageable);

                if (damageable != null)
                {
                    damageable.NotifyDamage(Mathf.Lerp(80, 0, Mathf.InverseLerp(0, 8, Vector3.Distance(hit.ClosestPoint(transform.position), transform.position))));
                }

                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddExplosionForce(500, explosionPos, 10, 3.0F, ForceMode.Acceleration);
            }

            //Debug.Break();

            UpdateVisuals();
            Destroy(gameObject, 5);
            yield break;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude > 5)
            {
                AudioToolService.PlayClipAtPoint(_bounce.GetRandom(), transform.position, 1, AudioChannels.ENVIRONMENT, 8);
            }
        }

        private void UpdateVisuals()
        {
            Debug.Log("Exploding");
            _mesh.enabled = false;
            Bootstrap.Resolve<ImpactService>().System.ExplosionAtPosition(transform.position);
            //FindObjectOfType<CameraShakeController>().TriggerShake();
        }

        void IDamagableFromHurtbox.NotifyDamage(float damage, Vector3 position, Vector3 direction)
        {
            if(_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.AddForce(direction.normalized * 10f, ForceMode.VelocityChange);

        }
    }
}