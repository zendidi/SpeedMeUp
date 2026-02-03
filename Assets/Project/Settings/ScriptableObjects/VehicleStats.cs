using UnityEngine;

namespace ArcadeRacer.Settings
{
    /// <summary>
    /// ScriptableObject contenant toutes les statistiques et paramètres d'un véhicule. 
    /// Permet de créer différents profils de voitures avec des comportements variés.
    /// </summary>
    [CreateAssetMenu(fileName = "New Vehicle Stats", menuName = "Arcade Racer/Vehicle Stats")]
    public class VehicleStats : ScriptableObject
    {
        [Header("=== ACCELERATION & SPEED ===")]
        [Tooltip("Vitesse maximale en km/h")]
        [Range(50f, 300f)]
        public float maxSpeed = 120f;

        [Tooltip("Force d'accélération")]
        [Range(500f, 5000f)]
        public float accelerationForce = 2000f;

        [Tooltip("Vitesse de décélération naturelle (sans frein)")]
        [Range(1f, 50f)]
        public float naturalDecelerationInverted = 2f;

        [Tooltip("Force de freinage")]
        [Range(1000f, 8000f)]
        public float brakeForce = 3500f;

        [Tooltip("Force de la marche arrière (% de l'accélération)")]
        [Range(0.2f, 0.8f)]
        public float reverseSpeedMultiplier = 0.5f;

        [Header("=== TORQUE CURVE ===")]
        [Tooltip("Courbe de torque du moteur en fonction de la vitesse")]
        public TorquePoint[] torqueCurve = new TorquePoint[]
        {
            new TorquePoint(0.0f, 1.0f),   // Bas régime : couple max
            new TorquePoint(0.3f, 1.2f),   // Mi-régime : pic de couple
            new TorquePoint(0.7f, 0.9f),   // Haut régime : couple diminue
            new TorquePoint(1.0f, 0.6f)    // Vitesse max : couple faible
        };

        [Tooltip("Seuil de vitesse pour la friction statique (0-1)")]
        [Range(0f, 0.2f)]
        public float staticFrictionThreshold = 0.05f;

        [Tooltip("Facteur de friction statique au démarrage")]
        [Range(0.5f, 1.0f)]
        public float staticFrictionFactor = 0.8f;

        [Header("=== STEERING & HANDLING ===")]
        [Tooltip("Vitesse de rotation en degrés/seconde")]
        [Range(50f, 300f)]
        public float steeringSpeed = 150f;

        [Tooltip("Multiplicateur de steering à basse vitesse")]
        [Range(0.5f, 2f)]
        public float lowSpeedSteeringMultiplier = 1.2f;

        [Tooltip("Multiplicateur de steering à haute vitesse")]
        [Range(0.3f, 1f)]
        public float highSpeedSteeringMultiplier = 0.6f;

        [Tooltip("Force d'adhérence latérale (empêche le glissement)")]
        [Range(0.5f, 10f)]
        public float gripStrength = 5f;

        [Tooltip("Vitesse de retour au centre du volant")]
        [Range(1f, 10f)]
        public float steeringReturnSpeed = 5f;

        [Tooltip("Perte de vitesse en virage")]
        [Range(0f, 2f)]
        public float turningSpeedLoss = 0.3f;

        [Tooltip("Multiplicateur de drag selon la vitesse")]
        [Range(0f, 2f)]
        public float speedDragMultiplier = 1.0f;

        [Header("=== DRIFT SYSTEM ===")]
        [Tooltip("Activer le système de drift")]
        public bool allowDrift = true;

        [Tooltip("Réduction d'adhérence pendant le drift (plus bas = plus de glisse)")]
        [Range(0.1f, 0.9f)]
        public float driftGripReduction = 0.4f;

        [Tooltip("Multiplicateur de direction pendant le drift")]
        [Range(0.8f, 2f)]
        public float driftSteeringMultiplier = 1.2f;

        [Tooltip("Force latérale appliquée pendant le drift")]
        [Range(0f, 50f)]
        public float driftForce = 15f;

        [Tooltip("Vitesse minimale pour pouvoir drifter (en km/h)")]
        [Range(10f, 60f)]
        public float minDriftSpeed = 30f;

        [Header("=== BRAKING ===")]
        [Tooltip("Seuil de vitesse basse pour le freinage (km/h)")]
        [Range(0f, 50f)]
        public float brakeMinSpeedThreshold = 10f;

        [Tooltip("Seuil de vitesse haute pour le freinage (km/h)")]
        [Range(50f, 200f)]
        public float brakeMaxSpeedThreshold = 100f;

        [Tooltip("Efficacité du frein à basse vitesse")]
        [Range(0.5f, 2f)]
        public float brakeLowSpeedEfficiency = 1.5f;

        [Tooltip("Efficacité du frein à haute vitesse")]
        [Range(0.3f, 1f)]
        public float brakeHighSpeedEfficiency = 0.8f;

        [Header("=== PHYSICS ===")]
        [Tooltip("Masse du véhicule en kg")]
        [Range(100f, 2000f)]
        public float mass = 1000f;

        [Tooltip("Centre de masse Y (plus bas = plus stable)")]
        [Range(-1f, 1f)]
        public float centerOfMassY = -0.5f;

        [Tooltip("Drag aérodynamique (résistance à l'air)")]
        [Range(0f, 2f)]
        public float drag = 0.3f;

        [Tooltip("Drag angulaire (résistance à la rotation)")]
        [Range(0f, 5f)]
        public float angularDrag = 1.5f;

        [Header("=== GROUND CHECK ===")]
        [Tooltip("Distance de détection du sol")]
        [Range(0.1f, 2f)]
        public float groundCheckDistance = 0.5f;

        [Tooltip("Force de maintien au sol")]
        [Range(10f, 100f)]
        public float downForce = 50f;

        [Header("=== COLLISIONS ===")]
        [Tooltip("Multiplicateur de rebond contre les murs")]
        [Range(0f, 1f)]
        public float wallBounceMultiplier = 0.5f;

        [Tooltip("Facteur de rebond entre véhicules")]
        [Range(0f, 1f)]
        public float vehicleBounceFactor = 0.7f;

        [Header("=== ARCADE FEEL ===")]
        [Tooltip("Rotation automatique vers le haut en l'air")]
        public bool autoFlip = true;

        [Tooltip("Vitesse de rotation automatique")]
        [Range(1f, 10f)]
        public float autoFlipSpeed = 3f;

        [Tooltip("Lissage de la rotation (plus haut = plus smooth)")]
        [Range(1f, 20f)]
        public float rotationSmoothing = 10f;

        [Header("=== HELPER PROPERTIES ===")]
        /// <summary>
        /// Vitesse max en m/s (calculée automatiquement depuis maxSpeed en km/h)
        /// </summary>
        public float MaxSpeedMS => maxSpeed / 3.6f;

        /// <summary>
        /// Vitesse minimale de drift en m/s
        /// </summary>
        public float MinDriftSpeedMS => minDriftSpeed / 3.6f;

        #region Validation
        private void OnValidate()
        {
            // S'assurer que les valeurs restent cohérentes
            maxSpeed = Mathf.Max(50f, maxSpeed);
            accelerationForce = Mathf.Max(500f, accelerationForce);
            brakeForce = Mathf.Max(1000f, brakeForce);
            steeringSpeed = Mathf.Max(50f, steeringSpeed);
            mass = Mathf.Max(100f, mass);

            // Le drift nécessite une vitesse minimale raisonnable
            if (minDriftSpeed > maxSpeed * 0.5f)
            {
                minDriftSpeed = maxSpeed * 0.3f;
            }

            // Validation de la courbe de torque
            if (torqueCurve == null || torqueCurve.Length == 0)
            {
                torqueCurve = new TorquePoint[]
                {
                    new TorquePoint(0.0f, 1.0f),
                    new TorquePoint(0.3f, 1.2f),
                    new TorquePoint(0.7f, 0.9f),
                    new TorquePoint(1.0f, 0.6f)
                };
            }
        }
        #endregion
    }

    /// <summary>
    /// Point de la courbe de torque
    /// </summary>
    [System.Serializable]
    public struct TorquePoint
    {
        [Tooltip("Ratio de vitesse (0 = arrêt, 1 = vitesse max)")]
        [Range(0f, 1f)]
        public float speedRatio;

        [Tooltip("Multiplicateur de torque à cette vitesse")]
        [Range(0f, 2f)]
        public float torque;

        public TorquePoint(float speedRatio, float torque)
        {
            this.speedRatio = speedRatio;
            this.torque = torque;
        }
    }
}