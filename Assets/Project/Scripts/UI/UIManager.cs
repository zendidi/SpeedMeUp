using ArcadeRacer.RaceSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        [SerializeField] private FinishScreenUI Info;
        [SerializeField] private CircuitSelectionUI circuitSelectionUI;
        [SerializeField] private HighscoreNameInputUI highscoreNameInputUI;

        [Header("=== RACE SETTINGS UI ===")]
        [SerializeField] private Toggle warmUpToggle;
        [SerializeField] private Slider lapCountSlider;
        [SerializeField] private TextMeshProUGUI lapCountText;

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
            InitializeRaceSettingsUI();

            if (Info != null)
            {
                Info.gameObject.SetActive(true);
                Debug.Log("[UIManager] Affichage de l'écran d'info au lancement");
            }
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

            if (raceHUD == null)
            {
                raceHUD = FindFirstObjectByType<RaceHUD>();
            }

            if (countdownUI == null)
            {
                countdownUI = FindFirstObjectByType<CountdownUI>();
            }

            if (Info == null)
            {
                Info = FindFirstObjectByType<FinishScreenUI>();
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
            if (raceHUD != null)
            {
                raceHUD.SetVisible(false);
            }

            if (countdownUI != null)
            {
                countdownUI.gameObject.SetActive(false);
            }

            if (circuitSelectionUI != null)
            {
                circuitSelectionUI.Hide();
            }

            if (highscoreNameInputUI != null)
            {
                highscoreNameInputUI.Hide();
            }

            if (Info != null)
            {
                Info.gameObject.SetActive(true);
                Debug.Log("[UIManager] Affichage de l'écran d'info au lancement");
            }
        }

        /// <summary>
        /// Initialise les contrôles de réglage de course (Toggle warmup + Slider lap count)
        /// et synchronise leur état initial avec le RaceManager.
        /// </summary>
        private void InitializeRaceSettingsUI()
        {
            if (raceManager == null) return;

            // === TOGGLE WARMUP ===
            if (warmUpToggle != null)
            {
                // Synchroniser l'état du toggle avec la valeur actuelle du RaceManager
                warmUpToggle.isOn = raceManager.EnableWarmupLap;

                // S'abonner au changement de valeur
                warmUpToggle.onValueChanged.AddListener(OnWarmupToggleChanged);
            }

            // === SLIDER LAP COUNT ===
            if (lapCountSlider != null)
            {
                lapCountSlider.wholeNumbers = true;

                // Synchroniser le slider avec la valeur actuelle du RaceManager
                lapCountSlider.value = raceManager.TotalLaps;

                // S'abonner au changement de valeur
                lapCountSlider.onValueChanged.AddListener(OnLapCountSliderChanged);
            }

            // Mettre à jour le texte immédiatement
            UpdateLapCountText(raceManager.TotalLaps);
        }

        #endregion

        #region Race Settings UI Handlers

        /// <summary>
        /// Appelé quand le Toggle warmup change d'état.
        /// Propage la valeur au RaceManager immédiatement.
        /// </summary>
        private void OnWarmupToggleChanged(bool isOn)
        {
            if (raceManager != null)
            {
                raceManager.EnableWarmupLap = isOn;
                Debug.Log($"[UIManager] Warmup Lap : {(isOn ? "activé" : "désactivé")}");
            }
        }

        /// <summary>
        /// Appelé quand le Slider de nombre de tours change de valeur.
        /// Propage la valeur au RaceManager et met à jour le texte.
        /// </summary>
        private void OnLapCountSliderChanged(float value)
        {
            int lapCount = Mathf.RoundToInt(value);

            if (raceManager != null)
            {
                raceManager.TotalLaps = lapCount;
                Debug.Log($"[UIManager] Nombre de tours : {lapCount}");
            }

            UpdateLapCountText(lapCount);
        }

        private void UpdateLapCountText(int lapCount)
        {
            if (lapCountText != null)
            {
                lapCountText.text = $"Lap Count: {lapCount}";
            }
        }

        #endregion

        #region Screen Navigation



        #endregion

        #region Race Event Subscriptions

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
            if (countdownUI != null)
            {
                countdownUI.gameObject.SetActive(true);
            }

            if (raceHUD != null)
            {
                raceHUD.SetVisible(false);
            }

            if (Info != null)
            {
                Info.gameObject.SetActive(false);
            }

            Debug.Log("[UIManager] Countdown UI activé");
        }

        private void HandleRaceStarted()
        {
            if (raceHUD != null)
            {
                raceHUD.SetVisible(true);
            }

            Debug.Log("[UIManager] Race HUD activé");
        }

        private void HandleRaceFinished()
        {
            if (raceHUD != null)
            {
                raceHUD.SetVisible(false);
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
            Info?.Hide();
            circuitSelectionUI?.Hide();
            highscoreNameInputUI?.Hide();
        }

        public void ShowInfo()
        {
            ResetUI();
            if (Info != null)
            {
                Info.Show();
                Debug.Log("[UIManager] Affichage de l'écran d'info");
            }
        }
        
        /// <summary>
        /// Afficher l'UI de sélection de circuits
        /// </summary>
        public void ShowCircuitSelection()
        {
            if (circuitSelectionUI != null)
            {
                ResetUI();
                circuitSelectionUI.Show();
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

        #region Input

        private Car_Actions _carActions;

        private void InitializeInput()
        {
            _carActions = new Car_Actions();
            SubscribeToInputEvents();
        }

        private void SubscribeToInputEvents()
        {
            Debug.Log("[UIManager] Abonnement à l'input MenuTrigger");
            _carActions.Driving.MenuTrigger.performed += SelectionCircuitTrigger;
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

        #endregion

        private void OnDestroy()
        {
            // Désabonner les listeners UI pour éviter les fuites mémoire
            warmUpToggle?.onValueChanged.RemoveListener(OnWarmupToggleChanged);
            lapCountSlider?.onValueChanged.RemoveListener(OnLapCountSliderChanged);

            if (_carActions != null)
            {
                _carActions.Dispose();
            }
        }
    }
}