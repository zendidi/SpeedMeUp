#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CreateMeshAsset : MonoBehaviour
{
    [ContextMenu("Create Mesh Asset")]
    void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "RaceTrack_ExtrudeMesh";

        // Créer le dossier si nécessaire
        if (!AssetDatabase.IsValidFolder("Assets/Project/Meshes"))
        {
            AssetDatabase.CreateFolder("Assets/Project", "Meshes");
        }

        // Sauvegarder le mesh
        string path = "Assets/Project/Meshes/RaceTrack_ExtrudeMesh.asset";
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Mesh créé : {path}");

        // Assigner automatiquement au Spline Extrude
        var extrude = GetComponent<UnityEngine.Splines.SplineExtrude>();
        if (extrude != null)
        {
            extrude.targetMesh = mesh;
            Debug.Log("✅ Mesh assigné au Spline Extrude!");
        }
    }
}
#endif