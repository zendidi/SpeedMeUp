using UnityEngine;
using ArcadeRacer.Settings;
using ArcadeRacer.Core;
using ArcadeRacer.UI;
using ArcadeRacer.Managers;
using ArcadeRacer.RaceSystem;

namespace ArcadeRacer.Examples
{
    /// <summary>
    /// Exemple d'intégration complète du système de circuits.
    /// Montre comment utiliser CircuitDatabase, HighscoreManager et CircuitSelectionUI ensemble.
    /// </summary>
    public class CircuitSystemIntegrationExample : MonoBehaviour
    {
        [Header("=== UI REFERENCES ===")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private CircuitSelectionUI circuitSelectionUI;

        [Header("=== RACE REFERENCES ===")]
        [SerializeField] private RaceManager raceManager;

        [Header("=== SETTINGS ===")]
        [SerializeField] private bool showCircuitSelectionOnStart = true;

        private CircuitData _selectedCircuit;
        private bool _raceInProgress;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeReferences();
            SetupEventListeners();

            if (showCircuitSelectionOnStart)
            {
                ShowCircuitSelectionMenu();
            }
        }

        private void OnDestroy()
        {
            RemoveEventListeners();
        }

        #endregion

        #region Initialization

        private void InitializeReferences()
        {
            // Auto-find si non assigné
            if (uiManager == null)
            {
                uiManager = FindFirstObjectByType<UIManager>();
            }

            if (circuitSelectionUI == null)
            {
                circuitSelectionUI = FindFirstObjectByType<CircuitSelectionUI>();
            }

            if (raceManager == null)
            {
                raceManager = FindFirstObjectByType<RaceManager>();
            }
        }

        private void SetupEventListeners()
        {
            // Écouter la sélection de circuit
            if (circuitSelectionUI != null)
            {
                circuitSelectionUI.OnCircuitSelected.AddListener(OnCircuitSelected);
            }

            // Écouter la fin de course
            if (raceManager != null)
            {
                raceManager.OnRaceFinished += OnRaceFinished;
            }
        }

        private void RemoveEventListeners()
        {
            if (circuitSelectionUI != null)
            {
                circuitSelectionUI.OnCircuitSelected.RemoveListener(OnCircuitSelected);
            }

            if (raceManager != null)
            {
                raceManager.OnRaceFinished -= OnRaceFinished;
            }
        }

        #endregion

        #region Circuit Selection

        public void ShowCircuitSelectionMenu()
        {
            if (uiManager != null)
            {
                uiManager.ShowCircuitSelection();
            }
            else if (circuitSelectionUI != null)
            {
                circuitSelectionUI.Show();
            }

            Debug.Log("[CircuitSystemExample] Menu de sélection de circuits affiché");
        }

        private void OnCircuitSelected(CircuitData circuit)
        {
            if (circuit == null) return;

            _selectedCircuit = circuit;
            Debug.Log($"[CircuitSystemExample] Circuit sélectionné: {circuit.circuitName}");

            if (uiManager != null)
            {
                uiManager.HideCircuitSelection();
            }

            LoadAndStartCircuit(circuit);
        }

        #endregion

        #region Circuit Loading

        private void LoadAndStartCircuit(CircuitData circuit)
        {
            CircuitManager.Instance.LoadCircuit(circuit);
            DisplayBestTime(circuit.circuitName);

            if (raceManager != null)
            {
                raceManager.StartCountdown();
                _raceInProgress = true;
            }
        }

        #endregion

        #region Highscore System

        private void DisplayBestTime(string circuitName)
        {
            HighscoreEntry? bestTime = HighscoreManager.Instance.GetBestTime(circuitName);

            if (bestTime.HasValue)
            {
                Debug.Log($"[CircuitSystemExample] Record: {bestTime.Value.FormattedTime} par {bestTime.Value.playerName}");
            }
        }

        private void OnRaceFinished()
        {
            if (!_raceInProgress || _selectedCircuit == null)
                return;

            _raceInProgress = false;
            float finalTime = GetPlayerFinalTime();

            if (finalTime > 0)
            {
                CheckAndSaveHighscore(finalTime);
            }
        }

        private float GetPlayerFinalTime()
        {
            if (raceManager == null)
                return 0f;

            var finishedVehicles = raceManager.FinishedVehicles;
            if (finishedVehicles.Count > 0)
            {
                var playerVehicle = finishedVehicles[0];
                var timer = raceManager.GetVehicleTimer(playerVehicle);
                
                if (timer != null)
                {
                    return timer.TotalRaceTime;
                }
            }

            return 0f;
        }

        private void CheckAndSaveHighscore(float finalTime)
        {
            string circuitName = _selectedCircuit.circuitName;
            bool wouldBeTop = HighscoreManager.Instance.WouldBeTopScore(circuitName, finalTime);

            if (wouldBeTop)
            {
                Debug.Log($"[CircuitSystemExample] Nouveau record! {HighscoreEntry.FormatTime(finalTime)}");
                string playerName = "Player";
                
                bool added = HighscoreManager.Instance.TryAddScore(circuitName, finalTime, playerName);
                
                if (added)
                {
                    DisplayHighscoreTable(circuitName);
                }
            }
        }

        private void DisplayHighscoreTable(string circuitName)
        {
            var scores = HighscoreManager.Instance.GetHighscores(circuitName);
            Debug.Log($"===== HIGHSCORES - {circuitName} =====");
            
            foreach (var entry in scores)
            {
                Debug.Log($"{entry.rank}. {entry.FormattedTime} - {entry.playerName}");
            }
            
            Debug.Log("=====================================");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Debug: List All Circuits")]
        private void DebugListAllCircuits()
        {
            if (CircuitDatabase.Instance == null) return;

            Debug.Log($"===== CIRCUITS ({CircuitDatabase.Instance.CircuitCount}) =====");
            
            int index = 0;
            foreach (var circuit in CircuitDatabase.Instance.AvailableCircuits)
            {
                if (circuit != null)
                {
                    Debug.Log($"{index}. {circuit.circuitName} - {circuit.TotalLength:F1}m");
                }
                index++;
            }
        }

        [ContextMenu("Debug: Add Test Scores")]
        private void DebugAddTestScores()
        {
            if (CircuitDatabase.Instance == null || CircuitDatabase.Instance.CircuitCount == 0) return;

            var firstCircuit = CircuitDatabase.Instance.GetCircuitByIndex(0);
            if (firstCircuit != null)
            {
                string circuitName = firstCircuit.circuitName;
                
                HighscoreManager.Instance.TryAddScore(circuitName, 65.432f, "TestPlayer1");
                HighscoreManager.Instance.TryAddScore(circuitName, 62.123f, "TestPlayer2");
                HighscoreManager.Instance.TryAddScore(circuitName, 68.999f, "TestPlayer3");

                DisplayHighscoreTable(circuitName);
            }
        }

        #endregion
    }
}
