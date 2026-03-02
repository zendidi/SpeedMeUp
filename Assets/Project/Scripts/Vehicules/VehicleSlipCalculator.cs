using UnityEngine;

namespace ArcadeRacer.Physics
{
    /// <summary>
    /// Calcule les angles de glissement (slip angles) aux essieux avant et arrière
    /// pour simuler le survirage et le sous-virage.
    ///
    /// Survirage  (oversteer)  : l'essieu arrière glisse plus que l'avant
    ///                           → la voiture tourne plus que prévu (queue qui part).
    /// Sous-virage (understeer): l'essieu avant glisse plus que l'arrière
    ///                           → la voiture tourne moins que prévu (pousse tout droit).
    ///
    /// Toutes les corrections sont exprimées en Vector3 (espace monde) ou en delta
    /// de vitesse angulaire (rad/s), sans aucun MonoBehaviour.
    /// </summary>
    [System.Serializable]
    public class VehicleSlipCalculator
    {
        [Header("=== ACTIVATION ===")]
        [Tooltip("Active la simulation de survirage/sous-virage")]
        public bool enabled = true;

        [Header("=== GÉOMÉTRIE ===")]
        [Tooltip("Empattement en mètres (synchronisé avec VehiclePhysicsCore.wheelbase)")]
        public float wheelbase = 2.5f;

        [Tooltip("Angle de braquage maximum en degrés (pour normaliser l'input de direction)")]
        [Range(10f, 45f)]
        public float maxSteeringAngleDeg = 25f;

        [Header("=== SEUILS ===")]
        [Tooltip("Angle de glissement minimum (rad) avant que le survirage s'applique")]
        [Range(0.01f, 0.5f)]
        public float oversteerThreshold = 0.08f;

        [Tooltip("Angle de glissement minimum (rad) avant que le sous-virage s'applique")]
        [Range(0.01f, 0.5f)]
        public float understeerThreshold = 0.10f;

        [Header("=== INTENSITÉS ===")]
        [Tooltip("Amplification du survirage (0 = désactivé, 1 = valeur par défaut)")]
        [Range(0f, 3f)]
        public float oversteerStrength = 1.0f;

        [Tooltip("Intensité de la résistance au virage en sous-virage (0 = désactivé, 1 = valeur par défaut)")]
        [Range(0f, 3f)]
        public float understeerStrength = 0.8f;

        [Header("=== COEFFICIENTS ANGULAIRES ===")]
        [Tooltip("Facteur de conversion glissement arrière → delta de rotation (survirage)")]
        [Range(0f, 10f)]
        public float oversteerYawFactor = 3f;

        [Tooltip("Facteur d'amortissement de la rotation en sous-virage")]
        [Range(0f, 10f)]
        public float understeerYawDampFactor = 2f;

        // Epsilon pour éviter la division par zéro dans les intensités normalisées
        private const float THRESHOLD_EPSILON = 0.001f;

        // === État calculé (lecture seule) ===
        private float _frontSlipAngle;
        private float _rearSlipAngle;
        private float _oversteerIntensity;
        private float _understeerIntensity;

        #region Properties

        /// <summary>Angle de glissement à l'essieu avant (radians)</summary>
        public float FrontSlipAngle => _frontSlipAngle;

        /// <summary>Angle de glissement à l'essieu arrière (radians)</summary>
        public float RearSlipAngle => _rearSlipAngle;

        /// <summary>Intensité du survirage normalisée (0 = aucun, 1 = maximum)</summary>
        public float OversteerIntensity => _oversteerIntensity;

        /// <summary>Intensité du sous-virage normalisée (0 = aucun, 1 = maximum)</summary>
        public float UndersteerIntensity => _understeerIntensity;

        #endregion

        /// <summary>
        /// Calcule les corrections issues du survirage/sous-virage.
        /// </summary>
        /// <param name="velocity">Vélocité actuelle du véhicule (espace monde, m/s)</param>
        /// <param name="vehicleTransform">Transform du véhicule</param>
        /// <param name="steeringInput">Entrée de direction [-1 gauche … 1 droite]</param>
        /// <param name="angularVelocity">Vitesse angulaire de lacet courante (rad/s)</param>
        /// <param name="deltaTime">Pas de temps (Time.fixedDeltaTime)</param>
        /// <param name="angularVelocityDelta">
        ///   Delta à ajouter à la vitesse angulaire de lacet (rad/s).
        ///   Positif = rotation droite, négatif = rotation gauche.
        /// </param>
        /// <returns>Correction de vélocité en espace monde (Vector3)</returns>
        public Vector3 ComputeSlipCorrection(
            Vector3 velocity,
            Transform vehicleTransform,
            float steeringInput,
            float angularVelocity,
            float deltaTime,
            out float angularVelocityDelta)
        {
            angularVelocityDelta = 0f;

            if (!enabled)
            {
                _frontSlipAngle = 0f;
                _rearSlipAngle = 0f;
                _oversteerIntensity = 0f;
                _understeerIntensity = 0f;
                return Vector3.zero;
            }

            float forwardSpeed = Vector3.Dot(velocity, vehicleTransform.forward);
            float lateralSpeed = Vector3.Dot(velocity, vehicleTransform.right);

            // En dessous de 1 m/s les angles de glissement ne sont pas significatifs
            if (Mathf.Abs(forwardSpeed) < 1f)
            {
                _frontSlipAngle = 0f;
                _rearSlipAngle = 0f;
                _oversteerIntensity = 0f;
                _understeerIntensity = 0f;
                return Vector3.zero;
            }

            float halfWB = wheelbase * 0.5f;

            // Vitesse latérale à chaque essieu (effet de la rotation du véhicule inclus)
            float frontLateralSpeed = PhysicsFormulas.AxleLateralVelocity(lateralSpeed, angularVelocity, halfWB, isFront: true);
            float rearLateralSpeed  = PhysicsFormulas.AxleLateralVelocity(lateralSpeed, angularVelocity, halfWB, isFront: false);

            // Angles de glissement par essieu
            float steeringAngleRad = steeringInput * maxSteeringAngleDeg * Mathf.Deg2Rad;
            _frontSlipAngle = PhysicsFormulas.SlipAngle(frontLateralSpeed, forwardSpeed) - steeringAngleRad;
            _rearSlipAngle  = PhysicsFormulas.SlipAngle(rearLateralSpeed,  forwardSpeed);

            float rearSlipAbs  = Mathf.Abs(_rearSlipAngle);
            float frontSlipAbs = Mathf.Abs(_frontSlipAngle);

            // Survirage : l'arrière glisse plus que l'avant
            _oversteerIntensity = Mathf.Clamp01(
                (rearSlipAbs - frontSlipAbs - oversteerThreshold) / (oversteerThreshold + THRESHOLD_EPSILON));

            // Sous-virage : l'avant glisse plus que l'arrière
            _understeerIntensity = Mathf.Clamp01(
                (frontSlipAbs - rearSlipAbs - understeerThreshold) / (understeerThreshold + THRESHOLD_EPSILON));

            Vector3 velocityCorrection = Vector3.zero;
            float speed = Mathf.Abs(forwardSpeed);

            // SURVIRAGE ─────────────────────────────────────────────────────────────
            // L'arrière part en glissade : amplifier la dérive latérale et accentuer
            // la rotation de lacet (le nez part plus loin dans le virage).
            if (_oversteerIntensity > 0f)
            {
                // Correction de vélocité : pousse dans la direction du glissement arrière
                float slideForce = _rearSlipAngle * _oversteerIntensity * oversteerStrength * speed;
                velocityCorrection += vehicleTransform.right * (slideForce * deltaTime);

                // Delta angulaire : augmente la rotation dans le sens du virage
                angularVelocityDelta += -_rearSlipAngle * _oversteerIntensity * oversteerStrength * oversteerYawFactor * deltaTime;
            }

            // SOUS-VIRAGE ────────────────────────────────────────────────────────────
            // L'avant résiste au virage : atténuer la rotation de lacet
            // (le volant est moins efficace, la voiture pousse tout droit).
            if (_understeerIntensity > 0f)
            {
                angularVelocityDelta -= Mathf.Sign(angularVelocity)
                    * _understeerIntensity
                    * understeerStrength
                    * Mathf.Abs(angularVelocity)
                    * understeerYawDampFactor * deltaTime;
            }

            return velocityCorrection;
        }
    }
}
