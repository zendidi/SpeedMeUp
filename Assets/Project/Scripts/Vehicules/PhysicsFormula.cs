using UnityEngine;

namespace ArcadeRacer.Physics
{
    /// <summary>
    /// Formules de physique mécanique classique pour véhicules
    /// </summary>
    public static class PhysicsFormulas
    {
        // Constantes physiques
        public const float AIR_DENSITY = 1.225f; // kg/m³ (niveau de la mer, 15°C)
        public const float GRAVITY = 9.81f; // m/s²

        #region Inertie Linéaire

        /// <summary>
        /// Calcule la résistance au roulement
        /// F = Crr × m × g
        /// </summary>
        public static float RollingResistance(float mass, float rollingCoefficient)
        {
            return rollingCoefficient * mass * GRAVITY;
        }

        /// <summary>
        /// Calcule la résistance aérodynamique (traînée)
        /// F = 0.5 × ρ × Cd × A × v²
        /// </summary>
        public static float AerodynamicDrag(float speed, float dragCoefficient, float frontalArea)
        {
            return 0.5f * AIR_DENSITY * dragCoefficient * frontalArea * speed * speed;
        }

        /// <summary>
        /// Décélération totale sans accélérateur (coast down)
        /// a = (F_roulement + F_air) / m
        /// </summary>
        public static float CoastDownDeceleration(float speed, float mass, float rollingCoefficient, float dragCoefficient, float frontalArea)
        {
            float fRolling = RollingResistance(mass, rollingCoefficient);
            float fAir = AerodynamicDrag(speed, dragCoefficient, frontalArea);
            return (fRolling + fAir) / mass;
        }

        #endregion

        #region Inertie Angulaire

        /// <summary>
        /// Moment d'inertie simplifié pour un véhicule (approximation cylindre)
        /// I = 0.5 × m × r²
        /// Pour arcade : r = longueur/2
        /// </summary>
        public static float MomentOfInertia(float mass, float vehicleLength)
        {
            float r = vehicleLength * 0.5f;
            return 0.5f * mass * r * r;
        }

        /// <summary>
        /// Décélération angulaire due au frottement
        /// α = -k × ω (amortissement proportionnel)
        /// </summary>
        public static float AngularDeceleration(float angularVelocity, float dampingCoefficient)
        {
            return -dampingCoefficient * angularVelocity;
        }

        #endregion

        #region Angles de Glissement (Slip Angles)

        /// <summary>
        /// Calcule l'angle de glissement d'un pneu (slip angle).
        /// α = atan2(v_latéral, |v_longitudinal| + ε)
        /// </summary>
        /// <param name="lateralVelocity">Composante latérale de la vélocité à l'essieu (m/s)</param>
        /// <param name="forwardVelocity">Composante longitudinale de la vélocité (m/s)</param>
        /// <returns>Angle de glissement en radians</returns>
        public static float SlipAngle(float lateralVelocity, float forwardVelocity)
        {
            // Le +0.1f évite la singularité à vitesse nulle et lisse le comportement
            // à très basse vitesse (différent du THRESHOLD_EPSILON de division pure).
            return Mathf.Atan2(lateralVelocity, Mathf.Abs(forwardVelocity) + 0.1f);
        }

        /// <summary>
        /// Calcule la vitesse latérale à un essieu en tenant compte de la rotation de lacet du véhicule.
        /// v_essieu_avant  = v_lat + ω × (L/2)
        /// v_essieu_arrière = v_lat − ω × (L/2)
        /// </summary>
        /// <param name="lateralVelocity">Vitesse latérale au centre de masse (m/s)</param>
        /// <param name="angularVelocity">Vitesse angulaire de lacet du véhicule (rad/s)</param>
        /// <param name="halfWheelbase">Demi-empattement en mètres</param>
        /// <param name="isFront">Vrai pour l'essieu avant, faux pour l'essieu arrière</param>
        /// <returns>Vitesse latérale à l'essieu (m/s)</returns>
        public static float AxleLateralVelocity(float lateralVelocity, float angularVelocity, float halfWheelbase, bool isFront)
        {
            return isFront
                ? lateralVelocity + angularVelocity * halfWheelbase
                : lateralVelocity - angularVelocity * halfWheelbase;
        }

        #endregion

        #region Transfert de Charge

        /// <summary>
        /// Transfert de charge longitudinal (freinage/accélération)
        /// ΔF = (m × a × h) / L
        /// </summary>
        public static float LongitudinalWeightTransfer(float mass, float acceleration, float centerOfGravityHeight, float wheelbase)
        {
            return (mass * acceleration * centerOfGravityHeight) / wheelbase;
        }

        #endregion
    }
}