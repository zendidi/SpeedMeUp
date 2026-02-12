using UnityEngine;
using UnityEditor;

namespace ArcadeRacer.Editor
{
    [CustomEditor(typeof(CircuitEditorTool))]
    public class CircuitEditorToolInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var tool = (CircuitEditorTool)target;
            
            // Header
            EditorGUILayout.Space(10);
            GUILayout.Label("🏁 CIRCUIT EDITOR TOOL", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Outil simple pour créer et sauvegarder des circuits.\n\n" +
                "WORKFLOW:\n" +
                "1. Cliquez 'New Circuit' pour commencer\n" +
                "2. Éditez la spline avec l'outil Unity Splines\n" +
                "3. Configurez les paramètres ci-dessous\n" +
                "4. Cliquez 'Preview' pour tester\n" +
                "5. Donnez un nom et cliquez 'Save Circuit'",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // BOUTON NEW
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("✨ NEW CIRCUIT", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Nouveau Circuit", 
                    "Créer un nouveau circuit ?\n\nCela va réinitialiser la spline actuelle.", 
                    "Oui", "Annuler"))
                {
                    tool.CreateNewCircuit();
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(15);
            
            // Afficher les champs
            DrawDefaultInspector();
            
            EditorGUILayout.Space(15);
            
            // BOUTONS D'ACTION
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("🔍 PREVIEW", GUILayout.Height(40)))
            {
                tool.GeneratePreview();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("💾 SAVE CIRCUIT", GUILayout.Height(50)))
            {
                tool.SaveCircuit();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("🧹 Clear Preview", GUILayout.Height(30)))
            {
                tool.ClearPreview();
            }
        }
    }
}