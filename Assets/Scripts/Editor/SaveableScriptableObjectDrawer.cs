using Core.Save;
using Game.Inventory;
using Life.Controllers;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace MyEditor.Save
{
    [CustomEditor(typeof(SaveableScriptableObject), true)]
    [CanEditMultipleObjects]
    public class SaveableScriptableObjectDrawer : Editor
    {
        private SaveableScriptableObject _saveable;

        public override void OnInspectorGUI(){
            _saveable = target as SaveableScriptableObject;
            if (targets.Length > 1) DisplayMultiEditionWarning();
            else {
                DrawHeaderForCustomEditor();
            }
            DrawDefaultInspector();
        }

        private void DisplayMultiEditionWarning()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.Separator();
                Rect r = EditorGUILayout.BeginVertical();
                EditorGUI.DrawRect(r, Color.blue / 4);
                EditorGUILayout.LabelField("Saveable Info not allowed in multiple selection", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(3);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();           
            }
        }

        private void DrawHeaderForCustomEditor()
        {
            using (new EditorGUILayout.VerticalScope())
            {
               // EditorGUILayout.LabelField($"GUID:{_saveable.Address}", EditorStyles.boldLabel);
               
            }
        }
    }
}