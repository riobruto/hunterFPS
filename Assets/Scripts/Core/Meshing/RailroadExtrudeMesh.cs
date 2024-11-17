using Rail;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Splines;

namespace Core.Meshing
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class RailroadExtrudeMesh : SplineComponent
    {
        [Header("Test Fields")]
        [SerializeField] private float testTime;

        [SerializeField] private bool _debugAxis;
        [SerializeField] private int edgeRingCount = 8;
        [SerializeField] private int _resolutionDividend = 1;
        [SerializeField] private RailShapeMesh2D _railShape;
        [SerializeField] private Material _railMaterial;
        [SerializeField] private RailShapeMesh2D _ballastShape;
        [SerializeField] private Material _ballastMaterial;
        [SerializeField] private Railroad _rail;

        private Mesh _railMesh;
        private Mesh _ballastMesh;

        [SerializeField] private bool _autoRegeneration;
        private bool _generateCollider;

        #region Generate Methods

        public void GenerateRailMesh()
        {
            _rail = GetComponentInParent<Railroad>();
            _railMesh = new Mesh();
            _railMesh.name = "RailMeshSegment";
            _railMesh.Clear();
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            edgeRingCount = (int)_rail.ActiveSpline.GetLength() / _resolutionDividend;

            for (int ring = 0; ring < edgeRingCount; ring++)
            {
                float t = ring / (edgeRingCount - 1f);

                //move this to a method that returns the OrientatedPoint --------------

                RailData data = _rail.GetRailDataFromTime(t);
                Vector3 currentPos = data.NearestPosition;
                Vector3 currentForward = data.Tangent;
                Vector3 currentUp = data.Up;

                OrientatedPoint p = new(currentPos, currentForward, currentUp);

                //---------------------------------------------------------------------------

                for (int i = 0; i < _railShape.VertexCount; i++)
                {
                    verts.Add(p.LocalToWorld(_railShape.Vertices[i].point) + UnityEngine.Random.insideUnitSphere * 0.01f);
                    uvs.Add(new Vector2(_railShape.Vertices[i].u, t * _rail.ActiveSpline.GetLength()));
                }
            }

            //Triangles
            List<int> triIndices = new List<int>();

            for (int ring = 0; ring < edgeRingCount - 1; ring++)
            {
                int rootIndex = ring * _railShape.VertexCount;
                int rootIndexNext = (ring + 1) * _railShape.VertexCount;

                for (int line = 0; line < _railShape.LineCount; line += 2)
                {
                    int lineIndexA = _railShape.LineIndices[line];
                    int lineIndexB = _railShape.LineIndices[line + 1];

                    int currentA = rootIndex + lineIndexA;
                    int currentB = rootIndex + lineIndexB;
                    int nextA = rootIndexNext + lineIndexA;
                    int nextB = rootIndexNext + lineIndexB;

                    triIndices.Add(currentA);
                    triIndices.Add(nextA);
                    triIndices.Add(nextB);
                    triIndices.Add(currentA);
                    triIndices.Add(nextB);
                    triIndices.Add(currentB);
                }
            }

            _railMesh.SetVertices(verts);
            _railMesh.SetTriangles(triIndices, 0);
            _railMesh.RecalculateTangents();
            _railMesh.RecalculateNormals();
            _railMesh.SetUVs(0, uvs);

            CombineInstance[] meshes = new CombineInstance[2];
            meshes[0].mesh = _railMesh;
            meshes[0].subMeshIndex = 0;
            meshes[0].transform = transform.worldToLocalMatrix;
            //terrible, esto deberia respetar la posicion de los rieles con la posicion local y global. nefasto

            meshes[1].mesh = GenerateBallastMesh();
            meshes[1].subMeshIndex = 0;
            meshes[1].subMeshIndex = 0;
            meshes[1].transform = transform.worldToLocalMatrix;
            Mesh combinedMeshes = new Mesh();
            combinedMeshes.subMeshCount = 2;
            combinedMeshes.name = "RailBallast";
            combinedMeshes.CombineMeshes(meshes, false);
            combinedMeshes.RecalculateBounds();

            //combinedMeshes.RecalculateTangents();
            //combinedMeshes.RecalculateNormals();

            GetComponent<MeshFilter>().sharedMesh = combinedMeshes;
            List<Material> railMaterials = new List<Material> { _railMaterial, _ballastMaterial };
            GetComponent<MeshRenderer>().sharedMaterials = railMaterials.ToArray();
            //GetComponent<MeshRenderer>().SetSharedMaterials(railMaterials);

            if (_generateCollider) SetCollider();
        }

        private Mesh GenerateBallastMesh()
        {
            Mesh ballastMesh = new Mesh();
            ballastMesh.Clear();

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            //Vertices
            edgeRingCount = (int)_rail.ActiveSpline.GetLength() / _resolutionDividend;
            for (int ring = 0; ring < edgeRingCount; ring++)
            {
                float t = ring / (edgeRingCount - 1f);
                RailData data = _rail.GetRailDataFromTime(t);
                Vector3 currentPos = data.NearestPosition;
                Vector3 currentForward = data.Tangent;
                Vector3 currentUp = data.Up;
                OrientatedPoint p = new OrientatedPoint(currentPos, currentForward, currentUp);

                //---------------------------------------------------------------------------

                for (int i = 0; i < _ballastShape.VertexCount; i++)
                {
                    verts.Add((p.LocalToWorld(_ballastShape.Vertices[i].point) + UnityEngine.Random.insideUnitSphere * 0.01f));
                    uvs.Add(new Vector2(_ballastShape.Vertices[i].u, t * _rail.ActiveSpline.GetLength()));
                }
            }

            //Triangles
            List<int> triIndices = new List<int>();

            for (int ring = 0; ring < edgeRingCount - 1; ring++)
            {
                int rootIndex = ring * _ballastShape.VertexCount;
                int rootIndexNext = (ring + 1) * _ballastShape.VertexCount;

                for (int line = 0; line < _ballastShape.LineCount; line += 2)
                {
                    int lineIndexA = _ballastShape.LineIndices[line];
                    int lineIndexB = _ballastShape.LineIndices[line + 1];

                    int currentA = rootIndex + lineIndexA;
                    int currentB = rootIndex + lineIndexB;
                    int nextA = rootIndexNext + lineIndexA;
                    int nextB = rootIndexNext + lineIndexB;

                    triIndices.Add(currentA);
                    triIndices.Add(nextA);
                    triIndices.Add(nextB);
                    triIndices.Add(currentA);
                    triIndices.Add(nextB);
                    triIndices.Add(currentB);
                }
            }

            ballastMesh.SetVertices(verts);
            ballastMesh.SetTriangles(triIndices, 0);
            ballastMesh.RecalculateTangents();
            ballastMesh.RecalculateNormals();
            ballastMesh.SetUVs(0, uvs);
            return ballastMesh;
        }

        #endregion Generate Methods

        private void SetCollider()
        {
            gameObject.AddComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
        }

        public void SetParamenters(RailShapeMesh2D railShape, Material railMaterial, RailShapeMesh2D ballastShape, Material ballastMaterial, int resolution, bool generateCollider)
        {
            _railShape = railShape;
            _railMaterial = railMaterial;
            _ballastShape = ballastShape;
            _ballastMaterial = ballastMaterial;
            _resolutionDividend = resolution;
            _generateCollider = generateCollider;
        }
    }
}