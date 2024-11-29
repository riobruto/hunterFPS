using System;
using UnityEngine;

namespace Game.Impact
{
    [Serializable]
    internal class BulletCoordinate
    {
        [SerializeField] private SurfaceType _surfaceType;

        [Tooltip("The root of the group of 4 holes")]
        [SerializeField] private Vector2Int _atlasCoordinates;

        public Vector2Int AtlasCoordinates { get => _atlasCoordinates; }
        internal SurfaceType SurfaceType { get => _surfaceType; }
    }
}