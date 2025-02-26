using Core.Engine;
using Game.Hit;
using Game.Player.Weapon.Engines;
using Game.Service;
using Nomnom.RaycastVisualization;
using UnityEngine;

namespace Game.Entities.Projectiles
{
    public class BazookaProjectile : MonoBehaviour, IProjectile
    {
        private Vector3 _direction;
        private bool _active;

        [SerializeField] private float _speed;
        [SerializeField] private float _fallSpeed;
        [SerializeField] private float _noise;

        [SerializeField] private float _activationDistance;
        [SerializeField] private float _radius;
        [SerializeField] private float _maxDamage;

        private float _hitPos;
        private float _distanceTraveled;
        private float _lifeTime = 0;
        private Vector3 _lastPosition;
        private Vector3 _currentPosition;

        void IProjectile.Launch(Vector3 direction)
        {
           // Debug.Break();
            _active = true;
            _direction = direction;
            transform.forward = _direction;

            _lastPosition = _currentPosition = transform.position;
        }

        private void Start()
        {
            _distanceTraveled = 0;
        }

        private void Update()
        {
            if (!_active) return;

            _lastPosition = _currentPosition;
            //we update the transform
            transform.position += transform.forward * _speed * Time.deltaTime;
            transform.position += -Vector3.up * _distanceTraveled * _fallSpeed * Time.deltaTime;
            transform.forward += Random.insideUnitSphere * _noise * Time.deltaTime;
            _currentPosition = transform.position;
            _lifeTime += Time.deltaTime;
            _distanceTraveled += (_lastPosition - _currentPosition).magnitude;

            if (CheckCollision())
            {
                Explode();
                UpdateVisuals();
                _active = false;
            }
           
        }

        private void UpdateVisuals()
        {
            Debug.Log("Exploding");
            Bootstrap.Resolve<ImpactService>().System.ExplosionAtPosition(_lastPosition);
            //FindObjectOfType<CameraShakeController>().TriggerShake();
            Destroy(gameObject, 3);
        }

        private void Explode()
        {
            Vector3 explosionPos = transform.position;
            LayerMask mask = Bootstrap.Resolve<GameSettings>().RaycastConfiguration.GrenadeHitLayers;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, _radius, mask, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in colliders)
            {
                if (collider.gameObject.isStatic) continue;                
                if(Physics.Linecast(collider.transform.position, _lastPosition, out RaycastHit hit, mask, QueryTriggerInteraction.Ignore))  continue;                

                if (collider.TryGetComponent(out IDamageableFromExplosive damageable))
                {
                    damageable.NotifyDamage(CalculateDamage(collider), transform.position, (collider.transform.position - transform.position));
                }
                if (collider.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddExplosionForce(500, explosionPos, 10, 3.0F, ForceMode.Acceleration);
                }
            }
        }

        private float CalculateDamage(Collider collider)
        {
            return Mathf.Lerp(_maxDamage, 1, Mathf.InverseLerp(0, _radius, Vector3.Distance(collider.transform.position, transform.position)));
        }

        private bool CheckCollision()       {            
            if (!_active) return false;
            if (_lifeTime > 15) return true;
            return VisualPhysics.Linecast(_lastPosition, _currentPosition, out RaycastHit hit, Bootstrap.Resolve<GameSettings>().RaycastConfiguration.GrenadeHitLayers, QueryTriggerInteraction.Ignore);
        }
    }
}