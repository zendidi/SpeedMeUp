using UnityEngine;
using UnityEditor;
using ArcadeRacer.Settings;

namespace ArcadeRacer.Editor
{
    /// <summary>
    /// Inspector personnalisé pour CircuitBuilder.
    /// Ajoute des boutons pour faciliter le workflow.
    /// </summary>
    [CustomEditor(typeof(CircuitBuilder))]
    public class CircuitBuilderEditor : UnityEditor.Editor
    {
        private CircuitBuilder builder;
        
        private void OnEnable()
        {
            builder = (CircuitBuilder)target;
        }
        
        public override void OnInspectorGUI()
        {
            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🏁 CIRCUIT BUILDER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Cet outil convertit une SplineContainer en CircuitData.asset.\n\n" +
                "1. Assignez un CircuitData asset\n" +
                "2. Créez votre spline visuellement\n" +
                "3. Cliquez sur 'Generate Preview' pour tester\n" +
                "4. Cliquez sur 'Export to CircuitData' pour sauvegarder", 
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // Afficher les champs par défaut
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // === BOUTONS D'ACTION ===
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("🔍 Generate Preview", GUILayout.Height(40)))
            {
                builder.GeneratePreview();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("💾 Export to CircuitData", GUILayout.Height(40)))
            {
                builder.ExportToCircuitData();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("🧹 Clear Preview", GUILayout.Height(30)))
            {
                builder.ClearPreview();
            }
            
            EditorGUILayout.Space(5);
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("📍 Create Spawn Point", GUILayout.Height(30)))
            {
                builder.CreateSpawnPoint();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(10);
            
            // === INFOS ===
            var circuitData = serializedObject.FindProperty("circuitData").objectReferenceValue as CircuitData;
            if (circuitData != null)
            {
                EditorGUILayout.LabelField("Circuit Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Name: {circuitData.circuitName}");
                EditorGUILayout.LabelField($"Track Width: {circuitData.trackWidth}m");
                EditorGUILayout.LabelField($"Checkpoints: {circuitData.autoCheckpointCount}");
                EditorGUILayout.LabelField($"Total Length: {circuitData.TotalLength:F1}m");
            }
        }
    }
}