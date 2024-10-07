using UnityEngine;

namespace Core.Configuration
{
    [CreateAssetMenu(fileName = nameof(RaycastConfiguration), menuName = "Game/Configuration/Raycast")]
    public class RaycastConfiguration : ScriptableObject
    {
        [SerializeField] private LayerMask _interactableLayer;
        [SerializeField] private LayerMask _actionLayer;
        [SerializeField] private LayerMask _ladderLayer;
        [SerializeField] private LayerMask _ignoreLayers;
        [SerializeField] private LayerMask _wallrunLayers;
        [SerializeField] private LayerMask _coverLayers;

        [SerializeField] private LayerMask _playerHitLayers;
        [SerializeField] private LayerMask _enemyHitLayers;
        [SerializeField] private LayerMask _grenadeHitLayers;

        public LayerMask InteractableLayer { get => _interactableLayer; }
        public LayerMask ActionLayer { get => _actionLayer; }
        public LayerMask LadderLayer { get => _ladderLayer; }
        public LayerMask IgnoreLayers { get => _ignoreLayers; }
        public LayerMask CoverLayers { get => _coverLayers; }
        public LayerMask WallrunLayers { get => _wallrunLayers; }
        public LayerMask PlayerGunLayers { get => _playerHitLayers; }
        public LayerMask GrenadeHitLayers { get => _grenadeHitLayers; }
        public LayerMask EnemyGunLayers { get => _enemyHitLayers; }
    }
}