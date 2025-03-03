using Game.Hit;
using Game.Player.Sound;
using Game.Service;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Entities
{
    [RequireComponent(typeof(PhysicalSurface), typeof(Rigidbody), typeof(Collider))]
    public class PhysicalEntity : MonoBehaviour, IDamagableFromHurtbox, IHittableFromWeapon
    {
        [SerializeField] private AudioClipGroup _collisionAudioClip;
        [SerializeField] private float _minimalMagnitudeToSound;

        void IHittableFromWeapon.Hit(HitWeaponEventPayload payload)
        {
            if(_collisionAudioClip != null)
                AudioToolService.PlayClipAtPoint(_collisionAudioClip.GetRandom(), payload.RaycastHit.point, 1, AudioChannels.ENVIRONMENT, 20);
        }

        void IDamagableFromHurtbox.NotifyDamage(float damage, Vector3 position, Vector3 direction)
        {
            if (_collisionAudioClip != null)
                AudioToolService.PlayClipAtPoint(_collisionAudioClip.GetRandom(), position, 1, AudioChannels.ENVIRONMENT, 20);
        }

        private void OnCollisionEnter(Collision collision)
        {
            float magnitudeOfImpact = collision.relativeVelocity.magnitude;
            if (magnitudeOfImpact > _minimalMagnitudeToSound)
            {
                if (_collisionAudioClip != null)
                    AudioToolService.PlayClipAtPoint(_collisionAudioClip.GetRandom(), collision.contacts[0].point, Mathf.InverseLerp(0, 10, magnitudeOfImpact), AudioChannels.ENVIRONMENT, 20);
            }
        }

       
    }
}