using Game.Train;
using Rail;
using UnityEditor;
using UnityEngine;

namespace MyEditor.Train
{
    [CustomEditor(typeof(TrainBase), true)]
    [CanEditMultipleObjects]
    public class TrainPartDrawer : Editor
    {
        private TrainBase controller;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            controller = target as TrainBase;

            using (new EditorGUILayout.VerticalScope())
            {
                if (controller.SpawnRailroad != null)
                {
                    //Rect r = EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button($"Set in current spawn rail: '{controller.SpawnRailroad.name}' "))
                    {
                        RailData data = controller.SpawnRailroad.GetRailDataFromPoint(controller.transform.position);

                        Vector3 forward = Vector3.Normalize(data.Tangent);
                        if (Vector3.Dot(forward, controller.transform.forward) < 0)
                        {
                            forward = -forward;
                        }
                        controller.transform.position = data.NearestPosition;
                        controller.transform.forward = forward;
                    }
                    if (GUILayout.Button($"Flip Direction"))
                    {
                      
                        controller.transform.forward = -controller.transform.forward;
                    }




                }
            }
        }
    }
}