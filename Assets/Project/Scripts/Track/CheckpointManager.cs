using UnityEngine;
using Unity. Cinemachine;
using System.Collections.Generic;
using ArcadeRacer.Vehicle;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace ArcadeRacer. RaceSystem
{
    /// <summary>
    /// Gère tous les checkpoints du circuit.  
    /// Peut les placer automatiquement sur une Spline ou manuellement.  
    /// Vérifie que les joueurs passent les checkpoints dans l'ordre.
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        [Header("=== SPLINE SETUP ===")]
        [SerializeField, Tooltip("Spline du circuit (optionnel - pour placement auto)")]
        private SplineContainer splineContainer;

        [SerializeField, Tooltip("Nombre de checkpoints à générer")]
        private int checkpointCount = 10;

        [SerializeField, Tooltip("Prefab de checkpoint (optionnel)")]
        private GameObject checkpointPrefab;

        [Header("=== MANUAL SETUP ===")]
        [SerializeField, Tooltip("Si pas de spline, assigner les checkpoints manuellement")]
        private List<Checkpoint> manualCheckpoints = new List<Checkpoint>();

        [Header("=== SETTINGS ===")]
        [SerializeField, Tooltip("Largeur des checkpoints")]
        private float checkpointWidth = 15f;

        [SerializeField, Tooltip("Hauteur des checkpoints")]
        private float checkpointHeight = 5f;

        [SerializeField, Tooltip("Épaisseur des checkpoints")]
        private float checkpointDepth = 2f;

        // Runtime
        private List<Checkpoint> _checkpoints = new List<Checkpoint>();
        private Dictionary<VehicleController, int> _vehicleNextCheckpoint = new Dictionary<VehicleController, int>();
        private Dictionary<VehicleController, bool> _vehicleHasLeftStart = new Dictionary<VehicleController, bool>();

        #region Properties

        public int CheckpointCount => _checkpoints.Count;
        public List<Checkpoint> Checkpoints => _checkpoints;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Listen to CircuitManager events
            SubscribeToCircuitManager();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            UnsubscribeFromCircuitManager();
        }

        #endregion

        #region Circuit Manager Integration

        private void SubscribeToCircuitManager()
        {
            var circuitManager = FindFirstObjectByType<ArcadeRacer.Managers.CircuitManager>();
            if (circuitManager != null)
            {
                circuitManager.OnCircuitLoaded += OnCircuitLoadedHandler;
                Debug.Log("[CheckpointManager] Subscribed to CircuitManager events.");
            }
            else
            {
                Debug.LogWarning("[CheckpointManager] CircuitManager not found! Will try to initialize checkpoints from current state.");
                // Fallback: try to initialize with current state
                InitializeCheckpoints();
            }
        }

        private void UnsubscribeFromCircuitManager()
        {
            var circuitManager = FindFirstObjectByType<ArcadeRacer.Managers.CircuitManager>();
            if (circuitManager != null)
            {
                circuitManager.OnCircuitLoaded -= OnCircuitLoadedHandler;
            }
        }

        private void OnCircuitLoadedHandler(ArcadeRacer.Settings.CircuitData circuitData)
        {
            Debug.Log($"[CheckpointManager] Circuit loaded event received: '{circuitData.circuitName}'. Initializing checkpoints...");
            InitializeCheckpoints();
        }

        #endregion

        #region Initialization

        private void InitializeCheckpoints()
        {
            // Priority 1: Use saved checkpoint data from CircuitData if available
            if (TryLoadCheckpointsFromCircuitData())
            {
                Debug.Log($"[CheckpointManager] {_checkpoints.Count} checkpoints loaded from CircuitData.");
                return;
            }
            
            // Priority 2: Generate from CircuitData using mesh-based generation (more accurate)
            if (TryGenerateCheckpointsFromCircuitData())
            {
                Debug.Log($"[CheckpointManager] {_checkpoints.Count} checkpoints generated from CircuitData mesh.");
                return;
            }
            
            // Priority 3: Generate from spline (fallback, less accurate)
            if (splineContainer != null && checkpointCount > 0)
            {
                GenerateCheckpointsFromSpline();
            }
            // Priority 4: Use manual checkpoints
            else if (manualCheckpoints.Count > 0)
            {
                _checkpoints = new List<Checkpoint>(manualCheckpoints);
                
                // Réindexer
                for (int i = 0; i < _checkpoints.Count; i++)
                {
                    _checkpoints[i].Setup(i, i == 0);
                }
            }
            else
            {
                Debug.LogWarning("[CheckpointManager] Ni spline ni checkpoints manuels assignés!");
            }

            Debug.Log($"[CheckpointManager] {_checkpoints.Count} checkpoints initialisés.");
        }
        
        /// <summary>
        /// Try to generate checkpoints from CircuitData using the same mesh-based interpolation.
        /// This ensures checkpoints align perfectly with the visible mesh.
        /// </summary>
        private bool TryGenerateCheckpointsFromCircuitData()
        {
            Debug.Log("[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - Starting...");
            
            // Find CircuitManager to get current circuit data
            var circuitManager = FindFirstObjectByType<ArcadeRacer.Managers.CircuitManager>();
            if (circuitManager == null)
            {
                Debug.LogWarning("[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - CircuitManager not found!");
                return false;
            }
            
            if (circuitManager.CurrentCircuit == null)
            {
                Debug.LogWarning("[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - CircuitManager.CurrentCircuit is null!");
                return false;
            }
            
            var circuitData = circuitManager.CurrentCircuit;
            Debug.Log($"[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - Found circuit: '{circuitData.circuitName}'");
            
            // Check if we have checkpoint data saved - if yes, load saved positions
            if (circuitData.checkpointData != null && circuitData.checkpointData.Length > 0)
            {
                Debug.Log($"[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - Circuit has saved checkpoint data ({circuitData.checkpointData.Length} checkpoints). Loading saved positions...");
                return TryLoadCheckpointsFromCircuitData();
            }
            
            // Determine checkpoint count
            int cpCount = circuitData.autoCheckpointCount > 0 ? circuitData.autoCheckpointCount : checkpointCount;
            Debug.Log($"[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - Generating {cpCount} checkpoints from mesh interpolation...");
            
            // Generate checkpoints using the same method as mesh generation
            var checkpoints = ArcadeRacer.Utilities.CircuitMeshGenerator.GenerateAutoCheckpoints(
                circuitData,
                cpCount
            );
            
            if (checkpoints == null || checkpoints.Length == 0)
            {
                Debug.LogError("[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - Failed to generate checkpoints! CircuitMeshGenerator returned empty array.");
                return false;
            }
            
            Debug.Log($"[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - Successfully generated {checkpoints.Length} checkpoint positions.");
            
            // Clear existing checkpoints
            ClearGeneratedCheckpoints();
            
            // Create parent for organization
            GameObject checkpointsParent = new GameObject("Generated_Checkpoints");
            checkpointsParent.transform.parent = transform;
            
            // Create checkpoint GameObjects
            for (int i = 0; i < checkpoints.Length; i++)
            {
                var cpInfo = checkpoints[i];
                
                GameObject checkpointGO = CreateCheckpointGameObject(i, checkpointsParent.transform);
                checkpointGO.transform.position = cpInfo.position;
                checkpointGO.transform.rotation = cpInfo.rotation;
                
                Checkpoint checkpoint = checkpointGO.GetComponent<Checkpoint>();
                if (checkpoint != null)
                {
                    checkpoint.Setup(i, i == 0);
                    _checkpoints.Add(checkpoint);
                }
                else
                {
                    Debug.LogWarning($"[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - Checkpoint {i} missing Checkpoint component!");
                }
            }
            
            Debug.Log($"[CheckpointManager] TryGenerateCheckpointsFromCircuitData() - ✓ Successfully created {_checkpoints.Count} checkpoint GameObjects.");
            return true;
        }
        
        /// <summary>
        /// Try to load checkpoints from CircuitData (saved relative positions)
        /// </summary>
        private bool TryLoadCheckpointsFromCircuitData()
        {
            // Find CircuitManager to get current circuit data
            var circuitManager = FindFirstObjectByType<ArcadeRacer.Managers.CircuitManager>();
            if (circuitManager == null || circuitManager.CurrentCircuit == null)
                return false;
            
            var circuitData = circuitManager.CurrentCircuit;
            
            // Check if we have saved checkpoint data
            if (circuitData.checkpointData == null || circuitData.checkpointData.Length == 0)
                return false;
            
            // Clear existing checkpoints
            ClearGeneratedCheckpoints();
            
            // Create parent for organization
            GameObject checkpointsParent = new GameObject("Generated_Checkpoints");
            checkpointsParent.transform.parent = transform;
            
            // Recreate checkpoints from saved data
            foreach (var cpData in circuitData.checkpointData)
            {
                cpData.GetWorldTransform(circuitData.spawnPosition, circuitData.spawnRotation, out Vector3 worldPos, out Quaternion worldRot);
                
                GameObject checkpointGO = CreateCheckpointGameObject(cpData.index, checkpointsParent.transform);
                checkpointGO.transform.position = worldPos;
                checkpointGO.transform.rotation = worldRot;
                
                Checkpoint checkpoint = checkpointGO.GetComponent<Checkpoint>();
                if (checkpoint != null)
                {
                    checkpoint.Setup(cpData.index, cpData.isStartFinishLine);
                    _checkpoints.Add(checkpoint);
                }
            }
            
            return true;
        }

        private void GenerateCheckpointsFromSpline()
        {
            if (splineContainer == null)
            {
                Debug.LogError("[CheckpointManager] SplineContainer manquant!");
                return;
            }

            var spline = splineContainer. Spline;
            
            // Nettoyer les anciens checkpoints
            ClearGeneratedCheckpoints();

            // Créer un parent pour organiser
            GameObject checkpointsParent = new GameObject("Generated_Checkpoints");
            checkpointsParent.transform.parent = transform;

            for (int i = 0; i < checkpointCount; i++)
            {
                // Position normalisée sur la spline (0 à 1)
                float t = i / (float)checkpointCount;

                // Obtenir la position et rotation sur la spline
                splineContainer. Evaluate(spline, t, out float3 position, out float3 tangent, out float3 up);

                // Créer le checkpoint
                GameObject checkpointGO = CreateCheckpointGameObject(i, checkpointsParent. transform);
                checkpointGO.transform.position = position;
                checkpointGO.transform.rotation = Quaternion.LookRotation(tangent, up);

                // Configurer le component
                Checkpoint checkpoint = checkpointGO. GetComponent<Checkpoint>();
                if (checkpoint != null)
                {
                    checkpoint.Setup(i, i == 0); // Le premier est la ligne start/finish
                    _checkpoints.Add(checkpoint);
                }
            }

            Debug.Log($"[CheckpointManager] {checkpointCount} checkpoints générés sur la spline.");
        }

        private GameObject CreateCheckpointGameObject(int index, Transform parent)
        {
            GameObject go;

            // Utiliser le prefab si assigné
            if (checkpointPrefab != null)
            {
                go = Instantiate(checkpointPrefab, parent);
            }
            else
            {
                // Créer un checkpoint basique
                go = new GameObject();
                go.transform.parent = parent;
                
                // Ajouter les components nécessaires
                Checkpoint checkpoint = go.AddComponent<Checkpoint>();
                BoxCollider collider = go.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(checkpointWidth, checkpointHeight, checkpointDepth);
            }

            go.name = $"Checkpoint_{index}";
            return go;
        }

        private void ClearGeneratedCheckpoints()
        {
            // Supprimer les checkpoints générés précédemment
            Transform existing = transform.Find("Generated_Checkpoints");
            if (existing != null)
            {
                DestroyImmediate(existing. gameObject);
            }

            _checkpoints.Clear();
        }

        #endregion

        #region Checkpoint Tracking

        /// <summary>
        /// Appelé quand un véhicule passe un checkpoint
        /// </summary>
        public void OnCheckpointPassed(VehicleController vehicle, Checkpoint checkpoint)
        {
            // Initialiser le tracking pour ce véhicule si nécessaire
            if (!_vehicleNextCheckpoint.ContainsKey(vehicle))
            {
                _vehicleNextCheckpoint[vehicle] = 0;
            }
            
            if (!_vehicleHasLeftStart.ContainsKey(vehicle))
            {
                _vehicleHasLeftStart[vehicle] = false;
            }
            
            int expectedCheckpoint = _vehicleNextCheckpoint[vehicle];

            if (checkpoint.IsStartFinishLine || expectedCheckpoint == 1)
            {
                LapTimer lapTimer = vehicle.GetComponent<LapTimer>();
                if (lapTimer != null)
                {
                   lapTimer.Reset();
                    lapTimer.StartRace();
                }
            }


            // Vérifier si c'est le bon checkpoint
            if (checkpoint.Index == expectedCheckpoint)
            {
                // Checkpoint valide ! 
                _vehicleNextCheckpoint[vehicle] = (expectedCheckpoint + 1) % _checkpoints.Count;
                
                // Marquer que le véhicule a quitté la ligne de départ après avoir passé le premier checkpoint
                if (expectedCheckpoint > 0)
                {
                    _vehicleHasLeftStart[vehicle] = true;
                }
                
                // Enregistrer le temps intermédiaire (sauf pour le passage de la ligne d'arrivée qui complète le tour)
                // On skip si c'est la ligne start/finish ET que c'est le checkpoint 0 (= fin de tour, pas intermédiaire)
                if (!checkpoint.IsStartFinishLine || expectedCheckpoint != 0)
                {
                    LapTimer lapTimer = vehicle.GetComponent<LapTimer>();
                    if (lapTimer != null)
                    {
                        lapTimer.RecordCheckpoint();
                    }
                }

                // Si c'est la ligne d'arrivée ET que le véhicule a fait le tour complet, notifier le RaceManager
                if (checkpoint.IsStartFinishLine && expectedCheckpoint == 0 && _vehicleHasLeftStart[vehicle])
                {
                    OnLapCompleted(vehicle);
                    // Reset le flag pour le prochain tour
                    _vehicleHasLeftStart[vehicle] = false;
                }

                Debug.Log($"[CheckpointManager] {vehicle.name} passed checkpoint {checkpoint.Index} ✅");
            }
            else
            {
                // Checkpoint dans le mauvais ordre (triche ou erreur)
                Debug.LogWarning($"[CheckpointManager] {vehicle.name} passed checkpoint {checkpoint.Index} but expected {expectedCheckpoint} ❌");
            }
        }

        /// <summary>
        /// Obtenir le prochain checkpoint attendu pour un véhicule
        /// </summary>
        public int GetNextCheckpointIndex(VehicleController vehicle)
        {
            if (_vehicleNextCheckpoint.ContainsKey(vehicle))
            {
                return _vehicleNextCheckpoint[vehicle];
            }
            return 0;
        }

        /// <summary>
        /// Réinitialiser le tracking pour un véhicule
        /// </summary>
        public void ResetVehicleProgress(VehicleController vehicle)
        {
            _vehicleNextCheckpoint[vehicle] = 0;
            _vehicleHasLeftStart[vehicle] = false; // Le véhicule démarre à la ligne de départ
        }

        #endregion

        #region Lap Completion

        private void OnLapCompleted(VehicleController vehicle)
        {
            // Notifier le RaceManager
            RaceManager raceManager = FindFirstObjectByType<RaceManager>();
            if (raceManager != null)
            {
                raceManager. OnLapCompleted(vehicle);
            }

            Debug.Log($"🏁 [CheckpointManager] {vehicle. name} completed a lap!");
        }

        #endregion

        #region Editor Tools

        [ContextMenu("Generate Checkpoints from Spline")]
        public void GenerateCheckpointsEditor()
        {
            GenerateCheckpointsFromSpline();
        }

        [ContextMenu("Clear Generated Checkpoints")]
        public void ClearCheckpointsEditor()
        {
            ClearGeneratedCheckpoints();
        }
        [ContextMenu("Generate Checkpoints from CircuitData")]
        public void GenerateCheckpointsFromCircuitData()
        {
            var tryME =TryGenerateCheckpointsFromCircuitData();
            Debug.Log($"[CheckpointManager] TryGenerateCheckpointsFromCircuitData result: {tryME}");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Save current checkpoint positions to CircuitData (relative to spawn point)
        /// </summary>
        [ContextMenu("Save Checkpoints to CircuitData")]
        public void SaveCheckpointsToCircuitData()
        {
            var circuitManager = FindFirstObjectByType<ArcadeRacer.Managers.CircuitManager>();
            if (circuitManager == null || circuitManager.CurrentCircuit == null)
            {
                Debug.LogError("[CheckpointManager] No CircuitManager or CurrentCircuit found!");
                return;
            }
            
            var circuitData = circuitManager.CurrentCircuit;
            
            if (_checkpoints.Count == 0)
            {
                Debug.LogWarning("[CheckpointManager] No checkpoints to save!");
                return;
            }
            
            // Convert checkpoints to relative data
            var checkpointDataList = new List<ArcadeRacer.Settings.CheckpointData>();
            
            foreach (var checkpoint in _checkpoints)
            {
                var cpData = ArcadeRacer.Settings.CheckpointData.CreateRelativeToSpawn(
                    checkpoint.transform.position,
                    checkpoint.transform.rotation,
                    circuitData.spawnPosition,
                    circuitData.spawnRotation,
                    checkpoint.Index,
                    checkpoint.IsStartFinishLine
                );
                checkpointDataList.Add(cpData);
            }
            
            circuitData.checkpointData = checkpointDataList.ToArray();
            
            // Mark asset as dirty to save changes
            UnityEditor.EditorUtility.SetDirty(circuitData);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"[CheckpointManager] Saved {checkpointDataList.Count} checkpoints to {circuitData.name}");
        }
#endif

        #endregion
    }
}