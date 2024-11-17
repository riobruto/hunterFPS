using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Minigames
{
    public enum PuzzleDirection
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    public class PuzzeLineNodeGame : MonoBehaviour
    {
        //TODO: JUEGO DE 3 EN RAYA
        [SerializeField] private PuzzleThreeLineGameSet _gameSet;

        private int _navigatorPointIndex;
        private bool _isMovingPiece;

        private Piece[] _pieces = new Piece[6];

        private Piece _currentPiece;
        private bool _won;

        private void Start()
        {
            _navigatorPointIndex = 0;
            _isMovingPiece = false;

            for (int i = 0; i < _pieces.Length; i++)
            {
                _pieces[i] = new Piece(i % 2);
                _pieces[i].Index = i;
            }
        }

        private void Update()
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ManagePieces();
                return;
            }

            if (Keyboard.current.wKey.wasPressedThisFrame) { Navigate(Vector2.up); return; }
            if (Keyboard.current.sKey.wasPressedThisFrame) { Navigate(-Vector2.up); return; }
            if (Keyboard.current.dKey.wasPressedThisFrame) { Navigate(Vector2.right); return; }
            if (Keyboard.current.aKey.wasPressedThisFrame) { Navigate(-Vector2.right); return; }
        }

        private void ManagePieces()
        {
            if (_isMovingPiece)
            {
                _currentPiece = null;
                _isMovingPiece = false;
                return;
            }
            _currentPiece = GetPieceInIndex(_navigatorPointIndex);
            if (_currentPiece != null) _isMovingPiece = true;
        }

        private void Navigate(Vector2 direction)
        {
            Vector2 current = _gameSet.Points[_navigatorPointIndex];

            Debug.DrawRay(current, direction, Color.yellow, 1);

            for (int i = 0; i < _gameSet.Points.Length; i++)
            {
                Debug.DrawRay(current, _gameSet.Points[i] - current, Color.red, 1);

                if (Vector3.Dot(_gameSet.Points[i] - current, direction) > 0)
                {
                    bool isValidLink = false;
                    foreach (var link in _gameSet.Links)
                    {
                        if (link.To == i && link.From == _navigatorPointIndex) { isValidLink = true; }
                        if (link.To == _navigatorPointIndex && link.From == i) { isValidLink = true; }

                        if (isValidLink)
                        {
                            if (_isMovingPiece)
                            {
                                if (GetPieceInIndex(i) == null)
                                {
                                    _currentPiece.Index = i;
                                }
                                CheckWinState();
                                _isMovingPiece = false;
                                return;
                            }
                            _navigatorPointIndex = i; return;
                        }
                    }
                }
            }
        }

        private Piece GetPieceInIndex(int i) => _pieces.FirstOrDefault(x => x.Index == i);

        private void CheckWinState()
        {
            foreach (WinLink win in _gameSet.WinPointsLinks)
            {
                foreach (int winIndex in win.IndexLine)
                {
                    if (GetPieceInIndex(winIndex) == null || GetPieceInIndex(winIndex).Type != win.Type) return;
                }
                _won = true;
            }
        }

        private void OnDrawGizmos()
        {
            for (int i = 0; i < _gameSet.Points.Length; i++)
            {
                Gizmos.DrawSphere(_gameSet.Points[i], 0.1f);
            }

            for (int k = 0; k < _gameSet.Links.Length; k++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawLine(_gameSet.Points[_gameSet.Links[k].From], _gameSet.Points[_gameSet.Links[k].To]);
            }

            foreach (Piece piece in _pieces)
            {
                if (piece == null) return;
                Gizmos.color = piece.Type == 0 ? Color.blue : Color.green;
                Gizmos.DrawSphere(_gameSet.Points[piece.Index], 0.08f);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_gameSet.Points[_navigatorPointIndex], 0.15f);
        }

        public class Piece
        {
            private int _index = 0;
            private int _type = 0;

            public Piece(int type)
            {
                _type = type;
            }

            public int Type => _type;

            public int Index
            {
                get { return _index; }
                set { _index = value; }
            }
        }
    }
}