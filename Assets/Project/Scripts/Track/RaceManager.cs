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

        [Header("=== WARMUP LAP ===")]
        [SerializeField, Tooltip("Activer le tour de formation : le premier tour ne sera pas comptabilisé")]
        private bool enableWarmupLap = false;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugInfo = true;

        // Runtime
        private Dictionary<VehicleController, LapTimer> _vehicleTimers = new Dictionary<VehicleController, LapTimer>();
        private Dictionary<VehicleController, int> _vehicleLaps = new Dictionary<VehicleController, int>();
        private Dictionary<VehicleController, bool> _warmupLapActive = new Dictionary<VehicleController, bool>();
        private List<VehicleController> _finishedVehicles = new List<VehicleController>();

        // Highscore name input context
        private VehicleController _pendingHighscoreVehicle;
        private float _pendingHighscoreLapTime;
        private string _pendingHighscoreCircuitName;
        private float[] _pendingCheckpointTimes; // NOUVEAU: sauvegarder les checkpoint times immédiatement
        [SerializeField, Tooltip("UI de saisie du nom pour les highscores")]
        private ArcadeRacer.UI.HighscoreNameInputUI _highscoreNameInputUI;

        // Qualifying laps accumulated during the race, shown at race end
        private List<(float lapTime, float[] checkpointTimes, string timeOfDay)> _pendingHighscoreLaps = new List<(float, float[], string)>();

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
        public int TotalLaps
        {
            get => totalLaps;
            set => totalLaps = Mathf.Max(1, value);
        }
        public float CountdownTimer => _countdownTimer;
        public float CountdownDuration => countdownDuration;
        public bool IsRacing => _currentState == RaceState.Racing;
        public List<VehicleController> FinishedVehicles => _finishedVehicles;

        /// <summary>
        /// Active ou désactive le tour de formation.
        /// Peut être modifié à tout moment avant le démarrage de la course.
        /// </summary>
        public bool EnableWarmupLap
        {
            get => enableWarmupLap;
            set
            {
                enableWarmupLap = value;

                // Mettre à jour les véhicules déjà enregistrés si la course n'a pas encore commencé
                if (_currentState == RaceState.NotStarted)
                {
                    foreach (var vehicle in racingVehicles)
                    {
                        if (_warmupLapActive.ContainsKey(vehicle))
                        {
                            _warmupLapActive[vehicle] = value;
                        }
                    }
                }

                Debug.Log($"[RaceManager] EnableWarmupLap → {value}");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeReferences();
            SetupVehicles();
            SetupHighscoreNameInput();
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

        private void OnDestroy()
        {
            // Cleanup highscore name input events
            if (_highscoreNameInputUI != null)
            {
                _highscoreNameInputUI.OnNameSubmitted -= OnPlayerNameSubmitted;
                _highscoreNameInputUI.OnCancelled -= OnPlayerNameCancelled;
            }
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
                _warmupLapActive[vehicle] = enableWarmupLap;

                // Désactiver le contrôle au départ
                vehicle.SetControllable(false);
            }

            Debug.Log($"[RaceManager] {racingVehicles.Count} véhicule(s) configuré(s).");
        }

        private void SetupHighscoreNameInput()
        {
            // Trouver et cacher l'UI de saisie du nom
            
            if (_highscoreNameInputUI != null)
            {
                // Subscribe aux événements une seule fois
                _highscoreNameInputUI.OnNameSubmitted += OnPlayerNameSubmitted;
                _highscoreNameInputUI.OnCancelled += OnPlayerNameCancelled;
                
                Debug.Log("[RaceManager] HighscoreNameInputUI initialisé et événements abonnés");
            }
            else
            {
                Debug.LogWarning("[RaceManager] HighscoreNameInputUI non trouvé - la fonctionnalité de saisie de nom ne sera pas disponible");
            }
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

            // Afficher le modal de saisie du nom une seule fois si des tours qualifiants existent
            if (_pendingHighscoreLaps.Count > 0)
            {
                if (_highscoreNameInputUI != null)
                {
                    // Afficher avec le meilleur temps qualifiant
                    float bestTime = float.MaxValue;
                    foreach (var lap in _pendingHighscoreLaps)
                        if (lap.lapTime < bestTime) bestTime = lap.lapTime;

                    _highscoreNameInputUI.Show(bestTime, _pendingHighscoreCircuitName);
                }
                else
                {
                    Debug.LogWarning("[RaceManager] HighscoreNameInputUI non disponible! Utilise le nom par défaut.");
                    SaveAllPendingHighscores("Player");
                }
            }
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
                _warmupLapActive[vehicle] = enableWarmupLap;
                _vehicleTimers[vehicle].Reset();
                checkpointManager?.ResetVehicleProgress(vehicle);
                CircuitManager.Instance.SpawnVehicle(vehicle.transform);
                // Téléporter au spawn point
               // vehicle.ResetToSpawnPoint();
            }

            _finishedVehicles.Clear();
            _pendingHighscoreLaps.Clear();
            _currentState = RaceState.NotStarted;
            CheckpointManager cpM= FindFirstObjectByType<CheckpointManager>();
            if (cpM != null) 
                cpM.TryGenerateCheckpointsFromCircuitData(); 
            
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

            // Vérifier si le tour de formation est actif pour ce véhicule
            if (IsWarmupLapActive(vehicle))
            {
                // Le tour de formation est terminé : ne pas compter le tour ni enregistrer le temps
                _warmupLapActive[vehicle] = false;

                // Réinitialiser le chrono pour repartir de zéro sur le vrai premier tour
                if (_vehicleTimers.ContainsKey(vehicle))
                {
                    _vehicleTimers[vehicle].ResetLapForWarmup();
                }

                Debug.Log($"[RaceManager] {vehicle.name} - Tour de formation terminé, le tour ne sera pas comptabilisé.");
                return;
            }

            // Incrémenter le compteur de tours
            _vehicleLaps[vehicle]++;
            int currentLap = _vehicleLaps[vehicle];

            // Notifier le timer et récupérer le temps du tour
            float lapTime = 0f;
            if (_vehicleTimers.ContainsKey(vehicle))
            {
                _vehicleTimers[vehicle].CompleteLap();
                lapTime = _vehicleTimers[vehicle].LastLapTime;
            }

            Debug.Log($"🏁 [RaceManager] {vehicle.name} completed lap {currentLap}/{totalLaps}");

            // Vérifier si ce temps est un top 10 et demander le nom du joueur
            CheckAndPromptForHighscore(vehicle, lapTime);

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

        /// <summary>
        /// Sauvegarde le meilleur temps au tour dans le HighscoreManager
        /// </summary>
        private void SaveBestLapToHighscores(VehicleController vehicle)
        {
            if (!_vehicleTimers.ContainsKey(vehicle)) return;

            var timer = _vehicleTimers[vehicle];
            float bestLapTime = timer.BestLapTime;

            // Vérifier qu'on a un temps valide
            if (bestLapTime <= 0f)
            {
                Debug.LogWarning($"[RaceManager] No valid lap time for {vehicle.name}");
                return;
            }

            // Obtenir le nom du circuit depuis CircuitManager
            var circuitManager = ArcadeRacer.Managers.CircuitManager.Instance;
            if (circuitManager == null || circuitManager.CurrentCircuit == null)
            {
                Debug.LogWarning("[RaceManager] CircuitManager ou CurrentCircuit introuvable - impossible de sauvegarder le highscore");
                return;
            }

            string circuitName = circuitManager.CurrentCircuit.circuitName;
            
            // Obtenir le nom du joueur (pour l'instant utilise le nom du véhicule, peut être remplacé par un input UI)
            string playerName = vehicle.name;

            // Obtenir les temps de checkpoints du meilleur tour
            // On identifie d'abord quel tour a le meilleur temps, puis on extrait ses checkpoints
            float[] checkpointTimes = null;
            var allLapCheckpoints = timer.AllLapsCheckpointTimes;
            if (allLapCheckpoints.Count > 0)
            {
                // Trouver l'index du meilleur tour
                var lapTimes = timer.LapTimes;
                int bestLapIndex = -1;
                float bestTime = float.MaxValue;
                for (int i = 0; i < lapTimes.Count; i++)
                {
                    if (lapTimes[i] < bestTime)
                    {
                        bestTime = lapTimes[i];
                        bestLapIndex = i;
                    }
                }

                // Récupérer les temps de checkpoints du meilleur tour
                if (bestLapIndex >= 0 && bestLapIndex < allLapCheckpoints.Count)
                {
                    checkpointTimes = allLapCheckpoints[bestLapIndex].ToArray();
                }
            }

            // Sauvegarder dans le HighscoreManager
            bool isTopScore = ArcadeRacer.Core.HighscoreManager.Instance.TryAddScore(
                circuitName,
                bestLapTime,
                playerName,
                checkpointTimes
            );

            if (isTopScore)
            {
                Debug.Log($"🏆 [RaceManager] Nouveau highscore pour {circuitName}: {LapTimer.FormatTime(bestLapTime)} - {playerName}");
            }
            else
            {
                Debug.Log($"[RaceManager] Temps enregistré pour {circuitName}: {LapTimer.FormatTime(bestLapTime)} - {playerName}");
            }
        }

        /// <summary>
        /// Vérifie si le temps au tour est un top 10 et l'enregistre pour la fin de course
        /// </summary>
        private void CheckAndPromptForHighscore(VehicleController vehicle, float lapTime)
        {
            // Vérifier qu'on a un temps valide
            if (lapTime <= 0f)
            {
                Debug.LogWarning($"[RaceManager] Temps invalide: {lapTime}");
                return;
            }

            // Obtenir le nom du circuit
            var circuitManager = ArcadeRacer.Managers.CircuitManager.Instance;
            if (circuitManager == null || circuitManager.CurrentCircuit == null)
            {
                Debug.LogWarning("[RaceManager] CircuitManager ou CurrentCircuit null!");
                return;
            }

            string circuitName = circuitManager.CurrentCircuit.circuitName;

            // Vérifier si ce temps ferait partie du top 10
            bool wouldBeTopScore = ArcadeRacer.Core.HighscoreManager.Instance.WouldBeTopScore(circuitName, lapTime);

            if (wouldBeTopScore)
            {
                Debug.Log($"🏆 [RaceManager] Temps qualifiant pour le top 10: {LapTimer.FormatTime(lapTime)} sur {circuitName}");

                // Sauvegarder les checkpoint times de ce tour
                float[] checkpointTimes = null;
                if (_vehicleTimers.ContainsKey(vehicle))
                {
                    var timer = _vehicleTimers[vehicle];
                    var allLapCheckpoints = timer.AllLapsCheckpointTimes;
                    if (allLapCheckpoints.Count > 0)
                    {
                        checkpointTimes = allLapCheckpoints[allLapCheckpoints.Count - 1].ToArray();
                        Debug.Log($"[RaceManager] Checkpoint times sauvegardés: {checkpointTimes.Length} checkpoints pour le lap");
                    }
                    else
                    {
                        Debug.LogWarning("[RaceManager] Aucun checkpoint time trouvé dans AllLapsCheckpointTimes!");
                    }
                }

                // Accumuler le tour qualifiant pour la fin de course
                _pendingHighscoreLaps.Add((lapTime, checkpointTimes, System.DateTime.Now.ToString("HH:mm:ss")));
                _pendingHighscoreCircuitName = circuitName;
            }
        }

        /// <summary>
        /// Appelé quand le joueur soumet son nom
        /// </summary>
        private void OnPlayerNameSubmitted(string playerName)
        {
            Debug.Log($"[RaceManager] Nom du joueur reçu: {playerName}");
            SaveAllPendingHighscores(playerName);
            ClearPendingHighscoreContext();
        }

        /// <summary>
        /// Appelé quand le joueur annule la saisie
        /// </summary>
        private void OnPlayerNameCancelled()
        {
            Debug.Log("[RaceManager] Saisie du nom annulée, utilise le nom par défaut");
            SaveAllPendingHighscores("Player");
            ClearPendingHighscoreContext();
        }

        /// <summary>
        /// Sauvegarde tous les tours qualifiants en attente avec le nom du joueur
        /// </summary>
        private void SaveAllPendingHighscores(string playerName)
        {
            foreach (var lap in _pendingHighscoreLaps)
            {
                SaveLapTimeToHighscores(playerName, lap.lapTime, _pendingHighscoreCircuitName, lap.checkpointTimes, lap.timeOfDay);
            }
            _pendingHighscoreLaps.Clear();
        }

        /// <summary>
        /// Nettoie le contexte du highscore en attente
        /// </summary>
        private void ClearPendingHighscoreContext()
        {
            _pendingHighscoreVehicle = null;
            _pendingHighscoreLapTime = 0f;
            _pendingHighscoreCircuitName = null;
            _pendingCheckpointTimes = null;
        }

        /// <summary>
        /// Sauvegarde un temps au tour dans le HighscoreManager
        /// </summary>
        private void SaveLapTimeToHighscores(string playerName, float lapTime, string circuitName, float[] checkpointTimes, string timeOfDay = null)
        {
            Debug.Log($"[RaceManager] SaveLapTimeToHighscores appelé: {playerName}, {LapTimer.FormatTime(lapTime)}, {circuitName}, checkpoints: {(checkpointTimes != null ? checkpointTimes.Length : 0)}");
            
            // Sauvegarder dans le HighscoreManager
            bool isTopScore = ArcadeRacer.Core.HighscoreManager.Instance.TryAddScore(
                circuitName,
                lapTime,
                playerName,
                checkpointTimes,
                timeOfDay
            );

            if (isTopScore)
            {
                Debug.Log($"🏆 [RaceManager] Highscore sauvegardé: {LapTimer.FormatTime(lapTime)} - {playerName} sur {circuitName}");
            }
            else
            {
                Debug.LogWarning($"[RaceManager] Échec de la sauvegarde du highscore pour {playerName}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Indique si le tour de formation est actif pour un véhicule donné
        /// </summary>
        public bool IsWarmupLapActive(VehicleController vehicle)
        {
            return _warmupLapActive.ContainsKey(vehicle) && _warmupLapActive[vehicle];
        }

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
                _warmupLapActive[vehicle] = enableWarmupLap;

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