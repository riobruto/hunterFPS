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
            CalculateHits();
            UpdateVisuals();
            Destroy(gameObject, 1);

            yield return null;
        }

        private void CalculateHits()
        {
            Vector3 explosionPos = transform.position;
            LayerMask mask = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.GrenadeHitLayers;
            Collider[] colliders = VisualPhysics.OverlapSphere(explosionPos, 10, mask, QueryTriggerInteraction.Ignore);

            foreach (Collider collider in colliders)
            {
                if (collider.gameObject.isStatic) continue;

                VisualPhysics.Linecast(collider.ClosestPoint(transform.position), transform.position, out RaycastHit hit, mask, QueryTriggerInteraction.Ignore);
                if (hit.transform == null) continue;
                if (hit.transform != transform) continue;


                if (collider.TryGetComponent(out IDamageableFromExplosive damageable))
                {
                    damageable.NotifyDamage(CalculateDamage(collider), transform.position,(collider.transform.position - transform.position));
                }
                if (collider.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddExplosionForce(500, explosionPos, 10, 3.0F, ForceMode.Acceleration);
                }
            }
        }

        private float CalculateDamage(Collider collider)
        {
            return Mathf.Lerp(200, 0, Mathf.InverseLerp(0, 6, Vector3.Distance(collider.ClosestPoint(transform.position), transform.position)));
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
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.AddForce(direction.normalized * 10f, ForceMode.VelocityChange);
        }
    }
}