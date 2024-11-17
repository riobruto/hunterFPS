using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Meshing
{
    [CreateAssetMenu(fileName = "new Mesh2D", menuName = " Mesh2D")]
    public class RailShapeMesh2D : ScriptableObject
    {
        
        public Vertex[] Vertices;
        public int[] LineIndices;

        public int VertexCount => Vertices.Length;
        public int LineCount => LineIndices.Length;

        [Serializable]
        public class Vertex
        {
            public Vector2 point;
            public Vector2 normal;
            public float u; 
        }
        public Action MeshUpdated;

        /*
		private void OnValidate()
		{
            MeshUpdated.Invoke();
		}
        */
	}

   

    


}

