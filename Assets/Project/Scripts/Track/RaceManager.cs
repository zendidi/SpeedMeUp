using ArcadeRacer.Managers;
using ArcadeRacer.Settings;
using ArcadeRacer.Vehicle;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcadeRacer.RaceSystem
{
    /// <summary>
    /// Gestionnaire principal de la course.   
    /// Coordonne les checkpoints, les timers, les positions.   
    /// Gère le départ, l'arrivée, et le classement.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        [Header("=== RACE SETTINGS ===")]
        [SerializeField, Tooltip("Nombre de tours pour terminer la course")]
        private int totalLaps = 3;

        [SerializeField, Tooltip("Compte à rebours avant le départ (secondes)")]
        private float countdownDuration = 3f;

        [SerializeField, Tooltip("Démarrage automatique au Start")]
        private bool autoStart = true;

        [Header("=== REFERENCES ===")]
        [SerializeField] private CheckpointManager checkpointManager;
        
        [Header("=== CIRCUIT (Optional - for auto-load) ===")]
        [SerializeField, Tooltip("Circuit à charger automatiquement au démarrage (optionnel - peut être chargé via CircuitManager)")]
        private CircuitData circuitToAutoLoad;

        [Header("=== VEHICLES ===")]
        [SerializeField, Tooltip("Liste des véhicules participants")]
        private List<VehicleController> racingVehicles = new List<VehicleController>();

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugInfo = true;

        // Runtime
        private Dictionary<VehicleController, LapTimer> _vehicleTimers = new Dictionary<VehicleController, LapTimer>();
        private Dictionary<VehicleController, int> _vehicleLaps = new Dictionary<VehicleController, int>();
        private List<VehicleController> _finishedVehicles = new List<VehicleController>();

        private RaceState _currentState = RaceState.NotStarted;
        private float _countdownTimer;

        // Events
        public event System.Action OnCountdownStarted;
        public event System.Action OnRaceStarted;
        public event System.Action OnRaceFinished;

        public enum RaceState
        {
            NotStarted,
            Countdown,
            Racing,
            Finished
        }

        #region Properties

        public RaceState CurrentState => _currentState;
        public int TotalLaps => totalLaps;
        public float CountdownTimer => _countdownTimer;
        public float CountdownDuration => countdownDuration;
        public bool IsRacing => _currentState == RaceState.Racing;
        public List<VehicleController> FinishedVehicles => _finishedVehicles;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeReferences();
            SetupVehicles();
        }

        private void Start()
        {
            // Auto-load circuit if specified
            if (circuitToAutoLoad != null)
            {
                Debug.Log($"[RaceManager] Auto-loading circuit '{circuitToAutoLoad.circuitName}'...");
                ArcadeRacer.Managers.CircuitManager.Instance.LoadCircuit(circuitToAutoLoad);
            }
            
            if (autoStart)
            {
                StartCountdown();
            }
            
            // Spawn le véhicule at circuit spawn point
            var vehicle = FindFirstObjectByType<VehicleController>();
            if (vehicle != null)
            {
                ArcadeRacer.Managers.CircuitManager.Instance.SpawnVehicle(vehicle.transform);
            }
        }

        private void Update()
        {
            UpdateCountdown();
        }

        #endregion

        #region Initialization

        private void InitializeReferences()
        {
            if (checkpointManager == null)
            {
                checkpointManager = FindFirstObjectByType<CheckpointManager>();
            }

            if (checkpointManager == null)
            {
                Debug.LogError("[RaceManager] CheckpointManager introuvable!");
            }

            // Auto-détecter les véhicules si liste vide
            if (racingVehicles.Count == 0)
            {
                racingVehicles = new List<VehicleController>(FindObjectsByType<VehicleController>(FindObjectsSortMode.None));
                Debug.LogWarning($"[RaceManager] Auto-détection:  {racingVehicles.Count} véhicule(s) trouvé(s).");
            }
        }

        private void SetupVehicles()
        {
            foreach (var vehicle in racingVehicles)
            {
                // Ajouter un LapTimer si pas déjà présent
                LapTimer timer = vehicle.GetComponent<LapTimer>();
                if (timer == null)
                {
                    timer = vehicle.gameObject.AddComponent<LapTimer>();
                }

                _vehicleTimers[vehicle] = timer;
                _vehicleLaps[vehicle] = 0;

                // Désactiver le contrôle au départ
                vehicle.SetControllable(false);
            }

            Debug.Log($"[RaceManager] {racingVehicles.Count} véhicule(s) configuré(s).");
        }

        #endregion

        #region Race Control

        /// <summary>
        /// Démarrer le compte à rebours
        /// </summary>
        public void StartCountdown()
        {
            if (_currentState != RaceState.NotStarted)
            {
                Debug.LogWarning("[RaceManager] La course a déjà commencé!");
                return;
            }

            _currentState = RaceState.Countdown;
            _countdownTimer = countdownDuration;

            // Notifier l'UI
            OnCountdownStarted?.Invoke();

            Debug.Log($"[RaceManager] Countdown started:  {countdownDuration}s");
        }

        private void UpdateCountdown()
        {
            if (_currentState != RaceState.Countdown) return;

            _countdownTimer -= Time.deltaTime;

            // Afficher le compte à rebours (TODO: UI)
            int secondsRemaining = Mathf.CeilToInt(_countdownTimer);
            if (_countdownTimer > 0 && secondsRemaining != Mathf.CeilToInt(_countdownTimer + Time.deltaTime))
            {
                Debug.Log($"[RaceManager] {secondsRemaining}...");
            }

            // Démarrer la course
            if (_countdownTimer <= 0f)
            {
                StartRace();
            }
        }

        /// <summary>
        /// Démarrer la course immédiatement
        /// </summary>
        public void StartRace()
        {
            _currentState = RaceState.Racing;

            // Activer les véhicules
            foreach (var vehicle in racingVehicles)
            {
                vehicle.SetControllable(true);

                // Démarrer le timer
                if (_vehicleTimers.ContainsKey(vehicle))
                {
                    _vehicleTimers[vehicle].StartRace();
                }

                // Reset checkpoint progress
                checkpointManager?.ResetVehicleProgress(vehicle);
            }

            // Notifier l'UI
            OnRaceStarted?.Invoke();

            Debug.Log("🏁 [RaceManager] GO!  Race started!");
        }

        /// <summary>
        /// Terminer la course
        /// </summary>
        private void FinishRace()
        {
            _currentState = RaceState.Finished;

            // Notifier l'UI
            OnRaceFinished?.Invoke();

            Debug.Log("🏆 [RaceManager] Race finished!");
            DisplayFinalResults();
        }

        /// <summary>
        /// Redémarrer la course
        /// </summary>
        public void RestartRace()
        {
            // Reset tous les véhicules
            foreach (var vehicle in racingVehicles)
            {
                vehicle.SetControllable(false);
                _vehicleLaps[vehicle] = 0;
                _vehicleTimers[vehicle].Reset();
                checkpointManager?.ResetVehicleProgress(vehicle);

                // Téléporter au spawn point
                vehicle.ResetToSpawnPoint();
            }

            _finishedVehicles.Clear();
            _currentState = RaceState.NotStarted;

            Debug.Log("[RaceManager] Race restarted!");

            if (autoStart)
            {
                StartCountdown();
            }
        }

        #endregion

        #region Lap Management

        /// <summary>
        /// Appelé par le CheckpointManager quand un véhicule termine un tour
        /// </summary>
        public void OnLapCompleted(VehicleController vehicle)
        {
            if (_currentState != RaceState.Racing) return;

            if (!_vehicleLaps.ContainsKey(vehicle))
            {
                Debug.LogWarning($"[RaceManager] Véhicule {vehicle.name} non enregistré!");
                return;
            }

            // Incrémenter le compteur de tours
            _vehicleLaps[vehicle]++;
            int currentLap = _vehicleLaps[vehicle];

            // Notifier le timer
            if (_vehicleTimers.ContainsKey(vehicle))
            {
                _vehicleTimers[vehicle].CompleteLap();
            }

            Debug.Log($"🏁 [RaceManager] {vehicle.name} completed lap {currentLap}/{totalLaps}");

            // Vérifier si le véhicule a terminé la course
            if (currentLap >= totalLaps)
            {
                OnVehicleFinished(vehicle);
            }
        }

        /// <summary>
        /// Obtenir le tour actuel d'un véhicule
        /// </summary>
        public int GetVehicleLap(VehicleController vehicle)
        {
            if (_vehicleLaps.ContainsKey(vehicle))
            {
                return _vehicleLaps[vehicle];
            }
            return 0;
        }

        #endregion

        #region Finish Management

        private void OnVehicleFinished(VehicleController vehicle)
        {
            if (_finishedVehicles.Contains(vehicle)) return;

            _finishedVehicles.Add(vehicle);

            // Position finale
            int position = _finishedVehicles.Count;

            // Terminer le timer
            if (_vehicleTimers.ContainsKey(vehicle))
            {
                _vehicleTimers[vehicle].FinishRace();
            }

            Debug.Log($"🏆 [RaceManager] {vehicle.name} finished in position {position}!");

            // Si tous les véhicules ont terminé
            if (_finishedVehicles.Count >= racingVehicles.Count)
            {
                FinishRace();
            }
        }

        private void DisplayFinalResults()
        {
            Debug.Log("====== FINAL RESULTS ======");
            for (int i = 0; i < _finishedVehicles.Count; i++)
            {
                var vehicle = _finishedVehicles[i];
                var timer = _vehicleTimers[vehicle];

                Debug.Log($"{i + 1}. {vehicle.name} - Total: {LapTimer.FormatTime(timer.TotalRaceTime)} | Best Lap: {LapTimer.FormatTime(timer.BestLapTime)}");
            }
            Debug.Log("===========================");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Ajouter un véhicule à la course
        /// </summary>
        public void RegisterVehicle(VehicleController vehicle)
        {
            if (!racingVehicles.Contains(vehicle))
            {
                racingVehicles.Add(vehicle);

                LapTimer timer = vehicle.GetComponent<LapTimer>();
                if (timer == null)
                {
                    timer = vehicle.gameObject.AddComponent<LapTimer>();
                }

                _vehicleTimers[vehicle] = timer;
                _vehicleLaps[vehicle] = 0;

                Debug.Log($"[RaceManager] {vehicle.name} registered for race.");
            }
        }

        /// <summary>
        /// Retirer un véhicule de la course
        /// </summary>
        public void UnregisterVehicle(VehicleController vehicle)
        {
            racingVehicles.Remove(vehicle);
            _vehicleTimers.Remove(vehicle);
            _vehicleLaps.Remove(vehicle);
            _finishedVehicles.Remove(vehicle);
        }

        /// <summary>
        /// Obtenir le timer d'un véhicule
        /// </summary>
        public LapTimer GetVehicleTimer(VehicleController vehicle)
        {
            if (_vehicleTimers.ContainsKey(vehicle))
            {
                return _vehicleTimers[vehicle];
            }
            return null;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [System.Serializable]
        private class DebugInfo
        {
            public RaceState currentState;
            public float countdownTimer;
            public int vehiclesRegistered;
            public int vehiclesFinished;
            public List<string> standings = new List<string>();
        }

        [SerializeField]
        private DebugInfo _debugInfo = new DebugInfo();

        private void LateUpdate()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            _debugInfo.currentState = _currentState;
            _debugInfo.countdownTimer = _countdownTimer;
            _debugInfo.vehiclesRegistered = racingVehicles.Count;
            _debugInfo.vehiclesFinished = _finishedVehicles.Count;

            // Standings
            _debugInfo.standings.Clear();
            var sortedVehicles = GetCurrentStandings();
            for (int i = 0; i < sortedVehicles.Count; i++)
            {
                var vehicle = sortedVehicles[i];
                int lap = _vehicleLaps.ContainsKey(vehicle) ? _vehicleLaps[vehicle] : 0;
                _debugInfo.standings.Add($"{i + 1}. {vehicle.name} - Lap {lap}/{totalLaps}");
            }
        }
#endif

        #endregion

        #region Position Tracking

        /// <summary>
        /// Obtenir le classement actuel
        /// </summary>
        public List<VehicleController> GetCurrentStandings()
        {
            // Trier par:  tours complétés, puis prochain checkpoint, puis distance au prochain checkpoint
            return racingVehicles
                .OrderByDescending(v => _vehicleLaps.ContainsKey(v) ? _vehicleLaps[v] : 0)
                .ThenByDescending(v => checkpointManager?.GetNextCheckpointIndex(v) ?? 0)
                .ToList();
        }

        /// <summary>
        /// Obtenir la position actuelle d'un véhicule
        /// </summary>
        public int GetVehiclePosition(VehicleController vehicle)
        {
            var standings = GetCurrentStandings();
            return standings.IndexOf(vehicle) + 1;
        }

        #endregion
    }
}