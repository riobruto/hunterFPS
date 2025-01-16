using Core.Meshing;
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Splines;
#if UNITY_EDITOR
namespace Rail.Visual
{

	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[ExecuteInEditMode]
	public class PowerRailExtrude : SplineComponent
	{
		[Header("Test Fields")]
		[SerializeField] private float testTime;

		[SerializeField] private bool _debugAxis;

		[SerializeField] private int edgeRingCount = 8;
		[SerializeField] private int resolutionDividend = 1;

		[SerializeField] private RailShapeMesh2D _catenaryCableShape;
		[SerializeField] private Material _catenaryCableMaterial;


		//[SerializeField] private SplineContainer _splineContainer;
		[SerializeField] private Railroad _rail;
		[SerializeField] private bool _flipPosition;
		private Mesh _cableMesh;


		[SerializeField] private bool _autoRegeneration;

		float _length;
		float _previousLength;

		private void Update()
		{
			if (!_autoRegeneration) return;

			_length = _rail.ActiveSpline.GetLength();
			if (_length != _previousLength)
			{
				GenerateMesh();
				_previousLength = _length;
			}

		}
		private void OnEnable()
		{
			_catenaryCableShape.MeshUpdated += OnMeshUpdated;
		}
		private void OnDisable()
		{
			_catenaryCableShape.MeshUpdated -= OnMeshUpdated;
		}
		private void OnMeshUpdated()
		{
			GenerateMesh();
		}

		#region Generate Methods

		[ContextMenu("Generate")]
		private void GenerateMesh()
		{
			//_splineContainer = GetComponent<SplineContainer>();
			_rail = GetComponentInParent<Railroad>();
			_cableMesh = new Mesh();
			_cableMesh.name = "RailCableSegment";
			_cableMesh.Clear();
			List<Vector3> verts = new List<Vector3>();
			List<Vector2> uvs = new List<Vector2>();
			//Vertices

			edgeRingCount = (int)_rail.ActiveSpline.GetLength() / resolutionDividend;

			for (int ring = 0; ring < edgeRingCount; ring++)
			{
				float t = ring / (edgeRingCount - 1f);

				//move this to a method that returns the OrientatedPoint --------------

				RailData data = _rail.GetRailDataFromTime(t);
				Vector3 currentPos = data.NearestPosition;
				Vector3 currentForward = data.Tangent;
				Vector3 currentUp = data.Up;
				Vector3 currentRight = -Vector3.Cross(currentForward, currentUp).normalized;

				OrientatedPoint p = new OrientatedPoint(currentPos, currentForward, currentUp);

				//---------------------------------------------------------------------------

				for (int i = 0; i < _catenaryCableShape.VertexCount; i++)
				{
					verts.Add(transform.InverseTransformPoint(p.LocalToWorld(_catenaryCableShape.Vertices[i].point) + UnityEngine.Random.insideUnitSphere * 0.01f));
					uvs.Add(new Vector2(_catenaryCableShape.Vertices[i].u, t * _rail.ActiveSpline.GetLength()));
				}
			}

			//Triangles
			List<int> triIndices = new List<int>();

			for (int ring = 0; ring < edgeRingCount - 1; ring++)
			{
				int rootIndex = ring * _catenaryCableShape.VertexCount;
				int rootIndexNext = (ring + 1) * _catenaryCableShape.VertexCount;

				for (int line = 0; line < _catenaryCableShape.LineCount; line += 2)
				{
					int lineIndexA = _catenaryCableShape.LineIndices[line];
					int lineIndexB = _catenaryCableShape.LineIndices[line + 1];

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

			_cableMesh.SetVertices(verts);
			_cableMesh.SetTriangles(triIndices, 0);
			_cableMesh.RecalculateTangents();
			_cableMesh.RecalculateNormals();
			_cableMesh.SetUVs(0, uvs);
			
			GetComponent<MeshFilter>().sharedMesh = _cableMesh;
			List<Material> railMaterials = new List<Material> { _catenaryCableMaterial };
			GetComponent<MeshRenderer>().sharedMaterials = railMaterials.ToArray();

		}

	
		#endregion Generate Methods


		private void OnDrawGizmos()
		{
			//Get Vector Data from time

			/*

            for (int i = 0; i < _railShape.Vertices.Length; i++)
            {
                Gizmos.DrawSphere(p.LocalToWorld(_railShape.Vertices[i].point), 0.0125f);
            }
            for (int i = 0; i < _railShape.LineCount; i++)
            {
               Gizmos.DrawLine(p.LocalToWorld(_railShape.Vertices[i].point), p.LocalToWorld(_railShape.Vertices[i + 1].point));
            }*/
		}
	}

}
#endif