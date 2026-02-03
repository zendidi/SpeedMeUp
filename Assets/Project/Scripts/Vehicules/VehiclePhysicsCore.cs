using UnityEngine;

namespace ArcadeRacer.Physics
{
    /// <summary>
    /// Gère l'état physique du véhicule et les calculs de physique avancée
    /// </summary>
    [System.Serializable]
    public class VehiclePhysicsCore
    {
        #region Configuration

        [Header("=== INERTIE LINÉAIRE ===")]
        [Tooltip("Coefficient de résistance au roulement (0.01-0.02 pour asphalte)")]
        public float rollingResistanceCoefficient = 0.015f;

        [Tooltip("Coefficient de traînée aérodynamique (0.25-0.35 pour voiture sport)")]
        public float dragCoefficient = 0.30f;

        [Tooltip("Surface frontale en m² (1.5-2.5 pour voiture arcade)")]
        public float frontalArea = 2.0f;

        [Header("=== INERTIE ANGULAIRE ===")]
        [Tooltip("Coefficient d'amortissement de la rotation (5-15)")]
        public float angularDampingCoefficient = 8.0f;

        [Tooltip("Force de retour au centre du volant (0-20, 0=pas de retour)")]
        public float steeringReturnStrength = 12.0f;

        [Tooltip("Longueur du véhicule en mètres (pour calcul du moment d'inertie)")]
        public float vehicleLength = 4.5f;

        [Header("=== TRANSFERT DE CHARGE ===")]
        [Tooltip("Hauteur du centre de gravité en mètres")]
        public float centerOfGravityHeight = 0.5f;

        [Tooltip("Empattement (distance entre essieux) en mètres")]
        public float wheelbase = 2.5f;

        [Tooltip("Influence du transfert de charge sur le grip (0-1)")]
        [Range(0f, 1f)]
        public float weightTransferInfluence = 0.3f;

        #endregion

        #region État

        // Inertie angulaire
        private float _angularVelocity; // rad/s
        private float _momentOfInertia;

        // Transfert de charge
        private float _frontAxleLoad; // ratio 0-1
        private float _rearAxleLoad;  // ratio 0-1

        #endregion

        #region Properties

        public float AngularVelocity => _angularVelocity;
        public float FrontAxleLoad => _frontAxleLoad;
        public float RearAxleLoad => _rearAxleLoad;

        #endregion

        #region Initialization

        public void Initialize(float mass)
        {
            _momentOfInertia = PhysicsFormulas.MomentOfInertia(mass, vehicleLength);
            _frontAxleLoad = 0.5f;
            _rearAxleLoad = 0.5f;
            _angularVelocity = 0f;
        }

        #endregion

        #region Inertie Linéaire

        /// <summary>
        /// Calcule la décélération naturelle (coast down)
        /// </summary>
        public float CalculateCoastDownDeceleration(float currentSpeed, float mass)
        {
            return PhysicsFormulas.CoastDownDeceleration(
                currentSpeed,
                mass,
                rollingResistanceCoefficient,
                dragCoefficient,
                frontalArea
            );
        }

        /// <summary>
        /// Applique le coast down à la vélocité
        /// </summary>
        public Vector3 ApplyCoastDown(Vector3 velocity, Transform transform, float mass, float deltaTime)
        {
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            if (forwardSpeed < 0.1f) return velocity;

            float deceleration = CalculateCoastDownDeceleration(forwardSpeed, mass);
            float newForwardSpeed = Mathf.Max(0f, forwardSpeed - deceleration * deltaTime);

            // Reconstruire la vélocité
            Vector3 lateralVelocity = Vector3.Project(velocity, transform.right);
            return transform.forward * newForwardSpeed + lateralVelocity;
        }

        #endregion

        #region Inertie Angulaire

        /// <summary>
        /// Met à jour la vitesse angulaire en fonction de l'input de steering
        /// </summary>
        public void UpdateAngularVelocity(float steeringInput, float steeringSpeed, float deltaTime)
        {
            // === TORQUE APPLIQUÉ PAR LE STEERING ===
            float inputTorque = steeringInput * steeringSpeed * 10f;

            // === FORCE DE RETOUR AU CENTRE ===
            float centeringTorque = 0f;
            if (Mathf.Abs(steeringInput) < 0.05f) // Dead zone
            {
                centeringTorque = -_angularVelocity * steeringReturnStrength;
            }

            // === TORQUE TOTAL ===
            float totalTorque = inputTorque + centeringTorque;

            // === α = τ / I ===
            float angularAcceleration = totalTorque / _momentOfInertia;

            // === AMORTISSEMENT ===
            float dampingAcceleration = PhysicsFormulas.AngularDeceleration(_angularVelocity, angularDampingCoefficient);

            // === METTRE À JOUR ω ===
            _angularVelocity += (angularAcceleration + dampingAcceleration) * deltaTime;

            // === LIMITER ===
            _angularVelocity = Mathf.Clamp(_angularVelocity, -5f, 5f);

            // === ARRÊT COMPLET SI TRÈS FAIBLE ===
            if (Mathf.Abs(steeringInput) < 0.01f && Mathf.Abs(_angularVelocity) < 0.02f)
            {
                _angularVelocity = 0f;
            }
        }

        /// <summary>
        /// Applique la rotation inertielle au transform
        /// </summary>
        public void ApplyAngularInertia(Transform transform, float deltaTime)
        {
            if (Mathf.Abs(_angularVelocity) < 0.01f)
            {
                _angularVelocity = 0f;
                return;
            }

            float rotationDegrees = _angularVelocity * Mathf.Rad2Deg * deltaTime;
            transform.Rotate(0f, rotationDegrees, 0f, Space.Self);
        }

        /// <summary>
        /// Réinitialise la vélocité angulaire (pour les resets)
        /// </summary>
        public void ResetAngularVelocity()
        {
            _angularVelocity = 0f;
        }

        #endregion

        #region Transfert de Charge

        /// <summary>
        /// Calcule la distribution de charge entre essieux
        /// </summary>
        public void UpdateWeightTransfer(float longitudinalAcceleration, float mass)
        {
            // Transfert longitudinal (freinage/accélération)
            float longitudinalTransfer = PhysicsFormulas.LongitudinalWeightTransfer(
                mass,
                longitudinalAcceleration,
                centerOfGravityHeight,
                wheelbase
            );

            // Distribution de base 50/50
            float baseFront = 0.5f;
            float baseRear = 0.5f;

            // Appliquer le transfert
            float transferRatio = (longitudinalTransfer / (mass * PhysicsFormulas.GRAVITY)) * weightTransferInfluence;

            _frontAxleLoad = Mathf.Clamp01(baseFront - transferRatio);
            _rearAxleLoad = Mathf.Clamp01(baseRear + transferRatio);

            // Normaliser
            float total = _frontAxleLoad + _rearAxleLoad;
            _frontAxleLoad /= total;
            _rearAxleLoad /= total;
        }

        /// <summary>
        /// Retourne un multiplicateur de grip basé sur la charge
        /// </summary>
        public float GetGripMultiplier()
        {
            return Mathf.Lerp(0.7f, 1.3f, _rearAxleLoad);
        }

        #endregion
    }
}