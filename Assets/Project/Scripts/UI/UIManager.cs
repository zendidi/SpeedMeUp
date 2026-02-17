using ArcadeRacer.RaceSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Gestionnaire central de l'UI.   
    /// Coordonne le HUD, le countdown, l'écran de fin et la sélection de circuits. 
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("=== UI COMPONENTS ===")]
        [SerializeField] private RaceHUD raceHUD;
        [SerializeField] private CountdownUI countdownUI;
        [SerializeField] private FinishScreenUI finishScreenUI;
        [SerializeField] private CircuitSelectionUI circuitSelectionUI;
        [SerializeField] private HighscoreNameInputUI highscoreNameInputUI;

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
            InitializeInput();
        }

        private void OnEnable()
        {
            SubscribeToRaceEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromRaceEvents();
            UnsubscribeFromInputEvents();
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

            if (circuitSelectionUI == null)
            {
                circuitSelectionUI = FindFirstObjectByType<CircuitSelectionUI>();
            }

            if (highscoreNameInputUI == null)
            {
                highscoreNameInputUI = FindFirstObjectByType<HighscoreNameInputUI>();
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

            if (circuitSelectionUI != null)
            {
                circuitSelectionUI.Hide();
            }

            if (highscoreNameInputUI != null)
            {
                highscoreNameInputUI.Hide();
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
            circuitSelectionUI?.Hide();
            highscoreNameInputUI?.Hide();
        }
        private Car_Actions _carActions;

        private void InitializeInput()
        {
            // Créer l'instance du Input Actions
            _carActions = new Car_Actions();

            // S'abonner aux événements
            SubscribeToInputEvents();
        }

        private void SubscribeToInputEvents()
        {
            // Actions continues (Value)
            Debug.Log("[UIManager] Abonnement à l'input MenuTrigger");
            _carActions.Driving.MenuTrigger.performed += SelectionCircuitTrigger;
            
            // Activer l'action map pour que les inputs fonctionnent
            _carActions.Driving.Enable();
        }
        
        private void UnsubscribeFromInputEvents()
        {
            if (_carActions == null) return;
            
            _carActions.Driving.MenuTrigger.performed -= SelectionCircuitTrigger;
            _carActions.Driving.Disable();
        }
        public void SelectionCircuitTrigger(InputAction.CallbackContext context)
        {
            Debug.Log("[UIManager] Menu Trigger activé");
            if (circuitSelectionUI.isActiveAndEnabled)
            {
                circuitSelectionUI.Hide();
            }
            else
            {
                circuitSelectionUI.Show();
            }
        }

        /// <summary>
        /// Afficher l'UI de sélection de circuits
        /// </summary>
        public void ShowCircuitSelection()
        {
            if (circuitSelectionUI != null)
            {
                circuitSelectionUI.Show();
                HideRaceHUD();
                Debug.Log("[UIManager] Circuit Selection UI activé");
            }
        }

        /// <summary>
        /// Cacher l'UI de sélection de circuits
        /// </summary>
        public void HideCircuitSelection()
        {
            circuitSelectionUI?.Hide();
        }

        /// <summary>
        /// Afficher le modal de saisie du nom pour highscore
        /// </summary>
        public void ShowHighscoreNameInput(float lapTime, string circuitName)
        {
            if (highscoreNameInputUI != null)
            {
                highscoreNameInputUI.Show(lapTime, circuitName);
                Debug.Log("[UIManager] Highscore Name Input UI activé");
            }
        }

        /// <summary>
        /// Cacher le modal de saisie du nom pour highscore
        /// </summary>
        public void HideHighscoreNameInput()
        {
            highscoreNameInputUI?.Hide();
        }

        #endregion
        
        private void OnDestroy()
        {
            if (_carActions != null)
            {
                _carActions.Dispose();
            }
        }
    }
}