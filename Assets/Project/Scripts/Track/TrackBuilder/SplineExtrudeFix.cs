using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineExtrude))]
public class SplineExtrudeFix : MonoBehaviour
{
    private void Awake()
    {
        var extrude = GetComponent<SplineExtrude>();
        
        if (extrude != null && extrude.targetMesh == null)
        {
            // Créer un mesh temporaire en mémoire
            Mesh mesh = new Mesh();
            mesh.name = $"{gameObject.name}_ExtrudeMesh";
            extrude.targetMesh = mesh;
            
            Debug.Log($"[SplineExtrudeFix] Mesh créé pour {gameObject.name}");
        }
    }
}