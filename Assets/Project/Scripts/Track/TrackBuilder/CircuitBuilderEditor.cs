using UnityEngine;
using UnityEditor;
using ArcadeRacer.Settings;

namespace ArcadeRacer.Editor
{
    /// <summary>
    /// Inspector personnalisé pour CircuitBuilder.
    /// Gère la création de nouveaux CircuitData et l'édition.
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
            serializedObject.Update();

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🏁 CIRCUIT BUILDER", EditorStyles.boldLabel);

            var circuitData = serializedObject.FindProperty("circuitData").objectReferenceValue as CircuitData;

            // === SI PAS DE CIRCUITDATA ASSIGNÉ ===
            if (circuitData == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ AUCUN CIRCUIT ASSIGNÉ\n\n" +
                    "Vous pouvez :\n" +
                    "1. Créer un NOUVEAU circuit (entrez un nom ci-dessous)\n" +
                    "2. OU Assigner un circuit existant dans le champ 'Circuit Data'",
                    MessageType.Warning
                );

                EditorGUILayout.Space(10);

                // Champ pour nouveau nom
                EditorGUILayout.LabelField("Créer un Nouveau Circuit", EditorStyles.boldLabel);

                SerializedProperty newNameProp = serializedObject.FindProperty("newCircuitName");
                EditorGUILayout.PropertyField(newNameProp, new GUIContent("Nom du Nouveau Circuit"));

                EditorGUILayout.Space(5);

                GUI.backgroundColor = Color.green;
                GUI.enabled = !string.IsNullOrWhiteSpace(newNameProp.stringValue);

                if (GUILayout.Button("✨ CRÉER NOUVEAU CIRCUIT", GUILayout.Height(50)))
                {
                    serializedObject.ApplyModifiedProperties();
                    builder.CreateNewCircuitData();
                    serializedObject.Update();
                }

                GUI.enabled = true;
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("OU", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space(5);
            }
            else
            {
                // === CIRCUIT ASSIGNÉ - WORKFLOW NORMAL ===
                EditorGUILayout.HelpBox(
                    "✅ Circuit Assigné\n\n" +
                    "WORKFLOW:\n" +
                    "1. Éditez votre spline visuellement\n" +
                    "2. Cliquez 'Generate Preview' pour tester\n" +
                    "3. Cliquez 'Export to CircuitData' pour sauvegarder",
                    MessageType.Info
                );

                EditorGUILayout.Space(10);
            }

            // === AFFICHER LES CHAMPS PAR DÉFAUT ===
            DrawDefaultInspector();

            // === BOUTONS D'ACTION (seulement si CircuitData assigné) ===
            if (circuitData != null)
            {
                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                // Preview
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("🔍 Generate Preview", GUILayout.Height(40)))
                {
                    builder.GeneratePreview();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(5);

                // Export
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("💾 Export to CircuitData", GUILayout.Height(50)))
                {
                    builder.ExportToCircuitData();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(5);

                // Clear Preview
                if (GUILayout.Button("🧹 Clear Preview", GUILayout.Height(30)))
                {
                    builder.ClearPreview();
                }

                EditorGUILayout.Space(5);

                // Create Spawn Point
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("📍 Create Spawn Point", GUILayout.Height(30)))
                {
                    builder.CreateSpawnPoint();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(15);

                // === INFOS DU CIRCUIT ===
                EditorGUILayout.LabelField("Circuit Info", EditorStyles.boldLabel);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Name", circuitData.circuitName);
                EditorGUILayout.FloatField("Track Width", circuitData.trackWidth);
                EditorGUILayout.IntField("Checkpoints", circuitData.autoCheckpointCount);
                EditorGUILayout.FloatField("Total Length", circuitData.TotalLength);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(5);

                // Bouton pour ouvrir le CircuitData
                if (GUILayout.Button("📝 Edit CircuitData Settings"))
                {
                    Selection.activeObject = circuitData;
                    EditorGUIUtility.PingObject(circuitData);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}