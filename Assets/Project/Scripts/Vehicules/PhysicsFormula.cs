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