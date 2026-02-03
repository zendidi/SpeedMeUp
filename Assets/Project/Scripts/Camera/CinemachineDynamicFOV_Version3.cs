using UnityEngine;
using Unity.Cinemachine;
using ArcadeRacer.Vehicle;

namespace ArcadeRacer.Camera
{
    /// <summary>
    /// Ajuste le FOV de la Cinemachine Camera en fonction de la vitesse du véhicule. 
    /// Compatible Cinemachine 3.x
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CinemachineDynamicFOV :  MonoBehaviour
    {
        [Header("=== TARGET ===")]
        [SerializeField] private VehicleController vehicle;

        [Header("=== FOV SETTINGS ===")]
        [SerializeField, Tooltip("FOV de base (à basse vitesse)")]
        private float baseFOV = 60f;

        [SerializeField, Tooltip("FOV maximum (à vitesse max)")]
        private float maxSpeedFOV = 75f;

        [SerializeField, Tooltip("Vitesse de transition du FOV")]
        private float fovTransitionSpeed = 3f;

        [Header("=== DISTANCE SETTINGS (optionnel) ===")]
        [SerializeField, Tooltip("Augmenter la distance de caméra à haute vitesse")]
        private bool adjustDistanceWithSpeed = false;

        [SerializeField, Tooltip("Distance supplémentaire max à vitesse max")]
        private float maxSpeedDistanceIncrease = 2f;

        private CinemachineCamera _cinemachineCamera;
        private CinemachineThirdPersonFollow _thirdPersonFollow;
        private float _baseCameraDistance;

        private void Awake()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();

            if (_cinemachineCamera == null)
            {
                Debug.LogError("[CinemachineDynamicFOV] CinemachineCamera component manquant!");
                enabled = false;
                return;
            }

            // Récupérer le composant Third Person Follow
            _thirdPersonFollow = _cinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();

            if (_thirdPersonFollow != null)
            {
                _baseCameraDistance = _thirdPersonFollow.CameraDistance;
            }

            if (vehicle == null)
            {
                Debug.LogWarning("[CinemachineDynamicFOV] VehicleController non assigné!  Tentative de recherche automatique.. .");
                vehicle = FindFirstObjectByType<VehicleController>();
            }
        }

        private void Start()
        {
            // Initialiser le FOV de base
            if (_cinemachineCamera != null)
            {
                _cinemachineCamera. Lens. FieldOfView = baseFOV;
            }
        }

        private void LateUpdate()
        {
            if (vehicle == null || _cinemachineCamera == null) return;

            UpdateFOV();

            if (adjustDistanceWithSpeed && _thirdPersonFollow != null)
            {
                UpdateCameraDistance();
            }
        }

        private void UpdateFOV()
        {
            // Calculer le FOV cible en fonction de la vitesse
            float speedPercentage = vehicle.Physics. SpeedPercentage;
            float targetFOV = Mathf.Lerp(baseFOV, maxSpeedFOV, speedPercentage);

            // Lerp smooth vers le FOV cible
            float currentFOV = _cinemachineCamera. Lens.FieldOfView;
            _cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(currentFOV, targetFOV,
                Time.deltaTime * fovTransitionSpeed);
        }

        private void UpdateCameraDistance()
        {
            // Augmenter la distance de caméra à haute vitesse
            float speedPercentage = vehicle.Physics.SpeedPercentage;
            float targetDistance = _baseCameraDistance + (speedPercentage * maxSpeedDistanceIncrease);

            _thirdPersonFollow.CameraDistance = Mathf.Lerp(
                _thirdPersonFollow. CameraDistance,
                targetDistance,
                Time.deltaTime * fovTransitionSpeed
            );
        }

        /// <summary>
        /// Changer le véhicule ciblé
        /// </summary>
        public void SetVehicle(VehicleController newVehicle)
        {
            vehicle = newVehicle;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Vérifier que les valeurs sont cohérentes
            if (baseFOV < 30f) baseFOV = 30f;
            if (baseFOV > 120f) baseFOV = 120f;
            if (maxSpeedFOV < baseFOV) maxSpeedFOV = baseFOV + 10f;
            if (maxSpeedFOV > 120f) maxSpeedFOV = 120f;
        }
#endif
    }
}