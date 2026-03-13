using UnityEngine;
using UnityEditor;
using ArcadeRacer.Settings;

namespace ArcadeRacer.Editor
{
    /// <summary>
    /// Inspector personnalisé pour CircuitBuilder.
    /// Gère la création de nouveaux CircuitData et l'édition.
    /// </summary>
    #if UNITY_EDITOR
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
                // === CIRCUIT ASSIGNÉ - DÉTECTER LE MODE ===
                var mode = builder.GetCurrentMode();
                string modeText = mode == CircuitBuilder.CircuitBuilderMode.Creation ? "CRÉATION" : "ÉDITION";
                Color modeColor = mode == CircuitBuilder.CircuitBuilderMode.Creation ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.3f, 0.6f, 1f);
                
                // Afficher le mode
                var originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = modeColor;
                EditorGUILayout.HelpBox(
                    $"MODE: {modeText}\n\n" +
                    (mode == CircuitBuilder.CircuitBuilderMode.Creation 
                        ? "CRÉATION - Nouveau circuit\n" +
                          "1. Éditez votre spline visuellement\n" +
                          "2. 'Generate Preview' pour tester\n" +
                          "3. 'Export to CircuitData' pour sauvegarder"
                        : "ÉDITION - Circuit existant\n" +
                          "1. 'Load from CircuitData' pour charger\n" +
                          "2. Modifiez la spline\n" +
                          "3. 'Export to CircuitData' pour sauvegarder"),
                    MessageType.Info
                );
                GUI.backgroundColor = originalBgColor;

                EditorGUILayout.Space(10);
            }

            // === AFFICHER LES CHAMPS PAR DÉFAUT ===
            DrawDefaultInspector();

            // === BOUTONS D'ACTION (seulement si CircuitData assigné) ===
            if (circuitData != null)
            {
                var mode = builder.GetCurrentMode();
                
                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("Actions Principales", EditorStyles.boldLabel);
                
                // === MODE ÉDITION: Bouton Load ===
                if (mode == CircuitBuilder.CircuitBuilderMode.Edition)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
                    if (GUILayout.Button("📥 Load from CircuitData", GUILayout.Height(45)))
                    {
                        builder.LoadCircuitDataIntoSpline();
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.Space(5);
                }

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

                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("Gestion Checkpoints", EditorStyles.boldLabel);
                
                // Generate Checkpoints
                GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
                if (GUILayout.Button("🚦 Generate Checkpoint Preview", GUILayout.Height(35)))
                {
                    builder.GenerateCheckpointPreview();
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.Space(5);
                
                // Save Checkpoints
                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.3f);
                if (GUILayout.Button("💾 Save Checkpoints to CircuitData", GUILayout.Height(35)))
                {
                    builder.SaveCheckpointsToCircuitData();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("Utilitaires", EditorStyles.boldLabel);

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

                EditorGUILayout.Space(15);

                // === STATUT ROULABLE ===
                EditorGUILayout.LabelField("Statut Roulable", EditorStyles.boldLabel);

                if (circuitData.isRaceable)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);
                    EditorGUILayout.HelpBox(
                        "✅ Ce circuit est ROULABLE\n" +
                        "Il apparaît dans la sélection de circuit en jeu.",
                        MessageType.None
                    );
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.Space(5);

                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("❌ Marquer comme NON ROULABLE", GUILayout.Height(30)))
                    {
                        builder.MarkAsNotRaceable();
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
                    EditorGUILayout.HelpBox(
                        "⚠️ Ce circuit est NON ROULABLE\n" +
                        "Il n'apparaît pas dans la sélection de circuit en jeu.\n\n" +
                        "Exportez la spline et sauvegardez les checkpoints,\n" +
                        "puis marquez-le comme roulable.",
                        MessageType.Warning
                    );
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.Space(5);

                    GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);
                    if (GUILayout.Button("✅ Valider et Marquer comme ROULABLE", GUILayout.Height(40)))
                    {
                        builder.ValidateAndMarkAsRaceable();
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}