using UnityEngine;

namespace ArcadeRacer. VFX
{
    /// <summary>
    /// Anime la rotation des roues selon la vitesse du véhicule.  
    /// Fait tourner les meshes des roues de manière réaliste.
    /// </summary>
    public class WheelAnimator : MonoBehaviour
    {
        [Header("=== WHEEL TRANSFORMS ===")]
        [SerializeField] private Transform frontLeftWheel;
        [SerializeField] private Transform frontRightWheel;
        [SerializeField] private Transform rearLeftWheel;
        [SerializeField] private Transform rearRightWheel;

        [Header("=== SETTINGS ===")]
        [SerializeField] private float wheelRadius = 0.3f;
        [SerializeField] private float maxSteerAngle = 30f;

        [Header("=== STEERING ===")]
        [SerializeField] private bool enableSteering = true;
        [SerializeField] private float steeringSpeed = 5f;

        private float _currentRotation = 0f;
        private float _currentSteerAngle = 0f;

        #region Unity Lifecycle

        private void Awake()
        {
            FindWheelTransforms();
        }

        #endregion

        #region Setup

        private void FindWheelTransforms()
        {
            // Auto-find wheels if not assigned
            // Tu peux adapter selon ta hiérarchie de GameObject
            if (frontLeftWheel == null)
            {
                Transform found = transform.Find("Wheels/FrontLeft");
                if (found != null) frontLeftWheel = found;
            }

            if (frontRightWheel == null)
            {
                Transform found = transform.Find("Wheels/FrontRight");
                if (found != null) frontRightWheel = found;
            }

            if (rearLeftWheel == null)
            {
                Transform found = transform.Find("Wheels/RearLeft");
                if (found != null) rearLeftWheel = found;
            }

            if (rearRightWheel == null)
            {
                Transform found = transform.Find("Wheels/RearRight");
                if (found != null) rearRightWheel = found;
            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// Mettre à jour l'animation selon la vitesse (en km/h)
        /// </summary>
        public void SetSpeed(float speedKMH, float steerInput = 0f)
        {
            // Convertir km/h en m/s
            float speedMS = speedKMH / 3.6f;

            // Calculer la rotation des roues
            // Rotation (degrés/sec) = (vitesse / circonférence) * 360
            float circumference = 2f * Mathf.PI * wheelRadius;
            float rotationSpeed = (speedMS / circumference) * 360f;

            // Accumuler la rotation
            _currentRotation += rotationSpeed * Time. deltaTime;

            // Appliquer la rotation
            RotateWheels(_currentRotation);

            // Steering
            if (enableSteering)
            {
                float targetSteerAngle = steerInput * maxSteerAngle;
                _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetSteerAngle, Time. deltaTime * steeringSpeed);
                SteerFrontWheels(_currentSteerAngle);
            }
        }

        private void RotateWheels(float rotation)
        {
            // Rotation autour de l'axe X (forward)
            Quaternion wheelRotation = Quaternion. Euler(rotation, 0f, 0f);

            if (frontLeftWheel != null)
            {
                frontLeftWheel.localRotation = wheelRotation * Quaternion.Euler(0f, _currentSteerAngle, 0f);
            }

            if (frontRightWheel != null)
            {
                frontRightWheel.localRotation = wheelRotation * Quaternion. Euler(0f, _currentSteerAngle, 0f);
            }

            if (rearLeftWheel != null)
            {
                rearLeftWheel.localRotation = wheelRotation;
            }

            if (rearRightWheel != null)
            {
                rearRightWheel.localRotation = wheelRotation;
            }
        }

        private void SteerFrontWheels(float steerAngle)
        {
            // Le steering est déjà appliqué dans RotateWheels
            // Cette fonction existe pour future customisation
        }

        #endregion

        #region Public API

        /// <summary>
        /// Assigner manuellement les roues
        /// </summary>
        public void SetWheelTransforms(Transform fl, Transform fr, Transform rl, Transform rr)
        {
            frontLeftWheel = fl;
            frontRightWheel = fr;
            rearLeftWheel = rl;
            rearRightWheel = rr;
        }

        /// <summary>
        /// Obtenir les positions des roues arrière (pour particules/skidmarks)
        /// </summary>
        public void GetRearWheelPositions(out Transform left, out Transform right)
        {
            left = rearLeftWheel;
            right = rearRightWheel;
        }

        #endregion
    }
}