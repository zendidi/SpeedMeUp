using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ArcadeRacer.Vehicle;
namespace ArcadeRacer.RaceSystem
{
    /// <summary>
    /// Track la position des véhicules de manière précise.   
    /// Utilise la distance parcourue sur la spline pour un classement exact.
    /// Optionnel mais améliore la précision du classement.
    /// </summary>
    public class RacePositionTracker : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private CheckpointManager checkpointManager;

        [Header("=== SETTINGS ===")]
        [SerializeField, Tooltip("Fréquence de mise à jour (fois par seconde)")]
        private float updateFrequency = 10f;

        // Runtime
        private Dictionary<VehicleController, float> _vehicleProgress = new Dictionary<VehicleController, float>();
        private float _updateTimer;

        #region Unity Lifecycle

        private void Awake()
        {
            if (raceManager == null)
            {
                raceManager = FindFirstObjectByType<RaceManager>();
            }

            if (checkpointManager == null)
            {
                checkpointManager = FindFirstObjectByType<CheckpointManager>();
            }
        }

        private void Update()
        {
            if (raceManager == null || ! raceManager.IsRacing) return;

            _updateTimer += Time.deltaTime;

            if (_updateTimer >= 1f / updateFrequency)
            {
                UpdateVehicleProgress();
                _updateTimer = 0f;
            }
        }

        #endregion

        #region Progress Tracking

        private void UpdateVehicleProgress()
        {
            // Pour chaque véhicule, calculer sa progression totale
            // Progression = (tours complétés * nombre de checkpoints) + prochain checkpoint
            
            var standings = raceManager.GetCurrentStandings();

            foreach (var vehicle in standings)
            {
                int currentLap = raceManager. GetVehicleLap(vehicle);
                int nextCheckpoint = checkpointManager.GetNextCheckpointIndex(vehicle);
                int totalCheckpoints = checkpointManager.CheckpointCount;

                // Progression totale (en nombre de checkpoints franchis)
                float progress = (currentLap * totalCheckpoints) + nextCheckpoint;

                _vehicleProgress[vehicle] = progress;
            }
        }

        /// <summary>
        /// Obtenir la progression d'un véhicule (en checkpoints franchis)
        /// </summary>
        public float GetVehicleProgress(VehicleController vehicle)
        {
            if (_vehicleProgress.ContainsKey(vehicle))
            {
                return _vehicleProgress[vehicle];
            }
            return 0f;
        }

        /// <summary>
        /// Obtenir le classement précis basé sur la progression
        /// </summary>
        public List<VehicleController> GetPreciseStandings()
        {
            return _vehicleProgress
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Obtenir la position précise d'un véhicule
        /// </summary>
        public int GetVehiclePrecisePosition(VehicleController vehicle)
        {
            var standings = GetPreciseStandings();
            return standings.IndexOf(vehicle) + 1;
        }

        #endregion
    }
}