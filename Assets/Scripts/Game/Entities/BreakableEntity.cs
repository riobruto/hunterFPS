using Game.Hit;
using Game.Player.Sound;
using Game.Service;
using UnityEngine;

namespace Game.Entities
{
    public delegate void BreakDelegate(BreakableEntity entity);

    public class BreakableEntity : MonoBehaviour, IHittableFromWeapon, IDamageableFromExplosive, IDamagableFromHurtbox
    {
        [SerializeField] private Transform[] _brokenMeshes;
        [SerializeField] private Transform _healthyMesh;

        [SerializeField] private bool _breakByShot;
        [SerializeField] private bool _breakByExplosion;
        [SerializeField] private bool _breakByDrop;
        [SerializeField] private bool _breakByKick;

        [Tooltip("This allows the entity to create the components of the broken pieces in runtime")]
        [SerializeField] private bool _generateGibs;

        [SerializeField] private float health = 1;
        private bool _broken;
        private Rigidbody _rigidbody;
        [SerializeField] private int _piecesDuration = 60;
        [SerializeField] private AudioClipGroup _breakAudio;

        public event BreakDelegate BreakEvent;

        public int PiecesDuration { get => _piecesDuration; set => _piecesDuration = value; }
        public bool Broken { get => _broken; }

        void IDamageableFromExplosive.NotifyDamage(float damage, Vector3 position, Vector3 explosionDirection)
        {
            
            Hurt(damage, position);
        }

        private void Hurt(float damage, Vector3 position)
        {
            health -= damage;
            _rigidbody.AddExplosionForce(5, position, 1);
            if (health <= 0 && !_broken)
            {
                Break();
            }
        }

        void IHittableFromWeapon.Hit(HitWeaponEventPayload payload)
        {
            //Bootstrap.Resolve<ImpactService>().System.ImpactAtPosition(payload.RaycastHit.point, payload.RaycastHit.normal);

            Hurt(payload.Damage, payload.Ray.direction);
        }

        private void Break()
        {
            _broken = true;
            _healthyMesh.gameObject.SetActive(false);
            GetComponent<MeshCollider>().enabled = false;
            AudioToolService.PlayClipAtPoint(_breakAudio.GetRandom(), transform.position, 1, AudioChannels.ENVIRONMENT);
            foreach (Transform t in _brokenMeshes)
            {
                t.gameObject.SetActive(true);
                var collider = t.gameObject.AddComponent<MeshCollider>();
                collider.convex = true;
                Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();
                rb.mass = _rigidbody.mass / _brokenMeshes.Length;
                rb.AddExplosionForce(5, transform.position, 1);
            }
            BreakEvent?.Invoke(this);
            Destroy(gameObject, _piecesDuration);
        }

        private void Start()
        {
            foreach (Transform t in _brokenMeshes)
            {
                t.gameObject.SetActive(false);
            }
            _rigidbody = GetComponent<Rigidbody>();
        }

        void IDamagableFromHurtbox.NotifyDamage(float damage, Vector3 position, Vector3 direction)
        {
            if (!_breakByKick) return;
            _rigidbody.AddForceAtPosition(direction, position);
            Hurt(damage,direction);
        }
    }
}