using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using ArcadeRacer.Settings;
using ArcadeRacer.Utilities;
using Unity.Mathematics;

namespace ArcadeRacer.Editor
{
    /// <summary>
    /// Outil éditeur pour créer et exporter des circuits.
    /// Convertit une SplineContainer en CircuitData.asset.
    /// </summary>
    [ExecuteInEditMode]
    public class CircuitBuilder : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("=== CIRCUIT DATA ===")]
        [SerializeField]
        [Tooltip("CircuitData asset à remplir avec les données de la spline")]
        private CircuitData circuitData;
        
        [Header("=== SPLINE SOURCE ===")]
        [SerializeField]
        [Tooltip("SplineContainer contenant la trajectoire du circuit")]
        private SplineContainer splineContainer;
        
        [Header("=== SPAWN POINT ===")]
        [SerializeField]
        [Tooltip("Transform définissant le point de spawn (optionnel, sinon = début de spline)")]
        private Transform spawnPoint;
        
        [Header("=== PREVIEW SETTINGS ===")]
        [SerializeField]
        private bool showPreview = true;
        
        [SerializeField]
        private bool showSplinePoints = true;
        
        [SerializeField]
        private bool showCheckpoints = true;
        
        [SerializeField]
        private bool showSpawnPoint = true;
        
        [SerializeField]
        [Range(5, 20)]
        private int previewSegmentsPerPoint = 10;
        
        [Header("=== PREVIEW OBJECTS ===")]
        [SerializeField]
        private GameObject previewRoadObject;
        
        [SerializeField]
        private GameObject previewWallsObject;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnValidate()
        {
            // Auto-detect SplineContainer si pas assigné
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }
            
            // Auto-detect SpawnPoint si pas assigné
            if (spawnPoint == null)
            {
                Transform child = transform.Find("SpawnPoint");
                if (child != null)
                {
                    spawnPoint = child;
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showPreview) return;
            
            DrawSplinePreview();
            DrawCheckpointsPreview();
            DrawSpawnPointPreview();
        }
        
        #endregion
        
        #region Public Methods (appelés par les boutons Inspector)
        
        /// <summary>
        /// Exporte la spline vers le CircuitData asset.
        /// </summary>
        public void ExportToCircuitData()
        {
            if (!ValidateBeforeExport(out string errorMessage))
            {
                EditorUtility.DisplayDialog("Export Failed", errorMessage, "OK");
                return;
            }
            
            // Convertir SplineContainer → SplinePoint[]
            var splinePoints = ConvertSplineToPoints(splineContainer);
            circuitData.splinePoints = splinePoints;
            
            // Définir le spawn point
            if (spawnPoint != null)
            {
                circuitData.spawnPosition = spawnPoint.position;
                circuitData.spawnRotation = spawnPoint.rotation;
            }
            else
            {
                // Utiliser le début de la spline
                var firstPoint = splinePoints[0];
                circuitData.spawnPosition = firstPoint.position;
                
                // Calculer rotation basée sur la tangente
                if (splinePoints.Length > 1)
                {
                    Vector3 forward = (splinePoints[1].position - splinePoints[0].position).normalized;
                    circuitData.spawnRotation = Quaternion.LookRotation(forward, Vector3.up);
                }
            }
            
            // Calculer la longueur totale
            circuitData.CalculateTotalLength();
            
            // Marquer comme modifié
            EditorUtility.SetDirty(circuitData);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[CircuitBuilder] Circuit exporté avec succès !\n" +
                      $"  - {splinePoints.Length} points de spline\n" +
                      $"  - Longueur totale : {circuitData.TotalLength:F1}m");
            
            EditorUtility.DisplayDialog("Export Success", 
                $"Circuit '{circuitData.circuitName}' exporté avec succès !\n\n" +
                $"Points: {splinePoints.Length}\n" +
                $"Longueur: {circuitData.TotalLength:F1}m", 
                "OK");
        }
        
        /// <summary>
        /// Génère un aperçu du mesh sans sauvegarder.
        /// </summary>
        public void GeneratePreview()
        {
            if (!ValidateBeforeExport(out string errorMessage))
            {
                EditorUtility.DisplayDialog("Preview Failed", errorMessage, "OK");
                return;
            }
            
            // Nettoyer l'ancien aperçu
            ClearPreview();
            
            // Créer un CircuitData temporaire
            var tempCircuitData = ScriptableObject.CreateInstance<CircuitData>();
            tempCircuitData.splinePoints = ConvertSplineToPoints(splineContainer);
            tempCircuitData.trackWidth = circuitData.trackWidth;
            tempCircuitData.wallHeight = circuitData.wallHeight;
            tempCircuitData.generateWalls = circuitData.generateWalls;
            tempCircuitData.closedLoop = circuitData.closedLoop;
            tempCircuitData.circuitName = "Preview";
            
            // Générer le mesh
            var config = new CircuitMeshGenerator.GenerationConfig
            {
                segmentsPerSplinePoint = previewSegmentsPerPoint,
                uvTilingX = 1f,
                uvTilingY = 0.5f,
                generateCollider = false, // Pas besoin de collider pour preview
                optimizeMesh = false
            };
            
            var result = CircuitMeshGenerator.Generate(tempCircuitData, config);
            
            if (!result.success)
            {
                EditorUtility.DisplayDialog("Preview Failed", result.errorMessage, "OK");
                return;
            }
            
            // Créer les GameObjects de preview
            previewRoadObject = new GameObject("PREVIEW_Road");
            previewRoadObject.transform.SetParent(transform);
            previewRoadObject.hideFlags = HideFlags.DontSave;
            
            var roadFilter = previewRoadObject.AddComponent<MeshFilter>();
            roadFilter.sharedMesh = result.roadMesh;
            
            var roadRenderer = previewRoadObject.AddComponent<MeshRenderer>();
            roadRenderer.sharedMaterial = circuitData.roadMaterial != null 
                ? circuitData.roadMaterial 
                : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            
            // Murs
            if (circuitData.generateWalls && result.leftWallMesh != null)
            {
                previewWallsObject = new GameObject("PREVIEW_Walls");
                previewWallsObject.transform.SetParent(transform);
                previewWallsObject.hideFlags = HideFlags.DontSave;
                
                var leftWall = new GameObject("LeftWall");
                leftWall.transform.SetParent(previewWallsObject.transform);
                var leftFilter = leftWall.AddComponent<MeshFilter>();
                leftFilter.sharedMesh = result.leftWallMesh;
                var leftRenderer = leftWall.AddComponent<MeshRenderer>();
                leftRenderer.sharedMaterial = circuitData.wallMaterial != null 
                    ? circuitData.wallMaterial 
                    : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                
                var rightWall = new GameObject("RightWall");
                rightWall.transform.SetParent(previewWallsObject.transform);
                var rightFilter = rightWall.AddComponent<MeshFilter>();
                rightFilter.sharedMesh = result.rightWallMesh;
                var rightRenderer = rightWall.AddComponent<MeshRenderer>();
                rightRenderer.sharedMaterial = circuitData.wallMaterial != null 
                    ? circuitData.wallMaterial 
                    : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }
            
            Debug.Log("[CircuitBuilder] Preview généré avec succès !");
        }
        
        /// <summary>
        /// Nettoie l'aperçu.
        /// </summary>
        public void ClearPreview()
        {
            if (previewRoadObject != null)
            {
                DestroyImmediate(previewRoadObject);
                previewRoadObject = null;
            }
            
            if (previewWallsObject != null)
            {
                DestroyImmediate(previewWallsObject);
                previewWallsObject = null;
            }
            
            Debug.Log("[CircuitBuilder] Preview nettoyé.");
        }
        
        /// <summary>
        /// Crée automatiquement un SpawnPoint enfant.
        /// </summary>
        public void CreateSpawnPoint()
        {
            if (spawnPoint != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "SpawnPoint existe déjà", 
                    "Un SpawnPoint existe déjà. Le remplacer ?", 
                    "Oui", 
                    "Non"
                );
                
                if (!replace) return;
                
                DestroyImmediate(spawnPoint.gameObject);
            }
            
            GameObject spawnGO = new GameObject("SpawnPoint");
            spawnGO.transform.SetParent(transform);
            
            // Placer au début de la spline
            if (splineContainer != null && splineContainer.Spline.Count > 0)
            {
                splineContainer.Evaluate(0, out float3 position, out float3 tangent, out float3 up);
                spawnGO.transform.position = position;
                spawnGO.transform.rotation = Quaternion.LookRotation(tangent, up);
            }
            
            spawnPoint = spawnGO.transform;
            
            Debug.Log("[CircuitBuilder] SpawnPoint créé au début de la spline.");
        }
        
        #endregion
        
        #region Private Methods
        
        private bool ValidateBeforeExport(out string errorMessage)
        {
            if (circuitData == null)
            {
                errorMessage = "CircuitData asset manquant ! Assignez-en un dans l'Inspector.";
                return false;
            }
            
            if (splineContainer == null)
            {
                errorMessage = "SplineContainer manquant ! Ajoutez un component SplineContainer.";
                return false;
            }
            
            if (splineContainer.Spline == null || splineContainer.Spline.Count < 2)
            {
                errorMessage = "La spline doit contenir au moins 2 points !";
                return false;
            }
            
            errorMessage = string.Empty;
            return true;
        }

        private SplinePoint[] ConvertSplineToPoints(SplineContainer container)
        {
            var spline = container.Spline;
            var points = new SplinePoint[spline.Count];

            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];

                // Position en world space
                Vector3 worldPosition = container.transform.TransformPoint(knot.Position);

                // CORRECTION: Les tangentes doivent être relatives à la position du knot
                // TangentIn et TangentOut sont déjà en espace local du knot
                Vector3 worldTangentIn = container.transform.TransformVector(knot.TangentIn);
                Vector3 worldTangentOut = container.transform.TransformVector(knot.TangentOut);

                points[i] = new SplinePoint
                {
                    position = worldPosition,
                    tangentIn = worldTangentIn,
                    tangentOut = worldTangentOut
                };
            }

            return points;
        }

        #endregion

        #region Gizmos

        private void DrawSplinePreview()
        {
            if (!showSplinePoints || splineContainer == null) return;
            
            var spline = splineContainer.Spline;
            if (spline == null || spline.Count == 0) return;
            
            Gizmos.color = Color.cyan;
            
            for (int i = 0; i < spline.Count; i++)
            {
                Vector3 pos = splineContainer.transform.TransformPoint(spline[i].Position);
                Gizmos.DrawWireSphere(pos, 1f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(pos + Vector3.up * 2f, $"P{i}");
                #endif
            }
        }
        
        private void DrawCheckpointsPreview()
        {
            if (!showCheckpoints || circuitData == null || splineContainer == null) return;
            
            // Créer un CircuitData temporaire pour preview
            var tempData = ScriptableObject.CreateInstance<CircuitData>();
            tempData.splinePoints = ConvertSplineToPoints(splineContainer);
            tempData.trackWidth = circuitData.trackWidth;
            tempData.closedLoop = circuitData.closedLoop;
            tempData.autoCheckpointCount = circuitData.autoCheckpointCount;
            
            var checkpoints = CircuitMeshGenerator.GenerateAutoCheckpoints(
                tempData, 
                circuitData.autoCheckpointCount
            );
            
            Gizmos.color = Color.green;
            
            foreach (var cp in checkpoints)
            {
                Vector3 size = new Vector3(cp.width, 3f, 0.5f);
                
                // Dessiner le checkpoint
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(cp.position, cp.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                
                // Version semi-transparente
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.color = Color.green;
                
                Gizmos.matrix = oldMatrix;
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(cp.position + Vector3.up * 4f, $"CP {cp.index}");
                #endif
            }
        }
        
        private void DrawSpawnPointPreview()
        {
            if (!showSpawnPoint || spawnPoint == null) return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnPoint.position, 1.5f);
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 3f, "SPAWN");
            #endif
        }
        
        #endregion
    }
}