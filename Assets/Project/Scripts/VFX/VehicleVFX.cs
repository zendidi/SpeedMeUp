using UnityEngine;
using ArcadeRacer.Vehicle;

namespace ArcadeRacer.VFX
{
    /// <summary>
    /// Gestionnaire central des effets visuels du véhicule. 
    /// Coordonne les particules, animations et effets selon l'état du véhicule. 
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public class VehicleVFX :  MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private VehicleController vehicle;

        [Header("=== VFX COMPONENTS ===")]
        [SerializeField] private DriftParticles driftParticles;
        [SerializeField] private SkidMarks skidMarks;
        [SerializeField] private SpeedLines speedLines;
        [SerializeField] private WheelAnimator wheelAnimator;
        [SerializeField] private CollisionVFX collisionVFX;

        [Header("=== SETTINGS ===")]
        [SerializeField] private bool enableDriftVFX = true;
        [SerializeField] private bool enableSkidMarks = true;
        [SerializeField] private bool enableSpeedLines = true;
        [SerializeField] private bool enableWheelAnimation = true;
        [SerializeField] private bool enableCollisionVFX = true;

        #region Unity Lifecycle

        private void Awake()
        {
            if (vehicle == null)
            {
                vehicle = GetComponent<VehicleController>();
            }

            FindVFXComponents();
            LinkWheelsToEffects();
        }

        private void Update()
        {
            UpdateVFX();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (enableCollisionVFX && collisionVFX != null)
            {
                collisionVFX.PlayCollisionEffect(collision);
            }
        }

        #endregion

        #region Initialization

        private void FindVFXComponents()
        {
            // Auto-find components if not assigned
            if (driftParticles == null)
            {
                driftParticles = GetComponentInChildren<DriftParticles>();
            }

            if (skidMarks == null)
            {
                skidMarks = GetComponentInChildren<SkidMarks>();
            }

            if (speedLines == null)
            {
                speedLines = GetComponentInChildren<SpeedLines>();
            }

            if (wheelAnimator == null)
            {
                wheelAnimator = GetComponentInChildren<WheelAnimator>();
            }

            if (collisionVFX == null)
            {
                collisionVFX = GetComponentInChildren<CollisionVFX>();
            }
        }

        #endregion

        #region VFX Update
        private void LinkWheelsToEffects()
        {
            if (wheelAnimator == null) return;

            // Obtenir les roues arrière
            wheelAnimator.GetRearWheelPositions(out Transform leftWheel, out Transform rightWheel);

            // Lier aux particules de drift
            if (driftParticles != null && leftWheel != null && rightWheel != null)
            {
                driftParticles.SetWheelPositions(leftWheel, rightWheel);
            }

            // Lier aux skidmarks
            if (skidMarks != null && leftWheel != null && rightWheel != null)
            {
                skidMarks.SetWheelPositions(leftWheel, rightWheel);
            }

            Debug.Log("[VehicleVFX] Wheels linked to effects!");
        }

        private void UpdateVFX()
        {
            if (vehicle == null) return;

            // Déterminer l'état du véhicule
            bool isDrifting = IsDrifting();
            float speedPercentage = vehicle.Physics.SpeedPercentage;

            // Drift particles
            if (enableDriftVFX && driftParticles != null)
            {
                driftParticles.SetDrifting(isDrifting, speedPercentage);
            }

            // Skid marks
            if (enableSkidMarks && skidMarks != null)
            {
                skidMarks.SetDrifting(isDrifting);
            }

            // Speed lines
            if (enableSpeedLines && speedLines != null)
            {
                speedLines.SetSpeed(speedPercentage);
            }

            // Wheel animation
            if (enableWheelAnimation && wheelAnimator != null)
            {
                wheelAnimator. SetSpeed(vehicle.Physics.CurrentSpeedKMH);
            }
        }

        private bool IsDrifting()
        {
            if (vehicle == null) return false;

            // Détection simple de drift
            float speed = vehicle.Physics.CurrentSpeedKMH;
            bool hasSpeed = speed > 20f;
            bool isSteering = Mathf.Abs(vehicle._input.Steering) > 0.3f;

            return hasSpeed && isSteering;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Activer/désactiver tous les VFX
        /// </summary>
        public void SetVFXEnabled(bool enabled)
        {
            enableDriftVFX = enabled;
            enableSkidMarks = enabled;
            enableSpeedLines = enabled;
            enableWheelAnimation = enabled;
            enableCollisionVFX = enabled;
        }

        /// <summary>
        /// Jouer un effet de boost (pour plus tard)
        /// </summary>
        public void PlayBoostEffect()
        {
            // TODO: Implémenter effet de boost
            Debug.Log("[VehicleVFX] Boost effect!");
        }

        #endregion
    }
}