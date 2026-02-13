using UnityEngine;
using ArcadeRacer.Settings;

namespace ArcadeRacer.Vehicle
{
    /// <summary>
    /// Contrôleur principal du véhicule.  
    /// Orchestre les composants VehicleInput et VehiclePhysics. 
    /// Point d'entrée central pour contrôler le véhicule.
    /// </summary>
    [RequireComponent(typeof(VehicleInput))]
    [RequireComponent(typeof(VehiclePhysics))]
    public class VehicleController : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField, Tooltip("Statistiques du véhicule")]
        private VehicleStats stats;

        [Header("=== RESET SETTINGS ===")]
        [SerializeField, Tooltip("Position de spawn/reset")]
        private Transform spawnPoint;

        [SerializeField, Tooltip("Hauteur minimale avant reset automatique")]
        private float fallResetHeight = -50f;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugInfo = true;

        // Components
        public VehicleInput _input;
        private VehiclePhysics _physics;

        // État
        private bool _isControllable = true;

        #region Properties

        /// <summary>
        /// Le véhicule peut être contrôlé
        /// </summary>
        public bool IsControllable
        {
            get => _isControllable;
            set
            {
                _isControllable = value;
                if (! _isControllable)
                {
                    // Désactiver les inputs
                    _input. DisableInput();
                }
                else
                {
                    _input.EnableInput();
                }
            }
        }

        /// <summary>
        /// Accès à la physique du véhicule
        /// </summary>
        public VehiclePhysics Physics => _physics;

        /// <summary>
        /// Accès aux inputs du véhicule
        /// </summary>
        public VehicleInput Input => _input;

        /// <summary>
        /// Statistiques du véhicule
        /// </summary>
        public VehicleStats Stats => stats;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            SetupSpawnPoint();
        }

        private void Start()
        {
            // Spawn initial
            if (spawnPoint != null)
            {
                ResetToSpawnPoint();
            }
            SetControllable(true); // Force l'activation
            Debug.Log($"[VehicleController] IsControllable: {IsControllable}");
        }

        private void Update()
        {
            if (!_isControllable) return;

            // Transférer les inputs à la physique
            UpdatePhysicsInputs();

            // Gérer les actions spéciales
            HandleSpecialActions();

            // Vérifier la chute
            CheckFallReset();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            if (_input == null)
            _input = GetComponent<VehicleInput>();
            _physics = GetComponent<VehiclePhysics>();

            if (_input == null)
            {
                Debug.LogError($"[VehicleController] VehicleInput manquant sur {gameObject.name}!");
            }

            if (_physics == null)
            {
                Debug. LogError($"[VehicleController] VehiclePhysics manquant sur {gameObject. name}!");
            }

            if (stats == null)
            {
                Debug.LogError($"[VehicleController] VehicleStats manquant sur {gameObject.name}!");
            }
        }

        private void SetupSpawnPoint()
        {
            // Si pas de spawn point défini, utiliser la position actuelle
            if (spawnPoint == null)
            {
                GameObject spawnGO = new GameObject($"{gameObject.name}_SpawnPoint");
                spawnPoint = spawnGO.transform;
                spawnPoint.position = transform.position;
                spawnPoint.rotation = transform.rotation;
                spawnPoint.parent = null; // Indépendant du véhicule
                
                Debug.LogWarning($"[VehicleController] Pas de SpawnPoint défini, création automatique à la position actuelle.");
            }
        }

        #endregion

        #region Input to Physics

        private void UpdatePhysicsInputs()
        {
            // Transférer tous les inputs à VehiclePhysics
            _physics.SetInputs(
                throttle: _input. Throttle,
                brake: _input.Brake,
                steering: _input.Steering,
                drift: _input.IsDrifting
            );
    
        }

        #endregion

        #region Special Actions

        private void HandleSpecialActions()
        {
            // Reset manuel
            if (_input.ResetPressed)
            {
                ResetToSpawnPoint();
            }

            // Pause (à gérer plus tard avec un GameManager)
            if (_input.PausePressed)
            {
                OnPausePressed();
            }
        }

        #endregion

        #region Reset System

        /// <summary>
        /// Réinitialiser le véhicule au spawn point
        /// </summary>
        public void ResetToSpawnPoint()
        {
            if (spawnPoint != null)
            {
                _physics.ResetVehicle(spawnPoint.position, spawnPoint.rotation);
                Debug.Log($"[VehicleController] Véhicule réinitialisé au spawn point{spawnPoint.position}.");
            }
        }

        /// <summary>
        /// Définir un nouveau spawn point
        /// </summary>
        public void SetSpawnPoint(Vector3 position, Quaternion rotation)
        {
            if (spawnPoint == null)
            {
                GameObject spawnGO = new GameObject($"{gameObject.name}_SpawnPoint");
                spawnPoint = spawnGO.transform;
            }

            spawnPoint.position = position;
            spawnPoint.rotation = rotation;
        }

        private void CheckFallReset()
        {
            // Reset automatique si le véhicule tombe trop bas
            if (transform.position.y < fallResetHeight)
            {
                Debug.LogWarning($"[VehicleController] Véhicule tombé trop bas, reset automatique!");
                ResetToSpawnPoint();
            }
        }

        #endregion

        #region Pause

        private void OnPausePressed()
        {
            // TODO: Implémenter le système de pause
            // Pour l'instant, juste un log
            Debug.Log("[VehicleController] Pause pressed (not implemented yet)");
        }

        #endregion

        #region Public Control Methods

        /// <summary>
        /// Activer/désactiver le contrôle du véhicule
        /// </summary>
        public void SetControllable(bool controllable)
        {
            IsControllable = controllable;
        }

        /// <summary>
        /// Téléporter le véhicule à une position
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _physics.ResetVehicle(position, rotation);
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [System.Serializable]
        private class DebugInfo
        {
            [Header("Physics")]
            public float speedKMH;
            public float speedPercentage;
            public bool isGrounded;
            public bool isDrifting;
            public float steeringAngle;

            [Header("Input")]
            public float throttle;
            public float brake;
            public float steering;
            public bool driftButton;

            [Header("State")]
            public bool isControllable;
        }

        [SerializeField]
        private DebugInfo _debugInfo = new DebugInfo();

        private void LateUpdate()
        {
            if (! showDebugInfo || !Application.isPlaying) return;

            // Mettre à jour les infos de debug
            _debugInfo.speedKMH = _physics.CurrentSpeedKMH;
            _debugInfo.speedPercentage = _physics.SpeedPercentage;
            _debugInfo.isGrounded = _physics.IsGrounded;
            _debugInfo.isDrifting = _physics.IsDrifting;
            _debugInfo.steeringAngle = _physics.CurrentSteeringAngle;

            _debugInfo.throttle = _input.Throttle;
            _debugInfo.brake = _input.Brake;
            _debugInfo.steering = _input.Steering;
            _debugInfo.driftButton = _input.IsDrifting;

            _debugInfo.isControllable = _isControllable;
        }

        private void OnDrawGizmosSelected()
        {
            // Afficher le spawn point
            if (spawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos. DrawWireSphere(spawnPoint.position, 1f);
                Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 3f);
            }

            // Ligne de fall reset
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Vector3 fallLineStart = transform.position;
                fallLineStart.y = fallResetHeight;
                Gizmos.DrawLine(fallLineStart + Vector3.left * 100f, fallLineStart + Vector3.right * 100f);
                Gizmos.DrawLine(fallLineStart + Vector3.forward * 100f, fallLineStart + Vector3.back * 100f);
            }
        }
#endif

        #endregion
    }
}