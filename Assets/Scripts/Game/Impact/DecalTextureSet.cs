using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Impact
{
    [CreateAssetMenu(fileName = "New DecalSet", menuName = "Game/DecalSet")]
    public class DecalTextureSet : ScriptableObject
    {
        [SerializeField] private Texture2D[] _textures;
        [SerializeField] private Material _material;
        public Texture2D GetRandom() => _textures[Random.Range(0, _textures.Length)];
        public Texture2D[] Textures => _textures;
        public Material Material { get => _material; }
    }
}