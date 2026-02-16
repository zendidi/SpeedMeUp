using UnityEngine;
using System;
using ArcadeRacer.Settings;
using ArcadeRacer.Utilities;
using ArcadeRacer.RaceSystem; // VOTRE namespace pour CheckpointManager
using UnityEngine.Splines;

namespace ArcadeRacer.Managers
{
    /// <summary>
    /// Manager singleton pour le chargement et la gestion des circuits au runtime.
    /// S'intègre avec le CheckpointManager existant.
    /// </summary>
    public class CircuitManager : MonoBehaviour
    {
        #region Singleton
        
        private static CircuitManager _instance;
        public static CircuitManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CircuitManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("CircuitManager");
                        _instance = go.AddComponent<CircuitManager>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Events
        
        public event Action<CircuitData> OnCircuitLoaded;
        public event Action OnCircuitUnloaded;
        public event Action<string> OnLoadError;
        
        #endregion
        
        #region Serialized Fields
        
        [Header("=== CONFIGURATION ===")]
        [SerializeField] 
        private Material _defaultRoadMaterial;
        
        [SerializeField] 
        private Material _defaultWallMaterial;
        
        [SerializeField]
        [Tooltip("Prefab de checkpoint (utilisé par votre CheckpointManager)")]
        private GameObject _checkpointPrefab;
        
        [Header("=== GENERATION SETTINGS ===")]
        [SerializeField]
        [Range(5, 20)]
        private int _segmentsPerSplinePoint = 10;
        
        [Header("=== DEBUG ===")]
        [SerializeField] 
        private bool _showDebugInfo = true;
        
        #endregion
        
        #region Private Fields
        
        private CircuitData _currentCircuit;
        private GameObject _circuitRoot;
        private GameObject _roadObject;
        private GameObject _leftWallObject;
        private GameObject _rightWallObject;
        private Transform _spawnPoint;
        private SplineContainer _runtimeSplineContainer; // Pour votre CheckpointManager
        private bool _isLoaded;
        
        #endregion
        
        #region Properties
        
        public CircuitData CurrentCircuit => _currentCircuit;
        public bool IsCircuitLoaded => _isLoaded;
        public Transform SpawnPoint => _spawnPoint;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Charge un circuit et génère tous les éléments nécessaires.
        /// Point d'entrée unique pour le chargement de circuits.
        /// </summary>
        public void LoadCircuit(CircuitData circuitData)
        {
            if (circuitData == null)
            {
                Debug.LogError("[CircuitManager] LoadCircuit() - CircuitData is null!");
                OnLoadError?.Invoke("CircuitData is null.");
                return;
            }
            
            Debug.Log($"[CircuitManager] LoadCircuit() - Loading circuit '{circuitData.circuitName}'...");
            
            if (!circuitData.Validate(out string errorMessage))
            {
                Debug.LogError($"[CircuitManager] LoadCircuit() - Validation failed: {errorMessage}");
                OnLoadError?.Invoke(errorMessage);
                return;
            }
            
            if (_isLoaded)
            {
                Debug.Log($"[CircuitManager] LoadCircuit() - Unloading previous circuit '{_currentCircuit?.circuitName}'...");
                UnloadCurrentCircuit();
            }
            
            // SET CURRENT CIRCUIT FIRST - This is critical for CheckpointManager!
            _currentCircuit = circuitData;
            Debug.Log($"[CircuitManager] LoadCircuit() - CurrentCircuit set to '{circuitData.circuitName}'");
            
            try
            {
                // 1. Créer le root
                _circuitRoot = new GameObject($"Circuit_{circuitData.circuitName}");
                Debug.Log($"[CircuitManager] LoadCircuit() - Created circuit root GameObject");

                // 2. Générer le mesh de route
                // Utiliser configuration unifiée pour garantir cohérence avec l'éditeur
                var config = CircuitGenerationConstants.RuntimeConfig;
                Debug.Log($"[CircuitManager] LoadCircuit() - Generating mesh with segments={config.segmentsPerSplinePoint}, quality={config.curveQualityMultiplier}...");
                
                var result = CircuitMeshGenerator.Generate(circuitData, config);
                
                if (!result.success)
                {
                    Debug.LogError($"[CircuitManager] LoadCircuit() - Mesh generation failed: {result.errorMessage}");
                    OnLoadError?.Invoke(result.errorMessage);
                    Destroy(_circuitRoot);
                    _currentCircuit = null;
                    return;
                }
                
                Debug.Log($"[CircuitManager] LoadCircuit() - Mesh generated successfully");
                
                // 3. Créer les GameObjects de mesh
                CreateRoadObject(result.roadMesh, circuitData);
                Debug.Log($"[CircuitManager] LoadCircuit() - Road mesh created");
                
                if (circuitData.generateWalls)
                {
                    CreateWallObjects(result.leftWallMesh, result.rightWallMesh, circuitData);
                    Debug.Log($"[CircuitManager] LoadCircuit() - Wall meshes created");
                }
                
                // 4. Créer le spawn point
                CreateSpawnPoint(circuitData);
                Debug.Log($"[CircuitManager] LoadCircuit() - Spawn point created at {_spawnPoint.position}");
                
                // 5. Créer la SplineContainer pour compatibilité
                CreateRuntimeSpline(circuitData);
                Debug.Log($"[CircuitManager] LoadCircuit() - Runtime spline container created");
                
                // 6. Initialiser votre CheckpointManager existant
                InitializeCheckpointManager(circuitData);
                
                _isLoaded = true;
                
                Debug.Log($"[CircuitManager] ✓ Circuit '{circuitData.circuitName}' loaded successfully!");
                
                // Fire event AFTER everything is ready - CheckpointManager listens to this!
                OnCircuitLoaded?.Invoke(circuitData);
                Debug.Log($"[CircuitManager] OnCircuitLoaded event fired for '{circuitData.circuitName}'");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CircuitManager] LoadCircuit() - Exception: {e.Message}");
                OnLoadError?.Invoke($"Failed to load circuit: {e.Message}");
                if (_circuitRoot != null) Destroy(_circuitRoot);
                _currentCircuit = null;
                Debug.LogException(e);
            }

            _circuitRoot.transform.position += new Vector3(0, 0.05f, 0);

        }

        public void UnloadCurrentCircuit()
        {
            if (!_isLoaded)
            {
                Debug.LogWarning("[CircuitManager] UnloadCurrentCircuit() - No circuit is currently loaded.");
                return;
            }
            
            Debug.Log($"[CircuitManager] UnloadCurrentCircuit() - Unloading '{_currentCircuit?.circuitName}'...");
            
            if (_circuitRoot != null)
            {
                Destroy(_circuitRoot);
            }
            
            _currentCircuit = null;
            _roadObject = null;
            _leftWallObject = null;
            _rightWallObject = null;
            _spawnPoint = null;
            _runtimeSplineContainer = null;
            _isLoaded = false;
            
            Debug.Log("[CircuitManager] UnloadCurrentCircuit() - Circuit unloaded.");
            OnCircuitUnloaded?.Invoke();
        }
        
        /// <summary>
        /// Change de circuit en déchargeant l'ancien et chargeant le nouveau.
        /// Méthode pratique pour changer facilement de circuit en playmode.
        /// </summary>
        public void ChangeCircuit(CircuitData newCircuitData)
        {
            Debug.Log($"[CircuitManager] ChangeCircuit() - Changing to '{newCircuitData?.circuitName}'...");
            LoadCircuit(newCircuitData); // LoadCircuit handles unloading the old circuit automatically
        }
        
        public void SpawnVehicle(Transform vehicle)
        {
            if (!_isLoaded || _spawnPoint == null || vehicle == null) return;
            
            vehicle.position = _spawnPoint.position;
            vehicle.rotation = _spawnPoint.rotation;
        }
        
        #endregion
        
        #region Private Methods
        
        private void CreateRoadObject(Mesh roadMesh, CircuitData circuitData)
        {
            _roadObject = new GameObject("Road");
            _roadObject.transform.SetParent(_circuitRoot.transform);
            
            var meshFilter = _roadObject.AddComponent<MeshFilter>();
            meshFilter.mesh = roadMesh;
            
            var meshRenderer = _roadObject.AddComponent<MeshRenderer>();
            meshRenderer.material = circuitData.roadMaterial != null 
                ? circuitData.roadMaterial 
                : _defaultRoadMaterial;
            
            var meshCollider = _roadObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = roadMesh;
        }
        
        private void CreateWallObjects(Mesh leftWallMesh, Mesh rightWallMesh, CircuitData circuitData)
        {
            Material wallMat = circuitData.wallMaterial != null 
                ? circuitData.wallMaterial 
                : _defaultWallMaterial;
            
            _leftWallObject = CreateWall("LeftWall", leftWallMesh, wallMat);
            _rightWallObject = CreateWall("RightWall", rightWallMesh, wallMat);
        }
        
        private GameObject CreateWall(string name, Mesh mesh, Material material)
        {
            var wallObj = new GameObject(name);
            wallObj.transform.SetParent(_circuitRoot.transform);
            
            var meshFilter = wallObj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = wallObj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            
            var meshCollider = wallObj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            
            return wallObj;
        }
        
        private void CreateSpawnPoint(CircuitData circuitData)
        {
            var spawnObj = new GameObject("SpawnPoint");
            spawnObj.transform.SetParent(_circuitRoot.transform);
            
            // If spawn position is at zero (not set), use the first spline point
            Vector3 spawnPos = circuitData.spawnPosition;
            Quaternion spawnRot = circuitData.spawnRotation;
            
            if (circuitData.spawnPosition == Vector3.zero && circuitData.splinePoints.Length > 0)
            {
                // Use first spline point as spawn location
                spawnPos = circuitData.splinePoints[0].position;
                
                // Calculate rotation based on direction to next spline point
                if (circuitData.splinePoints.Length > 1)
                {
                    Vector3 forward = (circuitData.splinePoints[1].position - circuitData.splinePoints[0].position).normalized;
                    if (forward != Vector3.zero)
                    {
                        spawnRot = Quaternion.LookRotation(forward, Vector3.up);
                    }
                }
                
                Debug.Log($"[CircuitManager] Auto-calculated spawn point from first spline point: {spawnPos}");
            }
            
            spawnObj.transform.position = spawnPos;
            spawnObj.transform.rotation = spawnRot;
            
            _spawnPoint = spawnObj.transform;
            
            if (_showDebugInfo)
            {
                Debug.Log($"[CircuitManager] Spawn point created at: {spawnPos}, rotation: {spawnRot.eulerAngles}");
            }
        }
        
        /// <summary>
        /// Crée une SplineContainer au runtime à partir des données CircuitData.
        /// Utilisé par votre CheckpointManager existant.
        /// </summary>
        private void CreateRuntimeSpline(CircuitData circuitData)
        {
            var splineObj = new GameObject("Circuit_Spline");
            splineObj.transform.SetParent(_circuitRoot.transform);
            
            _runtimeSplineContainer = splineObj.AddComponent<SplineContainer>();
            
            // Convertir SplinePoint[] → Unity Spline
            var spline = _runtimeSplineContainer.Spline;
            spline.Clear();
            
            foreach (var point in circuitData.splinePoints)
            {
                var knot = new BezierKnot(
                    point.position,
                    point.tangentIn,
                    point.tangentOut
                );
                spline.Add(knot);
            }
            
            spline.Closed = circuitData.closedLoop;
        }
        
        /// <summary>
        /// Initialise votre CheckpointManager existant avec la spline générée.
        /// </summary>
        private void InitializeCheckpointManager(CircuitData circuitData)
        {
            var checkpointManager = FindObjectOfType<CheckpointManager>();
            
            if (checkpointManager == null)
            {
                Debug.LogWarning("[CircuitManager] No CheckpointManager found in scene!");
                return;
            }
            
            // Si le CircuitData a des checkpoints sauvegardés, CheckpointManager les utilisera automatiquement
            // via TryLoadCheckpointsFromCircuitData() qui se base sur les positions relatives au spawn point
            
            // Sinon, on peut injecter la spline pour génération auto (mais cela peut causer des décalages)
            // Préférer sauvegarder les checkpoints dans l'éditeur pour garantir précision
            
            if (_showDebugInfo)
            {
                Debug.Log("[CircuitManager] CheckpointManager initialized. " +
                          $"CheckpointData available: {circuitData.checkpointData != null && circuitData.checkpointData.Length > 0}");
            }
        }
        
        #endregion
    }
}