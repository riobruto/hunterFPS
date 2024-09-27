using Life.Controllers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MyEditor.Life
{
    [CustomEditor(typeof(AgentController), true)]
    public class AgentControllerDrawer : Editor
    {
        private AgentController controller;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawHeaderForCustomEditor();

            controller = target as AgentController;

            using (new EditorGUILayout.VerticalScope())
            {
                //Setting dirty to reflect changes every draw call
                EditorUtility.SetDirty(target);

                DisplayPlayerDetection();
                DisplayAgentState();
                DisplayHealthBar();
            }
        }

        private void DrawHeaderForCustomEditor()
        {
            EditorGUILayout.Separator();
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(r, Color.gray);
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Agent Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

        }

        private void DisplayAgentState()
        {
            EditorGUILayout.LabelField("Agent Status", EditorStyles.boldLabel);
                        
            if (controller.GetHealth() <= 0)
            {
                Rect deadr = EditorGUILayout.BeginVertical();
                EditorGUI.DrawRect(deadr, Color.red / 4);
                EditorGUILayout.LabelField("Agent is Dead",  EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10);
                EditorGUILayout.EndVertical();
                return;
            }

            if (!controller.Initialized)
            {
                Rect empty = EditorGUILayout.BeginVertical();
                EditorGUI.DrawRect(empty, Color.grey/4);
                EditorGUILayout.LabelField("Machine has no state", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10);
                EditorGUILayout.EndVertical();
                return;
            }
            Rect rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rect, Color.yellow / 4);
            EditorGUILayout.LabelField(controller.CurrentState.GetType().Name, EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
        }

        private void DisplayHealthBar()
        {
           
            EditorGUILayout.Separator();
            float progress = controller.GetHealth() / controller.GetMaxHealth();
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.ProgressBar(r, progress, $"Health: {controller.GetHealth()}/{controller.GetMaxHealth()}");
            GUILayout.Space(18);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
        }

        private void DisplayPlayerDetection()
        {
            EditorGUILayout.LabelField(" Agent Player Status", EditorStyles.boldLabel);

            if (controller.PlayerBehavior == null)
            {
                Rect empty = EditorGUILayout.BeginVertical();
                EditorGUI.DrawRect(empty, Color.grey/4);
                EditorGUI.LabelField(empty, "Player Inactive", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Separator();
                return;
            }

            Rect playerinforect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(playerinforect, controller.PlayerBehavior.PlayerDetected ? Color.red / 2 : Color.green / 2);
            EditorGUI.LabelField(playerinforect, controller.PlayerBehavior.PlayerDetected ? "Player Detected" : "Player Undetected", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(18);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }
    }
}