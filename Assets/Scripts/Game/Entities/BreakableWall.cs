using Game.Hit;
using UnityEngine;

namespace Game.Entities
{
    public class BreakableWall : MonoBehaviour, IDamageableFromExplosive
    {
        [SerializeField] private GameObject _default;
        [SerializeField] private GameObject _broken;
        [SerializeField] private Animator _animator;
        private bool _hasExploded;

        void IDamageableFromExplosive.NotifyDamage(float damage)
        {
            if (damage < 70)  return; 
            if (_hasExploded) return;
            _animator = GetComponent<Animator>();
            _default.SetActive(false);
            _animator.SetTrigger("BREAK");
            _broken.SetActive(true);
            _hasExploded = true;
        }
    }
}