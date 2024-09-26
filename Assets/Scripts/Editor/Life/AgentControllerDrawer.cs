using Life.Controllers;
using UnityEditor;
using UnityEngine;

namespace MyEditor.Life
{
    [CustomEditor(typeof(AgentController), true)]
    public class AgentControllerDrawer : Editor
    {
        private AgentController controller;

        private void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            controller = target as AgentController;

            using (new EditorGUILayout.VerticalScope())
            {
                float progress = controller.GetHealth() / controller.GetMaxHealth();

                Rect r = EditorGUILayout.BeginVertical();
                EditorGUI.ProgressBar(r, progress, $"Health: {controller.GetHealth()}/{controller.GetMaxHealth()}");
                GUILayout.Space(18);
                EditorGUILayout.EndVertical();

                EditorGUILayout.Separator();

                if (controller == null)
                {
                    EditorGUILayout.HelpBox("Machine is not running", MessageType.Warning);
                    return;
                }

                if (!controller.Initialized)
                {
                    EditorGUILayout.LabelField("Machine has no state");
                    return;
                }

                EditorGUILayout.HelpBox(controller.CurrentState.GetType().Name, MessageType.Info);
            }
        }
    }
}