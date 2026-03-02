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

        [Header("=== GRIP DES PNEUS ===")]
        [Tooltip("Coefficient d'adhérence latérale des pneus avant (μ). " +
                 "1.0 = standard, 0.7 = pneu usé, 1.4 = slick racing.")]
        [Range(0.1f, 2f)]
        public float frontGripCoefficient = 1.0f;

        [Tooltip("Coefficient d'adhérence latérale des pneus arrière (μ). " +
                 "Valeur inférieure au front = tendance naturelle au survirage.")]
        [Range(0.1f, 2f)]
        public float rearGripCoefficient = 0.9f;

        [Tooltip("Vitesse longitudinale de référence (m/s) à partir de laquelle les effets atteignent " +
                 "leur pleine intensité. En dessous : effets réduits proportionnellement. " +
                 "30 m/s ≈ 108 km/h.")]
        [Range(5f, 100f)]
        public float referenceSpeed = 30f;

        [Header("=== ZONE MORTE ===")]
        [Tooltip("Vitesse latérale minimale au CENTRE du véhicule (m/s) avant que le système s'active. " +
                 "Empêche les faux positifs lors des petits coups de volant en ligne droite. " +
                 "0.2 = insensible à moins de 0.2 m/s de glissement latéral.")]
        [Range(0f, 2f)]
        public float lateralVelocityDeadZone = 0.2f;

        [Header("=== SEUILS D'ACTIVATION ===")]
        [Tooltip("Différence d'angle de glissement normalisé (par le grip) pour déclencher le survirage. " +
                 "Augmenter pour réduire la sensibilité. Valeur recommandée : 0.05-0.15.")]
        [Range(0f, 0.5f)]
        public float oversteerThreshold = 0.08f;

        [Tooltip("Différence d'angle de glissement normalisé (par le grip) pour déclencher le sous-virage.")]
        [Range(0f, 0.5f)]
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

        [Header("=== TÊTE-À-QUEUE (survirage extrême) ===")]
        [Tooltip("Intensité de survirage à partir de laquelle le tête-à-queue se déclenche (0-1). " +
                 "En dessous : survirage normal contrôlable. Au-dessus : la voiture commence à pivoter.")]
        [Range(0f, 1f)]
        public float spinOutThreshold = 0.65f;

        [Tooltip("Multiplicateur de couple angulaire supplémentaire pendant le tête-à-queue. " +
                 "Plus cette valeur est haute, plus la rotation s'emballe vite.")]
        [Range(0f, 10f)]
        public float spinOutAngularMultiplier = 4f;

        [Tooltip("Vitesse angulaire maximale (rad/s) atteinte en tête-à-queue total. " +
                 "5 rad/s = 286°/s ≈ 1 tour en 1.3s ; 15 rad/s ≈ 1 tour en 0.4s.")]
        [Range(5f, 30f)]
        public float spinOutMaxAngularVelocity = 12f;

        [Header("=== DÉPORT EXTÉRIEUR (sous-virage extrême) ===")]
        [Tooltip("Intensité de sous-virage à partir de laquelle la voiture est déportée vers l'extérieur du virage (0-1).")]
        [Range(0f, 1f)]
        public float understeerPushThreshold = 0.5f;

        [Tooltip("Force de déport vers l'extérieur du virage en sous-virage extrême (m/s de delta par seconde). " +
                 "Valeurs typiques : 2-8.")]
        [Range(0f, 15f)]
        public float understeerOutwardPushStrength = 4f;

        // Epsilon pour éviter la division par zéro dans les intensités normalisées
        private const float THRESHOLD_EPSILON = 0.001f;

        // === État calculé (lecture seule) ===
        private float _frontSlipAngle;
        private float _rearSlipAngle;
        private float _oversteerIntensity;
        private float _understeerIntensity;
        private float _spinOutIntensity;

        #region Properties

        /// <summary>Angle de glissement à l'essieu avant (radians)</summary>
        public float FrontSlipAngle => _frontSlipAngle;

        /// <summary>Angle de glissement à l'essieu arrière (radians)</summary>
        public float RearSlipAngle => _rearSlipAngle;

        /// <summary>Intensité du survirage normalisée (0 = aucun, 1 = maximum)</summary>
        public float OversteerIntensity => _oversteerIntensity;

        /// <summary>Intensité du sous-virage normalisée (0 = aucun, 1 = maximum)</summary>
        public float UndersteerIntensity => _understeerIntensity;

        /// <summary>
        /// Intensité du tête-à-queue normalisée (0 = aucun, 1 = spin total).
        /// Non-nul uniquement quand l'oversteer dépasse spinOutThreshold.
        /// </summary>
        public float SpinOutIntensity => _spinOutIntensity;

        #endregion

        /// <summary>
        /// Calcule les corrections issues du survirage/sous-virage.
        /// </summary>
        /// <param name="velocity">Vélocité actuelle du véhicule (espace monde, m/s)</param>
        /// <param name="vehicleTransform">Transform du véhicule</param>
        /// <param name="steeringInput">Entrée de direction [-1 gauche … 1 droite]</param>
        /// <param name="angularVelocity">Vitesse angulaire de lacet courante (rad/s)</param>
        /// <param name="deltaTime">Pas de temps (Time.fixedDeltaTime)</param>
        /// <param name="frontAxleLoad">Charge normalisée sur l'essieu avant (0-1, issue du transfert de charge)</param>
        /// <param name="rearAxleLoad">Charge normalisée sur l'essieu arrière (0-1)</param>
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
            float frontAxleLoad,
            float rearAxleLoad,
            out float angularVelocityDelta)
        {
            angularVelocityDelta = 0f;

            if (!enabled)
            {
                _frontSlipAngle = 0f;
                _rearSlipAngle = 0f;
                _oversteerIntensity = 0f;
                _understeerIntensity = 0f;
                _spinOutIntensity = 0f;
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
                _spinOutIntensity = 0f;
                return Vector3.zero;
            }

            // ─── ZONE MORTE ──────────────────────────────────────────────────────────
            // Ne pas calculer si le centre du véhicule ne glisse pas latéralement.
            // Empêche les faux positifs sur ligne droite lors des petits coups de volant
            // (ω crée de la vitesse latérale aux essieux mais pas au centre de masse).
            if (Mathf.Abs(lateralSpeed) < lateralVelocityDeadZone)
            {
                _frontSlipAngle = 0f;
                _rearSlipAngle = 0f;
                _oversteerIntensity = 0f;
                _understeerIntensity = 0f;
                _spinOutIntensity = 0f;
                return Vector3.zero;
            }

            float halfWB = wheelbase * 0.5f;

            // Vitesse latérale à chaque essieu (effet de la rotation du véhicule inclus)
            float frontLateralSpeed = PhysicsFormulas.AxleLateralVelocity(lateralSpeed, angularVelocity, halfWB, isFront: true);
            float rearLateralSpeed  = PhysicsFormulas.AxleLateralVelocity(lateralSpeed, angularVelocity, halfWB, isFront: false);

            // Angles de glissement par essieu
            float steeringAngleRad = steeringInput * maxSteeringAngleDeg * Mathf.Deg2Rad;
            // _frontSlipAngle conserve la compensation de braquage (utilisée pour la direction de correction)
            _frontSlipAngle = PhysicsFormulas.SlipAngle(frontLateralSpeed, forwardSpeed) - steeringAngleRad;
            _rearSlipAngle  = PhysicsFormulas.SlipAngle(rearLateralSpeed,  forwardSpeed);

            // ─── DÉTECTION PAR ANGLES RAW (sans compensation steering) ───────────────
            // Utiliser les angles bruts (non-compensés) garantit la SYMÉTRIE en ligne droite :
            // en ligne droite (v_lat = 0), ω pousse le front et le rear avec des vitesses latérales
            // égales et opposées → |frontRaw| = |rearRaw| → aucun régime ne se déclenche.
            // La compensation de steering (-steeringAngleRad) ne s'applique QU'À la correction
            // physique (direction du vecteur), PAS à la comparaison d'intensités.
            float frontSlipRaw = PhysicsFormulas.SlipAngle(frontLateralSpeed, forwardSpeed);
            float rearSlipAbs  = Mathf.Abs(_rearSlipAngle);
            float frontSlipAbs = Mathf.Abs(frontSlipRaw);

            // Normalisation par le grip effectif de chaque essieu
            // grip_effectif = μ_pneu × fraction_de_charge
            // Un essieu avec moins de grip ou moins de charge "sature" plus facilement.
            float rearEffectiveGrip  = rearGripCoefficient  * rearAxleLoad;
            float frontEffectiveGrip = frontGripCoefficient * frontAxleLoad;

            float rearNormSlip  = rearSlipAbs  / Mathf.Max(rearEffectiveGrip,  THRESHOLD_EPSILON);
            float frontNormSlip = frontSlipAbs / Mathf.Max(frontEffectiveGrip, THRESHOLD_EPSILON);

            // Facteur vitesse : les effets montent en intensité avec la vitesse longitudinale.
            float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / (referenceSpeed + THRESHOLD_EPSILON));

            // Survirage : essieu arrière normalisé > essieu avant normalisé
            _oversteerIntensity = Mathf.Clamp01(speedFactor * Mathf.Max(0f,
                (rearNormSlip - frontNormSlip - oversteerThreshold) / (oversteerThreshold + THRESHOLD_EPSILON)));

            // Sous-virage : essieu avant normalisé > essieu arrière normalisé
            _understeerIntensity = Mathf.Clamp01(speedFactor * Mathf.Max(0f,
                (frontNormSlip - rearNormSlip - understeerThreshold) / (understeerThreshold + THRESHOLD_EPSILON)));

            // Tête-à-queue : intensité du survirage au-delà du seuil critique
            _spinOutIntensity = Mathf.Clamp01(
                (_oversteerIntensity - spinOutThreshold) / (1f - spinOutThreshold + THRESHOLD_EPSILON));

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

                // Delta angulaire de base : augmente la rotation dans le sens du virage
                angularVelocityDelta += -_rearSlipAngle * _oversteerIntensity * oversteerStrength * oversteerYawFactor * deltaTime;

                // TÊTE-À-QUEUE : survirage extrême → couple auto-entretenu
                // L'arrière a lâché → la rotation s'emballe car il n'y a plus de force
                // de rappel depuis l'essieu arrière.
                if (_spinOutIntensity > 0f)
                {
                    angularVelocityDelta += -Mathf.Sign(_rearSlipAngle)
                        * _spinOutIntensity
                        * spinOutAngularMultiplier
                        * deltaTime;
                }
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

                // DÉPORT EXTÉRIEUR : sous-virage extrême → la voiture est poussée
                // vers l'extérieur du virage (comme quand on prend un virage trop vite).
                if (_understeerIntensity > understeerPushThreshold && Mathf.Abs(steeringInput) > 0.05f)
                {
                    float pushIntensity = (_understeerIntensity - understeerPushThreshold)
                        / (1f - understeerPushThreshold + THRESHOLD_EPSILON);

                    // Direction extérieure = opposée à la direction de braquage
                    // Si steering droite (+1) → extérieur = gauche (-transform.right)
                    float outwardForce = -steeringInput * pushIntensity
                        * understeerOutwardPushStrength * speed * deltaTime;
                    velocityCorrection += vehicleTransform.right * outwardForce;
                }
            }

            return velocityCorrection;
        }
    }
}
