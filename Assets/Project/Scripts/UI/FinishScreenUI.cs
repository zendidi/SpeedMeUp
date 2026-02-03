using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ArcadeRacer.RaceSystem;
using ArcadeRacer.Vehicle;
using System.Collections. Generic;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Écran affiché à la fin de la course avec les résultats.
    /// </summary>
    public class FinishScreenUI : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private RaceManager raceManager;

        [Header("=== UI ELEMENTS ===")]
        [SerializeField] private GameObject finishPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI totalTimeText;
        [SerializeField] private TextMeshProUGUI bestLapText;
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        [Header("=== SETTINGS ===")]
        [SerializeField] private string winTitle = "🏆 VICTORY!";
        [SerializeField] private string loseTitle = "RACE FINISHED";

        private VehicleController _playerVehicle;

        #region Unity Lifecycle

        private void Awake()
        {
            if (raceManager == null)
            {
                raceManager = FindFirstObjectByType<RaceManager>();
            }

            _playerVehicle = FindFirstObjectByType<VehicleController>();

            // Setup buttons
            if (restartButton != null)
            {
                restartButton. onClick.AddListener(OnRestartClicked);
            }

            if (quitButton != null)
            {
                quitButton. onClick.AddListener(OnQuitClicked);
            }
        }

        private void Start()
        {
            // Hide();  // ❌ COMMENTE OU SUPPRIME

            if (finishPanel != null)
            {
                finishPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Afficher quand la course est terminée
            if (raceManager != null && raceManager.CurrentState == RaceManager.RaceState.Finished)
            {
                if (finishPanel != null && ! finishPanel.activeSelf)
                {
                    ShowResults();
                }
            }
        }

        #endregion

        #region Display

        private void ShowResults()
        {
            if (finishPanel != null) finishPanel.SetActive(true);

            if (_playerVehicle == null || raceManager == null) return;

            LapTimer timer = raceManager.GetVehicleTimer(_playerVehicle);
            int position = raceManager.GetVehiclePosition(_playerVehicle);

            // Title
            if (titleText != null)
            {
                titleText. text = position == 1 ? winTitle :  loseTitle;
            }

            // Position
            if (positionText != null)
            {
                positionText.text = $"Position: {GetPositionSuffix(position)}";
            }

            // Total time
            if (totalTimeText != null && timer != null)
            {
                totalTimeText.text = $"Total Time: {LapTimer.FormatTime(timer.TotalRaceTime)}";
            }

            // Best lap
            if (bestLapText != null && timer != null)
            {
                bestLapText.text = $"Best Lap: {LapTimer.FormatTime(timer.BestLapTime)}";
            }
        }

        private void Hide()
        {
            if (finishPanel != null) finishPanel.SetActive(false);
        }

        #endregion

        #region Button Handlers

        private void OnRestartClicked()
        {
            Hide();
            raceManager?. RestartRace();
        }

        private void OnQuitClicked()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication. isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        #endregion

        #region Utility

        private string GetPositionSuffix(int position)
        {
            string suffix = "th";
            if (position == 1) suffix = "st";
            else if (position == 2) suffix = "nd";
            else if (position == 3) suffix = "rd";
            return $"{position}{suffix}";
        }

        #endregion
    }
}