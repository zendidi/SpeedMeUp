using UnityEngine;

namespace ArcadeRacer.VFX
{
    /// <summary>
    /// Gère les particules de fumée pendant le drift. 
    /// Émission proportionnelle à la vitesse et l'intensité du drift.
    /// </summary>
    public class DriftParticles : MonoBehaviour
    {
        [Header("=== PARTICLE SYSTEMS ===")]
        [SerializeField] private ParticleSystem leftWheelParticles;
        [SerializeField] private ParticleSystem rightWheelParticles;

        [Header("=== SETTINGS ===")]
        [SerializeField] private float maxEmissionRate = 50f;
        [SerializeField] private float minSpeedForDrift = 0.3f;
        [SerializeField] private Color driftColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        [SerializeField] private Material driftMaterial;


        private ParticleSystem.EmissionModule _leftEmission;
        private ParticleSystem.EmissionModule _rightEmission;
        private ParticleSystem.MainModule _leftMain;
        private ParticleSystem.MainModule _rightMain;

        #region Unity Lifecycle

        private void Awake()
        {
            SetupParticleSystems();
        }

        #endregion

        #region Setup

        private void SetupParticleSystems()
        {
            // Créer les particle systems s'ils n'existent pas
            if (leftWheelParticles == null)
            {
                leftWheelParticles = CreateDriftParticleSystem("DriftParticles_Left");
            }

            if (rightWheelParticles == null)
            {
                rightWheelParticles = CreateDriftParticleSystem("DriftParticles_Right");
            }

            // Récupérer les modules
            if (leftWheelParticles != null)
            {
                _leftEmission = leftWheelParticles.emission;
                _leftMain = leftWheelParticles.main;
            }

            if (rightWheelParticles != null)
            {
                _rightEmission = rightWheelParticles.emission;
                _rightMain = rightWheelParticles.main;
            }
        }

        private ParticleSystem CreateDriftParticleSystem(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            // Configuration du particle system
            var main = ps.main;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.startSize = 0.5f;
            main.startColor = driftColor;
            main.gravityModifier = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // Contrôlé manuellement

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            // Diminution de l'alpha au fil du temps
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Taille diminue
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            // Appliquer le matériau si défini
            if (driftMaterial != null)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.material = driftMaterial;
            }

            return ps;
        }

        #endregion

        #region Drift Control

        /// <summary>
        /// Activer/désactiver les particules de drift
        /// </summary>
        public void SetDrifting(bool isDrifting, float speedPercentage)
        {
            if (leftWheelParticles == null || rightWheelParticles == null) return;

            if (isDrifting && speedPercentage >= minSpeedForDrift)
            {
                // Calculer le taux d'émission selon la vitesse
                float emissionRate = Mathf.Lerp(0f, maxEmissionRate, speedPercentage);

                _leftEmission.rateOverTime = emissionRate;
                _rightEmission.rateOverTime = emissionRate;

                // Démarrer si pas encore actif
                if (!leftWheelParticles.isPlaying) leftWheelParticles.Play();
                if (!rightWheelParticles.isPlaying) rightWheelParticles.Play();
            }
            else
            {
                // Arrêter progressivement
                _leftEmission.rateOverTime = 0f;
                _rightEmission.rateOverTime = 0f;

                // Arrêter complètement après un délai
                if (leftWheelParticles.isPlaying && leftWheelParticles.particleCount == 0)
                {
                    leftWheelParticles.Stop();
                }

                if (rightWheelParticles.isPlaying && rightWheelParticles.particleCount == 0)
                {
                    rightWheelParticles.Stop();
                }
            }
        }

        /// <summary>
        /// Positionner les particules sur les roues arrière
        /// </summary>
        public void SetWheelPositions(Transform leftWheel, Transform rightWheel)
        {
            if (leftWheelParticles != null && leftWheel != null)
            {
                leftWheelParticles.transform.position = leftWheel.position;
            }

            if (rightWheelParticles != null && rightWheel != null)
            {
                rightWheelParticles.transform.position = rightWheel.position;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Changer la couleur des particules
        /// </summary>
        public void SetParticleColor(Color color)
        {
            driftColor = color;
            if (_leftMain.startColor.color != color) _leftMain.startColor = color;
            if (_rightMain.startColor.color != color) _rightMain.startColor = color;
        }

        #endregion
    }
}