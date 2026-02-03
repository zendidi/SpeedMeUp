using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeRacer.Vehicle
{
    /// <summary>
    /// Gère les entrées du joueur pour le véhicule.  
    /// Lit les inputs depuis le Input System et expose des propriétés propres.  
    /// </summary>
    public class VehicleInput : MonoBehaviour  // ← ENLEVÉ RequireComponent
    {
        [Header("=== INPUT STATE ===")]
        [SerializeField, Tooltip("Afficher les valeurs d'input dans l'Inspector (debug)")]
        private bool showDebugValues = true;

        // Références
        private Car_Actions _carActions;

        // État des inputs (exposé en lecture seule)
        private float _throttle;
        private float _brake;
        private float _steering;
        private bool _isDrifting;
        private bool _resetPressed;
        private bool _pausePressed;

        #region Properties (READ-ONLY)

        /// <summary>
        /// Valeur d'accélération (0 à 1)
        /// </summary>
        public float Throttle => _throttle;

        /// <summary>
        /// Valeur de freinage (0 à 1)
        /// </summary>
        public float Brake => _brake;

        /// <summary>
        /// Direction (-1 = gauche, 0 = centre, 1 = droite)
        /// </summary>
        public float Steering => _steering;

        /// <summary>
        /// Le joueur maintient le bouton de drift
        /// </summary>
        public bool IsDrifting => _isDrifting;

        /// <summary>
        /// Le bouton reset a été pressé ce frame
        /// </summary>
        public bool ResetPressed => _resetPressed;

        /// <summary>
        /// Le bouton pause a été pressé ce frame
        /// </summary>
        public bool PausePressed => _pausePressed;

        /// <summary>
        /// Le joueur accélère (throttle > 0)
        /// </summary>
        public bool IsAccelerating => _throttle > 0.1f;

        /// <summary>
        /// Le joueur freine (brake > 0)
        /// </summary>
        public bool IsBraking => _brake > 0.1f;

        /// <summary>
        /// Le joueur tourne (steering != 0)
        /// </summary>
        public bool IsSteering => Mathf.Abs(_steering) > 0.1f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeInput();
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        private void Update()
        {
            // Reset les événements "one-shot" chaque frame
            _resetPressed = false;
            _pausePressed = false;
        }

        #endregion

        #region Initialization

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
            _carActions.Driving.Accelerate.performed += OnAccelerate;
            _carActions.Driving.Accelerate.canceled += OnAccelerate;

            _carActions.Driving.Brake.performed += OnBrake;
            _carActions.Driving.Brake.canceled += OnBrake;

            _carActions.Driving.Steering.performed += OnSteering;
            _carActions.Driving.Steering.canceled += OnSteering;

            // Actions boutons (Button)
            _carActions.Driving.Drift.performed += OnDriftStarted;
            _carActions.Driving.Drift.canceled += OnDriftCanceled;

            //_carActions.Driving.Reset.performed += OnReset;
            //_carActions.Driving.Pause.performed += OnPause;
        }

        private void UnsubscribeFromInputEvents()
        {
            if (_carActions == null) return;

            _carActions.Driving.Accelerate.performed -= OnAccelerate;
            _carActions.Driving.Accelerate.canceled -= OnAccelerate;

            _carActions.Driving.Brake.performed -= OnBrake;
            _carActions.Driving.Brake.canceled -= OnBrake;

            _carActions.Driving.Steering.performed -= OnSteering;
            _carActions.Driving.Steering.canceled -= OnSteering;

            _carActions.Driving.Drift.performed -= OnDriftStarted;
            _carActions.Driving.Drift.canceled -= OnDriftCanceled;

            //_carActions.Driving.Reset.performed -= OnReset;
            //_carActions.Driving.Pause.performed -= OnPause;
        }

        #endregion

        #region Input Callbacks

        private void OnAccelerate(InputAction.CallbackContext context)
        {
            _throttle = context.ReadValue<float>();
        }

        private void OnBrake(InputAction.CallbackContext context)
        {
            _brake = context.ReadValue<float>();
        }

        private void OnSteering(InputAction.CallbackContext context)
        {
            _steering = context.ReadValue<float>();
        }

        private void OnDriftStarted(InputAction.CallbackContext context)
        {
            _isDrifting = true;
        }

        private void OnDriftCanceled(InputAction.CallbackContext context)
        {
            _isDrifting = false;
        }

        private void OnReset(InputAction.CallbackContext context)
        {
            _resetPressed = true;
        }

        private void OnPause(InputAction.CallbackContext context)
        {
            _pausePressed = true;
        }

        #endregion

        #region Enable/Disable

        /// <summary>
        /// Active les inputs
        /// </summary>
        public void EnableInput()
        {
            _carActions?.Driving.Enable();
        }

        /// <summary>
        /// Désactive les inputs (utile pour les menus, pause, etc.)
        /// </summary>
        public void DisableInput()
        {
            _carActions?.Driving.Disable();

            // Reset toutes les valeurs
            _throttle = 0f;
            _brake = 0f;
            _steering = 0f;
            _isDrifting = false;
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            UnsubscribeFromInputEvents();
            _carActions?.Dispose();
        }

        #endregion

        #region Debug (Inspector)

#if UNITY_EDITOR
        // Afficher les valeurs dans l'Inspector pour debug
        [System.Serializable]
        private class DebugInputValues
        {
            [Range(0f, 1f)] public float throttle;
            [Range(0f, 1f)] public float brake;
            [Range(-1f, 1f)] public float steering;
            public bool isDrifting;
            public bool isAccelerating;
            public bool isBraking;
            public bool isSteering;
        }

        [SerializeField]
        private DebugInputValues _debugValues = new DebugInputValues();

        private void LateUpdate()
        {
            if (!showDebugValues || !Application.isPlaying) return;

            // Mettre à jour les valeurs debug visibles dans l'Inspector
            _debugValues.throttle = _throttle;
            _debugValues.brake = _brake;
            _debugValues.steering = _steering;
            _debugValues.isDrifting = _isDrifting;
            _debugValues.isAccelerating = IsAccelerating;
            _debugValues.isBraking = IsBraking;
            _debugValues.isSteering = IsSteering;
        }
#endif

        #endregion
    }
}