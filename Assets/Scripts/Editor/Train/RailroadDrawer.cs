using Rail;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MyEditor.Train
{
    //[CustomEditor(typeof(Railroad), true)]
    [CanEditMultipleObjects]
    internal class RailroadDrawer : Editor
    {
        private List<Railroad> _selectedRailroads = new List<Railroad>();
        public override void OnInspectorGUI()
        {


        }
    }
}