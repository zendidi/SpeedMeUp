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

        #region Properties

        public int CheckpointCount => _checkpoints.Count;
        public List<Checkpoint> Checkpoints => _checkpoints;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeCheckpoints();
        }

        #endregion

        #region Initialization

        private void InitializeCheckpoints()
        {
            // Mode automatique :  générer depuis la spline
            if (splineContainer != null && checkpointCount > 0)
            {
                GenerateCheckpointsFromSpline();
            }
            // Mode manuel : utiliser les checkpoints assignés
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

            int expectedCheckpoint = _vehicleNextCheckpoint[vehicle];

            // Vérifier si c'est le bon checkpoint
            if (checkpoint.Index == expectedCheckpoint)
            {
                // Checkpoint valide ! 
                _vehicleNextCheckpoint[vehicle] = (expectedCheckpoint + 1) % _checkpoints.Count;

                // Si c'est la ligne d'arrivée, notifier le RaceManager
                if (checkpoint. IsStartFinishLine && expectedCheckpoint == 0)
                {
                    OnLapCompleted(vehicle);
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

        #endregion
    }
}