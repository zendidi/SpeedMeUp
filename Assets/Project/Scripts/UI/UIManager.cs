using UnityEngine;
using ArcadeRacer.RaceSystem;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Gestionnaire central de l'UI.   
    /// Coordonne le HUD, le countdown, et l'écran de fin. 
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("=== UI COMPONENTS ===")]
        [SerializeField] private RaceHUD raceHUD;
        [SerializeField] private CountdownUI countdownUI;
        [SerializeField] private FinishScreenUI finishScreenUI;

        [Header("=== REFERENCES ===")]
        [SerializeField] private RaceManager raceManager;

        #region Unity Lifecycle

        private void Awake()
        {
            FindReferences();
        }

        private void Start()
        {
            InitializeUI();
        }

        private void OnEnable()
        {
            SubscribeToRaceEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromRaceEvents();
        }

        #endregion

        #region Initialization

        private void FindReferences()
        {
            if (raceManager == null)
            {
                raceManager = FindFirstObjectByType<RaceManager>();
            }

            // Auto-find UI components si non assignés
            if (raceHUD == null)
            {
                raceHUD = FindFirstObjectByType<RaceHUD>();
            }

            if (countdownUI == null)
            {
                countdownUI = FindFirstObjectByType<CountdownUI>();
            }

            if (finishScreenUI == null)
            {
                finishScreenUI = FindFirstObjectByType<FinishScreenUI>();
            }
        }

        private void InitializeUI()
        {
            // Cacher tous les éléments UI au départ
            if (raceHUD != null)
            {
                raceHUD.SetVisible(false);
            }

            if (countdownUI != null)
            {
                countdownUI.gameObject.SetActive(false);
            }

            if (finishScreenUI != null)
            {
                finishScreenUI.gameObject.SetActive(false);
            }
        }

        private void SubscribeToRaceEvents()
        {
            if (raceManager != null)
            {
                raceManager.OnCountdownStarted += HandleCountdownStarted;
                raceManager.OnRaceStarted += HandleRaceStarted;
                raceManager.OnRaceFinished += HandleRaceFinished;
            }
        }

        private void UnsubscribeFromRaceEvents()
        {
            if (raceManager != null)
            {
                raceManager.OnCountdownStarted -= HandleCountdownStarted;
                raceManager.OnRaceStarted -= HandleRaceStarted;
                raceManager.OnRaceFinished -= HandleRaceFinished;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleCountdownStarted()
        {
            // Afficher le countdown UI
            if (countdownUI != null)
            {
                countdownUI.gameObject.SetActive(true);
            }

            // Cacher les autres UI
            if (raceHUD != null)
            {
                raceHUD.SetVisible(false);
            }

            if (finishScreenUI != null)
            {
                finishScreenUI.gameObject.SetActive(false);
            }

            Debug.Log("[UIManager] Countdown UI activé");
        }

        private void HandleRaceStarted()
        {
            // Afficher le HUD de course
            if (raceHUD != null)
            {
                raceHUD.SetVisible(true);
            }

            Debug.Log("[UIManager] Race HUD activé");
        }

        private void HandleRaceFinished()
        {
            // Cacher le HUD
            if (raceHUD != null)
            {
                raceHUD.SetVisible(false);
            }

            // Afficher l'écran de fin
            if (finishScreenUI != null)
            {
                finishScreenUI.gameObject.SetActive(true);
            }

            Debug.Log("[UIManager] Finish Screen activé");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Afficher le HUD de course
        /// </summary>
        public void ShowRaceHUD()
        {
            raceHUD?.SetVisible(true);
        }

        /// <summary>
        /// Cacher le HUD de course
        /// </summary>
        public void HideRaceHUD()
        {
            raceHUD?.SetVisible(false);
        }

        /// <summary>
        /// Reset toute l'UI
        /// </summary>
        public void ResetUI()
        {
            countdownUI?.Reset();
            raceHUD?.SetVisible(false);
            finishScreenUI?.gameObject.SetActive(false);
        }

        #endregion
    }
}