using UnityEngine;

namespace ArcadeRacer. VFX
{
    /// <summary>
    /// Gère les effets visuels lors des collisions.  
    /// Crée des particules d'impact au point de contact.
    /// </summary>
    public class CollisionVFX : MonoBehaviour
    {
        [Header("=== PARTICLE SYSTEM ===")]
        [SerializeField] private ParticleSystem collisionParticles;

        [Header("=== SETTINGS ===")]
        [SerializeField] private float minImpactForce = 5f;
        [SerializeField] private int particlesPerImpact = 20;
        [SerializeField] private Color impactColor = new Color(1f, 0.5f, 0f, 1f);
        [SerializeField] private float particleSize = 0.2f;

        private float _lastCollisionTime = 0f;
        private float _collisionCooldown = 0.1f;

        #region Unity Lifecycle

        private void Awake()
        {
            SetupParticleSystem();
        }

        #endregion

        #region Setup

        private void SetupParticleSystem()
        {
            if (collisionParticles == null)
            {
                collisionParticles = CreateCollisionParticleSystem();
            }
        }

        private ParticleSystem CreateCollisionParticleSystem()
        {
            GameObject go = new GameObject("CollisionParticles");
            go.transform. parent = transform;
            go. transform.localPosition = Vector3.zero;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            // Configuration
            var main = ps.main;
            main.startLifetime = 0.5f;
            main. startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = particleSize;
            main.startColor = impactColor;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = false; // Contrôlé manuellement via Emit()

            var shape = ps. shape;
            shape.shapeType = ParticleSystemShapeType. Sphere;
            shape.radius = 0.3f;

            return ps;
        }

        #endregion

        #region Collision Effects

        /// <summary>
        /// Jouer l'effet de collision
        /// </summary>
        public void PlayCollisionEffect(Collision collision)
        {
            if (collisionParticles == null) return;

            // Cooldown
            if (Time.time - _lastCollisionTime < _collisionCooldown) return;

            // Vérifier la force d'impact
            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce < minImpactForce) return;

            // Obtenir le point de contact
            ContactPoint contact = collision.GetContact(0);
            Vector3 impactPoint = contact. point;
            Vector3 impactNormal = contact.normal;

            // Positionner et orienter les particules
            collisionParticles.transform.position = impactPoint;
            collisionParticles.transform.rotation = Quaternion.LookRotation(impactNormal);

            // Calculer le nombre de particules selon la force
            int particleCount = Mathf.RoundToInt(Mathf.Lerp(10f, particlesPerImpact, impactForce / 20f));

            // Émettre les particules
            collisionParticles. Emit(particleCount);

            _lastCollisionTime = Time.time;

            Debug.Log($"[CollisionVFX] Impact at {impactPoint}, force: {impactForce: F1}, particles: {particleCount}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Changer la couleur des particules
        /// </summary>
        public void SetImpactColor(Color color)
        {
            impactColor = color;
            if (collisionParticles != null)
            {
                var main = collisionParticles. main;
                main.startColor = color;
            }
        }

        #endregion
    }
}