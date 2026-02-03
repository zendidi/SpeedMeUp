using UnityEngine;
using TMPro;
using ArcadeRacer.Vehicle;
using ArcadeRacer. RaceSystem;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Affiche les informations de course pendant le jeu. 
    /// HUD principal avec chrono, vitesse, position, tours. 
    /// </summary>
    public class RaceHUD : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private VehicleController playerVehicle;
        [SerializeField] private RaceManager raceManager;

        [Header("=== UI ELEMENTS ===")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI currentLapTimeText;
        [SerializeField] private TextMeshProUGUI bestLapTimeText;
        [SerializeField] private TextMeshProUGUI lapCountText;
        [SerializeField] private TextMeshProUGUI positionText;

        [Header("=== SETTINGS ===")]
        [SerializeField] private bool showSpeed = true;
        [SerializeField] private bool showCurrentLapTime = true;
        [SerializeField] private bool showBestLapTime = true;
        [SerializeField] private bool showLapCount = true;
        [SerializeField] private bool showPosition = true;

        private LapTimer _playerTimer;

        #region Unity Lifecycle

        private void Awake()
        {
            FindReferences();
        }

        private void Start()
        {
            Debug.Log($"[RaceHUD] Start() - GameObject active: {gameObject.activeSelf}");
            InitializeHUD();
            gameObject.SetActive(false);
            Debug.Log($"[RaceHUD] After Init - GameObject active: {gameObject.activeSelf}");
        }

        private void Update()
        {
            // Récupérer le timer si pas encore fait
            if (_playerTimer == null && raceManager != null && playerVehicle != null)
            {
                _playerTimer = raceManager.GetVehicleTimer(playerVehicle);

                if (_playerTimer != null)
                {
                    Debug.Log("[RaceHUD] Player timer retrieved successfully!");
                }
                else
                {
                    Debug.LogWarning("[RaceHUD] Player timer is still null!");
                }
            }

            // Afficher le HUD seulement quand la course a démarré
            if (raceManager != null)
            {
                if (raceManager.CurrentState == RaceManager.RaceState.Racing)
                {
                    gameObject.SetActive(true);
                    UpdateHUD();
                }
                else if (raceManager.CurrentState == RaceManager.RaceState.Countdown ||
                         raceManager.CurrentState == RaceManager.RaceState.NotStarted)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Initialization

        private void FindReferences()
        {
            // Auto-find player vehicle
            if (playerVehicle == null)
            {
                playerVehicle = FindFirstObjectByType<VehicleController>();
                Debug.Log($"[RaceHUD] Player vehicle found: {playerVehicle?.name}");
            }

            // Auto-find race manager
            if (raceManager == null)
            {
                raceManager = FindFirstObjectByType<RaceManager>();
                Debug.Log($"[RaceHUD] Race manager found: {raceManager?.name}");
            }
        }

        private void InitializeHUD()
        {
            gameObject.SetActive(true);
            // Reset tous les textes
            if (speedText != null) speedText.text = "0 km/h";
            if (currentLapTimeText != null) currentLapTimeText.text = "00:00.000";
            if (bestLapTimeText != null) bestLapTimeText.text = "Best: --:--.---";
            if (lapCountText != null) lapCountText.text = "Lap 1/" + raceManager?. TotalLaps;
            if (positionText != null) positionText.text = "1st";
        }

        #endregion

        #region Update HUD

        private void UpdateHUD()
        {
            if (showSpeed) UpdateSpeed();
            if (showCurrentLapTime) UpdateCurrentLapTime();
            if (showBestLapTime) UpdateBestLapTime();
            if (showLapCount) UpdateLapCount();
            if (showPosition) UpdatePosition();
        }

        private void UpdateSpeed()
        {
            if (speedText == null || playerVehicle == null) return;

            float speed = playerVehicle.Physics.CurrentSpeedKMH;
            speedText.text = $"{Mathf.RoundToInt(speed)} km/h";
        }

        private void UpdateCurrentLapTime()
        {
            if (currentLapTimeText == null)
            {
                Debug.LogWarning("[RaceHUD] currentLapTimeText is null!");
                return;
            }

            if (_playerTimer == null)
            {
                Debug.LogWarning("[RaceHUD] _playerTimer is null!");
                return;
            }

            float currentTime = _playerTimer.CurrentLapTime;
            currentLapTimeText.text = LapTimer.FormatTime(currentTime);
        }

        private void UpdateBestLapTime()
        {
            if (bestLapTimeText == null || _playerTimer == null) return;

            float bestTime = _playerTimer.BestLapTime;
            
            if (bestTime > 0)
            {
                bestLapTimeText. text = "Best: " + LapTimer. FormatTime(bestTime);
            }
            else
            {
                bestLapTimeText.text = "Best: --:--.---";
            }
        }

        private void UpdateLapCount()
        {
            if (lapCountText == null)
            {
                Debug.LogWarning("[RaceHUD] lapCountText is null!");
                return;
            }

            if (raceManager == null)
            {
                Debug.LogWarning("[RaceHUD] raceManager is null!");
                return;
            }

            if (playerVehicle == null)
            {
                Debug.LogWarning("[RaceHUD] playerVehicle is null!");
                return;
            }

            int currentLap = raceManager.GetVehicleLap(playerVehicle) + 1;
            int totalLaps = raceManager.TotalLaps;

            if (currentLap > totalLaps) currentLap = totalLaps;

            lapCountText.text = $"Lap {currentLap}/{totalLaps}";
        }


        private void UpdatePosition()
        {
            if (positionText == null)
            {
                Debug.LogWarning("[RaceHUD] positionText is null!");
                return;
            }

            if (raceManager == null || playerVehicle == null) return;

            int position = raceManager.GetVehiclePosition(playerVehicle);
            positionText.text = GetPositionSuffix(position);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Convertit un nombre en texte avec suffixe (1st, 2nd, 3rd, etc.)
        /// </summary>
        private string GetPositionSuffix(int position)
        {
            string suffix = "th";

            if (position == 1) suffix = "st";
            else if (position == 2) suffix = "nd";
            else if (position == 3) suffix = "rd";

            return $"{position}{suffix}";
        }

        #endregion

        #region Public API

        /// <summary>
        /// Changer le véhicule suivi
        /// </summary>
        public void SetPlayerVehicle(VehicleController vehicle)
        {
            playerVehicle = vehicle;
            _playerTimer = raceManager?.GetVehicleTimer(vehicle);
        }

        /// <summary>
        /// Afficher/cacher le HUD
        /// </summary>
        public void SetVisible(bool visible)
        {
            Debug.Log($"[RaceHUD] SetVisible called with: {visible}");
            gameObject.SetActive(visible);
        }

        #endregion
    }
}