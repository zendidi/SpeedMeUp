using UnityEngine;
using ArcadeRacer.Settings;
using ArcadeRacer.Utilities;
using ArcadeRacer.RaceSystem;

namespace ArcadeRacer.Track
{
    /// <summary>
    /// Charge et instancie un circuit depuis un CircuitData.
    /// Place les meshes, checkpoints, et spawn point.
    /// </summary>
    public class CircuitLoader : MonoBehaviour
    {
        [Header("=== CIRCUIT TO LOAD ===")]
        [SerializeField]
        [Tooltip("Circuit à charger au démarrage")]
        private CircuitData circuitToLoad;
        
        [Header("=== GENERATED OBJECTS ===")]
        [SerializeField] private GameObject roadMeshObject;
        [SerializeField] private GameObject wallsObject;
        [SerializeField] private GameObject checkpointsParent;
        
        [Header("=== PREFABS ===")]
        [SerializeField]
        [Tooltip("Prefab de checkpoint (utilisez votre Checkpoint existant)")]
        private GameObject checkpointPrefab;
        
        [Header("=== AUTO-REFERENCES ===")]
        [SerializeField] private CheckpointManager checkpointManager;
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private float circuitHeight=.55f;


        [Header("=== QUALITY SETTINGS ===")]
        [SerializeField]
        private int meshQuality = CircuitMeshGenerator.DEFAULT_SEGMENTS;  // ← Auto-sync
        private float curveQuality = CircuitMeshGenerator.DEFAULT_CURVE_QUALITY;

        private void Awake()
        {
            // Auto-find managers si pas assignés
            if (checkpointManager == null)
                checkpointManager = FindFirstObjectByType<CheckpointManager>();
            
            if (raceManager == null)
                raceManager = FindFirstObjectByType<RaceManager>();
        }
        
        private void Start()
        {
            if (circuitToLoad == null)
            {
                Debug.LogWarning("[CircuitLoader] Aucun circuit assigné !");
                return;
            }
            
           // LoadCircuit();
        }
        
        /// <summary>
        /// Charge le circuit (meshes + checkpoints + spawn)
        /// </summary>
        public void LoadCircuit()
        {
            if (circuitToLoad == null)
            {
                Debug.LogError("[CircuitLoader] CircuitData manquant !");
                return;
            }
            
            Debug.Log($"[CircuitLoader] Chargement du circuit '{circuitToLoad.circuitName}'...");
            
            // 1. Nettoyer l'ancien circuit
            ClearCircuit();
            
            // 2. Générer les meshes
            GenerateCircuitMeshes();
            
            // 3. Créer les checkpoints
            GenerateCheckpoints();
            
            // 4. Positionner le spawn point (pour le joueur)
            SetupSpawnPoint();
            
            // 5. Configurer le RaceManager
            ConfigureRaceManager();
            
            Debug.Log($"[CircuitLoader] Circuit '{circuitToLoad.circuitName}' chargé avec succès !");
        }
        
        #region Private Methods
        
        private void ClearCircuit()
        {
            if (roadMeshObject != null)
                Destroy(roadMeshObject);
            
            if (wallsObject != null)
                Destroy(wallsObject);
            
            if (checkpointsParent != null)
                Destroy(checkpointsParent);
        }

        private void GenerateCircuitMeshes()
        {
            // Utiliser LA MÊME CONFIG que le preview (subdivision adaptative)
            var config = new CircuitMeshGenerator.GenerationConfig
            {
                segmentsPerSplinePoint = meshQuality,  // ← Base (comme dans preview)
                uvTilingX = 1f,
                uvTilingY = 0.5f,
                generateCollider = true,
                optimizeMesh = true,
                curveQualityMultiplier = curveQuality   // ← IMPORTANT : Active la subdivision adaptative
            };

            var result = CircuitMeshGenerator.Generate(circuitToLoad, config);

            if (!result.success)
            {
                Debug.LogError($"[CircuitLoader] Erreur génération mesh: {result.errorMessage}");
                return;
            }

            // === ROAD ===
            roadMeshObject = new GameObject($"{circuitToLoad.circuitName}_Road");
            roadMeshObject.transform.SetParent(transform);
            roadMeshObject.transform.localPosition = new Vector3(0, circuitHeight, 0);

            var roadFilter = roadMeshObject.AddComponent<MeshFilter>();
            roadFilter.sharedMesh = result.roadMesh;

            var roadRenderer = roadMeshObject.AddComponent<MeshRenderer>();
            roadRenderer.sharedMaterial = circuitToLoad.roadMaterial != null
                ? circuitToLoad.roadMaterial
                : new Material(Shader.Find("Standard"));

            // Collider pour la route
            var roadCollider = roadMeshObject.AddComponent<MeshCollider>();
            roadCollider.sharedMesh = result.roadMesh;
            roadCollider.convex = false;

            // === WALLS ===
            if (circuitToLoad.generateWalls && result.leftWallMesh != null)
            {
                wallsObject = new GameObject($"{circuitToLoad.circuitName}_Walls");
                wallsObject.transform.SetParent(transform);
                wallsObject.transform.localPosition = new Vector3(0, circuitHeight, 0);

                CreateWall("LeftWall", result.leftWallMesh, wallsObject.transform);
                CreateWall("RightWall", result.rightWallMesh, wallsObject.transform);
            }
        }

        private void CreateWall(string name, Mesh mesh, Transform parent)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.localPosition = Vector3.zero;
            
            var filter = wall.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            
            var renderer = wall.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = circuitToLoad.wallMaterial != null
                ? circuitToLoad.wallMaterial
                : new Material(Shader.Find("Standard"));
            
            // Collider pour les murs
            var collider = wall.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
        }
        
        private void GenerateCheckpoints()
        {
            checkpointsParent = new GameObject("Checkpoints");
            checkpointsParent.transform.SetParent(transform);
            checkpointsParent.transform.localPosition = Vector3.zero;
            
            // Générer les checkpoints automatiques
            var checkpointInfos = CircuitMeshGenerator.GenerateAutoCheckpoints(
                circuitToLoad,
                circuitToLoad.autoCheckpointCount
            );
            
            var checkpointsList = new System.Collections.Generic.List<Checkpoint>();
            
            for (int i = 0; i < checkpointInfos.Length; i++)
            {
                var info = checkpointInfos[i];
                
                GameObject checkpointGO;
                
                // Utiliser le prefab si assigné
                if (checkpointPrefab != null)
                {
                    checkpointGO = Instantiate(checkpointPrefab, checkpointsParent.transform);
                }
                else
                {
                    // Créer un checkpoint basique
                    checkpointGO = new GameObject($"Checkpoint_{i}");
                    checkpointGO.transform.SetParent(checkpointsParent.transform);
                    
                    var checkpoint = checkpointGO.AddComponent<Checkpoint>();
                    var collider = checkpointGO.GetComponent<BoxCollider>();
                    if (collider == null)
                        collider = checkpointGO.AddComponent<BoxCollider>();
                    
                    collider.isTrigger = true;
                    collider.size = new Vector3(info.width, circuitToLoad.checkpointHeight, 0.5f);
                }
                
                // Position & Rotation
                checkpointGO.transform.position = info.position;
                checkpointGO.transform.rotation = info.rotation;
                
                // Setup
                var cp = checkpointGO.GetComponent<Checkpoint>();
                if (cp != null)
                {
                    cp.Setup(i, i == 0); // Le premier est start/finish
                    checkpointsList.Add(cp);
                }
            }
            
            // Connecter au CheckpointManager
            if (checkpointManager != null)
            {
                // Vous devrez peut-être adapter selon votre CheckpointManager
                Debug.Log($"[CircuitLoader] {checkpointsList.Count} checkpoints créés.");
            }
        }
        
        private void SetupSpawnPoint()
        {
            // Trouver le véhicule du joueur
            var playerVehicle = FindFirstObjectByType<ArcadeRacer.Vehicle.VehicleController>();
            
            if (playerVehicle != null)
            {
                playerVehicle.transform.position = circuitToLoad.spawnPosition;
                playerVehicle.transform.rotation = circuitToLoad.spawnRotation;
                
                Debug.Log($"[CircuitLoader] Véhicule placé au spawn point.");
            }
            else
            {
                Debug.LogWarning("[CircuitLoader] Aucun véhicule trouvé pour positionner au spawn.");
            }
        }
        
        private void ConfigureRaceManager()
        {
            if (raceManager != null)
            {
                // Configurer le nombre de tours depuis le CircuitData
                // NOTE: Vous devrez peut-être exposer ces paramètres dans RaceManager
                Debug.Log($"[CircuitLoader] RaceManager configuré pour {circuitToLoad.targetLapCount} tours.");
            }
        }
        
        #endregion
        
        #region Editor Helpers
        
        [ContextMenu("Reload Circuit")]
        public void ReloadCircuit()
        {
            LoadCircuit();
        }
        
        #endregion
    }
}