using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using ArcadeRacer.Settings;
using ArcadeRacer.Utilities;
using Unity.Mathematics;
using System.IO;

namespace ArcadeRacer.Editor
{
    /// <summary>
    /// Outil tout-en-un pour créer et sauvegarder des circuits.
    /// Interface simple pour les level designers.
    /// </summary>
    [ExecuteInEditMode]
    public class CircuitEditorTool : MonoBehaviour
    {
        #region Configuration
        
        [Header("=== CIRCUIT NAME ===")]
        [Tooltip("Nom du circuit (utilisé pour les fichiers)")]
        public string circuitName = "NewCircuit";
        
        [Header("=== TRACK SETTINGS ===")]
        [Range(5f, 30f)]
        [Tooltip("Largeur de la piste en mètres")]
        public float trackWidth = 10f;
        
        [Range(3, 50)]
        [Tooltip("Nombre de checkpoints automatiques")]
        public int checkpointCount = 10;
        
        [Range(1, 10)]
        [Tooltip("Nombre de tours pour gagner")]
        public int targetLaps = 3;
        
        [Header("=== VISUAL ===")]
        [Tooltip("Matériau de la route")]
        public Material roadMaterial;
        
        [Tooltip("Matériau des murs")]
        public Material wallMaterial;
        
        [Range(0.5f, 5f)]
        [Tooltip("Hauteur des murs")]
        public float wallHeight = 2f;
        
        [Tooltip("Générer les murs latéraux")]
        public bool generateWalls = true;
        
        [Header("=== AUTO-REFERENCES (Auto-assigned) ===")]
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private CircuitData circuitDataAsset;
        
        [Header("=== PREVIEW ===")]
        [SerializeField] private bool showPreview = true;
        [SerializeField] private GameObject previewMesh;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnValidate()
        {
            // Auto-detect SplineContainer
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }
            
            // Auto-detect SpawnPoint
            if (spawnPoint == null)
            {
                Transform child = transform.Find("SpawnPoint");
                if (child != null)
                {
                    spawnPoint = child;
                }
            }
        }
        
        #endregion
        
        #region Public Methods (Appelés par l'Inspector)
        
        /// <summary>
        /// Crée un nouveau circuit de zéro
        /// </summary>
        public void CreateNewCircuit()
        {
            // Nettoyer l'existant
            ClearPreview();
            
            // Créer la spline si elle n'existe pas
            if (splineContainer == null)
            {
                splineContainer = gameObject.AddComponent<SplineContainer>();
            }
            
            // Créer une spline de base (rectangle)
            CreateDefaultSpline();
            
            // Créer le spawn point
            if (spawnPoint == null)
            {
                GameObject spawnGO = new GameObject("SpawnPoint");
                spawnGO.transform.SetParent(transform);
                spawnGO.transform.position = Vector3.zero;
                spawnGO.transform.rotation = Quaternion.identity;
                spawnPoint = spawnGO.transform;
            }
            
            Debug.Log($"[CircuitEditorTool] Nouveau circuit créé ! Éditez la spline avec l'outil Spline.");
            EditorUtility.DisplayDialog("Succès", 
                "Nouveau circuit créé !\n\n" +
                "1. Utilisez l'outil Spline pour modifier le tracé\n" +
                "2. Cliquez sur 'Preview' pour voir le résultat\n" +
                "3. Cliquez sur 'Save Circuit' pour sauvegarder", 
                "OK");
        }
        
        /// <summary>
        /// Génère un aperçu du circuit
        /// </summary>
        public void GeneratePreview()
        {
            if (!ValidateCircuit(out string error))
            {
                EditorUtility.DisplayDialog("Erreur", error, "OK");
                return;
            }
            
            ClearPreview();
            
            // Créer CircuitData temporaire
            var tempData = CreateTempCircuitData();
            
            // Générer le mesh
            var config = new CircuitMeshGenerator.GenerationConfig
            {
                segmentsPerSplinePoint = 14,
                uvTilingX = 1f,
                uvTilingY = 0.5f,
                generateCollider = false,
                optimizeMesh = true,
                curveQualityMultiplier = CircuitMeshGenerator.DEFAULT_CURVE_QUALITY

            };
            
            var result = CircuitMeshGenerator.Generate(tempData, config);
            
            if (!result.success)
            {
                EditorUtility.DisplayDialog("Erreur", result.errorMessage, "OK");
                return;
            }
            
            // Créer l'objet de preview
            previewMesh = new GameObject("PREVIEW_Circuit");
            previewMesh.transform.SetParent(transform);
            previewMesh.transform.localPosition = Vector3.zero;
            previewMesh.hideFlags = HideFlags.DontSave;
            
            // Road
            var roadFilter = previewMesh.AddComponent<MeshFilter>();
            roadFilter.sharedMesh = result.roadMesh;
            var roadRenderer = previewMesh.AddComponent<MeshRenderer>();
            roadRenderer.sharedMaterial = roadMaterial != null 
                ? roadMaterial 
                : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            
            // Walls
            if (generateWalls && result.leftWallMesh != null)
            {
                CreateWallMesh("LeftWall", result.leftWallMesh);
                CreateWallMesh("RightWall", result.rightWallMesh);
            }
            
            Debug.Log("[CircuitEditorTool] Preview généré !");
        }
        
        /// <summary>
        /// Sauvegarde le circuit complet (CircuitData + Meshes)
        /// </summary>
        public void SaveCircuit()
        {
            if (!ValidateCircuit(out string error))
            {
                EditorUtility.DisplayDialog("Erreur", error, "OK");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(circuitName))
            {
                EditorUtility.DisplayDialog("Erreur", "Veuillez donner un nom au circuit !", "OK");
                return;
            }
            
            // Créer les dossiers si nécessaire
            CreateDirectories();
            
            // Créer ou mettre à jour le CircuitData
            string assetPath = $"Assets/Project/Settings/Circuits/{circuitName}.asset";
            
            CircuitData data = AssetDatabase.LoadAssetAtPath<CircuitData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CircuitData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            
            // Remplir les données
            PopulateCircuitData(data);
            
            // Générer et sauvegarder les meshes
            SaveCircuitMeshes(data);
            
            // Sauvegarder
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            circuitDataAsset = data;
            
            Debug.Log($"[CircuitEditorTool] Circuit '{circuitName}' sauvegardé !");
            EditorUtility.DisplayDialog("Succès", 
                $"Circuit '{circuitName}' sauvegardé !\n\n" +
                $"Fichier: {assetPath}\n\n" +
                "Vous pouvez maintenant l'utiliser en jeu.", 
                "OK");
        }
        
        /// <summary>
        /// Nettoie le preview
        /// </summary>
        public void ClearPreview()
        {
            if (previewMesh != null)
            {
                DestroyImmediate(previewMesh);
                previewMesh = null;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void CreateDefaultSpline()
        {
            var spline = splineContainer.Spline;
            spline.Clear();
            
            // Créer un circuit rectangulaire de base
            float size = 50f;
            var knots = new[]
            {
                new BezierKnot(new float3(0, 0, 0)),
                new BezierKnot(new float3(size, 0, 0)),
                new BezierKnot(new float3(size, 0, size)),
                new BezierKnot(new float3(0, 0, size))
            };
            
            foreach (var knot in knots)
            {
                spline.Add(knot, TangentMode.AutoSmooth);
            }
            
            spline.Closed = true;
        }
        
        private bool ValidateCircuit(out string error)
        {
            if (splineContainer == null || splineContainer.Spline == null || splineContainer.Spline.Count < 3)
            {
                error = "La spline doit contenir au moins 3 points !";
                return false;
            }
            
            error = string.Empty;
            return true;
        }
        
        private CircuitData CreateTempCircuitData()
        {
            var data = ScriptableObject.CreateInstance<CircuitData>();
            PopulateCircuitData(data);
            return data;
        }
        
        private void PopulateCircuitData(CircuitData data)
        {
            data.circuitName = circuitName;
            data.splinePoints = ConvertSplineToPoints();
            data.trackWidth = trackWidth;
            data.closedLoop = splineContainer.Spline.Closed;
            data.autoCheckpointCount = checkpointCount;
            data.targetLapCount = targetLaps;
            data.wallHeight = wallHeight;
            data.generateWalls = generateWalls;
            data.roadMaterial = roadMaterial;
            data.wallMaterial = wallMaterial;
            
            // Spawn point
            if (spawnPoint != null)
            {
                data.spawnPosition = spawnPoint.position;
                data.spawnRotation = spawnPoint.rotation;
            }
            else
            {
                var firstPoint = data.splinePoints[0];
                data.spawnPosition = firstPoint.position;
                data.spawnRotation = Quaternion.LookRotation(
                    (data.splinePoints[1].position - firstPoint.position).normalized
                );
            }
            
            data.CalculateTotalLength();
        }
        
        private SplinePoint[] ConvertSplineToPoints()
        {
            var spline = splineContainer.Spline;
            var points = new SplinePoint[spline.Count];
            
            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                
                Vector3 worldPos = splineContainer.transform.TransformPoint(knot.Position);
                Quaternion worldRot = splineContainer.transform.rotation * knot.Rotation;
                
                Vector3 tangentInWorld = splineContainer.transform.TransformPoint(knot.Position + knot.TangentIn) - worldPos;
                Vector3 tangentOutWorld = splineContainer.transform.TransformPoint(knot.Position + knot.TangentOut) - worldPos;
                
                points[i] = new SplinePoint
                {
                    position = worldPos,
                    tangentIn = tangentInWorld,
                    tangentOut = tangentOutWorld,
                    rotation = worldRot
                };
            }
            
            return points;
        }
        
        private void SaveCircuitMeshes(CircuitData data)
        {
            var config = new CircuitMeshGenerator.GenerationConfig
            {
                segmentsPerSplinePoint = 14,
                uvTilingX = 1f,
                uvTilingY = 0.5f,
                generateCollider = true,
                optimizeMesh = true,
                curveQualityMultiplier = CircuitMeshGenerator.DEFAULT_CURVE_QUALITY
            };
            
            var result = CircuitMeshGenerator.Generate(data, config);
            
            if (!result.success)
            {
                Debug.LogError($"Erreur génération mesh: {result.errorMessage}");
                return;
            }
            
            // Sauvegarder les meshes comme assets
            string meshFolder = "Assets/Project/Meshes/Circuits";
            Directory.CreateDirectory(meshFolder);
            
            if (result.roadMesh != null)
            {
                AssetDatabase.CreateAsset(result.roadMesh, $"{meshFolder}/{circuitName}_Road.asset");
            }
            
            if (result.leftWallMesh != null)
            {
                AssetDatabase.CreateAsset(result.leftWallMesh, $"{meshFolder}/{circuitName}_LeftWall.asset");
            }
            
            if (result.rightWallMesh != null)
            {
                AssetDatabase.CreateAsset(result.rightWallMesh, $"{meshFolder}/{circuitName}_RightWall.asset");
            }
        }
        
        private void CreateWallMesh(string name, Mesh mesh)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(previewMesh.transform);
            wall.transform.localPosition = Vector3.zero;
            
            var filter = wall.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            
            var renderer = wall.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = wallMaterial != null 
                ? wallMaterial 
                : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        }
        
        private void CreateDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Project/Settings/Circuits"))
            {
                AssetDatabase.CreateFolder("Assets/Project/Settings", "Circuits");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/Project/Meshes/Circuits"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Project/Meshes"))
                {
                    AssetDatabase.CreateFolder("Assets/Project", "Meshes");
                }
                AssetDatabase.CreateFolder("Assets/Project/Meshes", "Circuits");
            }
        }
        
        #endregion
    }
}