using Core.Meshing;
using UnityEngine;

namespace MyEditor.Train.Data
{
    [CreateAssetMenu(fileName = "New RailResources", menuName = "Editor/Rail/Resources")]
    internal class RailResources : ScriptableObject
    {
        [SerializeField] private GameObject _rail;
        [SerializeField] private GameObject _junctionInLeft;
        [SerializeField] private GameObject _junctionInRight;
        [SerializeField] private GameObject _junctionOutLeft;
        [SerializeField] private GameObject _junctionOutRight;

        [SerializeField] private RailShapeMesh2D _railmesh;
        [SerializeField] private RailShapeMesh2D _ballastMesh;
        [SerializeField] private Material _railMat;
        [SerializeField] private Material _ballastMat;
        public GameObject Rail { get => _rail; }
        public GameObject JunctionInLeft { get => _junctionInLeft; }
        public GameObject JunctionInRight { get => _junctionInRight; }
        public GameObject JunctionOutLeft { get => _junctionOutLeft; }
        public GameObject JunctionOutRight { get => _junctionOutRight; }

        public RailShapeMesh2D Railmesh { get => _railmesh; }
        public RailShapeMesh2D BallastMesh { get => _ballastMesh; }
        public Material RailMaterial { get => _railMat; }
        public Material BallastMaterial { get => _ballastMat; }
    }
}