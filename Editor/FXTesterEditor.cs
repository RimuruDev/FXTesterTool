using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AbyssMoth.Internal.Codebase.Tools.FXTesterTool.Editor
{
    [CustomEditor(typeof(FXTester))]
    public sealed class FXTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var tester = (FXTester)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Управление", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play Once"))
                    tester.PlayOnce();
                
                if (GUILayout.Button("Play Loop")) 
                    tester.PlayLoop();
                
                if (GUILayout.Button("Stop")) 
                    tester.StopAllContext();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Trigger"))
                    tester.PlayOnce();
                
                if (GUILayout.Button("Collect Children"))
                {
                    var list = new List<ParticleSystem>();
                    tester.GetComponentsInChildren(includeInactive: true, list);
                    tester.SetParticles(list);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Информация", EditorStyles.boldLabel);
            
            var particles = tester.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
          
            EditorGUILayout.LabelField("Детей ParticleSystem:", label2: (particles?.Length ?? 0).ToString());

            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
                EditorUtility.SetDirty(tester);
        }
    }
}