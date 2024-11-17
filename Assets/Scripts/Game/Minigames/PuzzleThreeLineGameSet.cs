using System;
using UnityEngine;

namespace Game.Minigames
{
    [CreateAssetMenu(fileName = "ThreeLinePuzzle Game set", menuName = "Game/Puzzle/ThreeLinePuzze")]
    public class PuzzleThreeLineGameSet : ScriptableObject
    {
        [SerializeField] private Vector2[] _positions;
        [SerializeField] private PuzzlePointLink[] _links;
        [SerializeField] private WinLink[] _winLinks;
        public Vector2[] Points => _positions;
        public PuzzlePointLink[] Links => _links;
        public WinLink[] WinPointsLinks => _winLinks;
    }

    [Serializable]
    public struct PuzzlePointLink
    {
        [SerializeField] private int from, to;

        public int From
        {
            get { return from; }
        }

        public int To
        {
            get { return to; }
        }
    }

    [Serializable]
    public struct WinLink
    {
        [SerializeField] private int[] _indexLine;
        [SerializeField] private int _type;

        public int[] IndexLine { get => _indexLine; }
        public int Type { get => _type; }
    }
}