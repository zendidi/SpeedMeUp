using UnityEngine;

namespace ArcadeRacer.VFX
{
    /// <summary>
    /// Crée un effet de lignes de vitesse (speed lines) autour de la caméra.  
    /// L'intensité augmente avec la vitesse du véhicule.
    /// </summary>
    public class SpeedLines : MonoBehaviour
    {
        [Header("=== PARTICLE SYSTEM ===")]
        [SerializeField] private ParticleSystem speedLinesParticles;

        [Header("=== SETTINGS ===")]
        [SerializeField] private float minSpeed = 0.5f;
        [SerializeField] private float maxEmissionRate = 100f;
        [SerializeField] private Color speedLineColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private float particleSpeed = 20f;

        [Header("=== CAMERA ===")]
        [SerializeField] private Transform cameraTransform;

        private ParticleSystem. EmissionModule _emission;
        private ParticleSystem.MainModule _main;
        private float _currentSpeed = 0f;
        public Material SpeedLineMaterial ;

        #region Unity Lifecycle

        private void Awake()
        {
            SetupParticleSystem();
            FindCamera();
        }

        private void LateUpdate()
        {
            // Suivre la caméra
            if (cameraTransform != null)
            {
                transform.position = cameraTransform. position;
                transform.rotation = cameraTransform.rotation;
            }
        }

        #endregion

        #region Setup

        private void SetupParticleSystem()
        {
            if (speedLinesParticles == null)
            {
                speedLinesParticles = CreateSpeedLinesParticleSystem();
            }

            if (speedLinesParticles != null)
            {
                _emission = speedLinesParticles. emission;
                _main = speedLinesParticles.main;
            }
        }

        private ParticleSystem CreateSpeedLinesParticleSystem()
        {
            GameObject go = new GameObject("SpeedLines_Particles");
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            // Configuration
            var main = ps.main;
            main.startLifetime = 0.5f;
            main. startSpeed = particleSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor = speedLineColor;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType. Sphere;
            shape.radius = 5f;
            shape.radiusThickness = 1f;

            // Renderer
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2f;
            renderer.velocityScale = 0.5f;
            renderer.material = SpeedLineMaterial;
            return ps;
        }

        private void FindCamera() 
        {
            if (cameraTransform == null)
            {
                UnityEngine.Camera mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                }
            }
        }

        #endregion

        #region Speed Control

        /// <summary>
        /// Définir la vitesse (0-1)
        /// </summary>
        public void SetSpeed(float speedPercentage)
        {
            if (speedLinesParticles == null) return;

            _currentSpeed = speedPercentage;

            if (speedPercentage >= minSpeed)
            {
                // Calculer l'émission selon la vitesse
                float emissionRate = Mathf.Lerp(0f, maxEmissionRate, (speedPercentage - minSpeed) / (1f - minSpeed));
                _emission.rateOverTime = emissionRate;

                // Démarrer si pas actif
                if (! speedLinesParticles.isPlaying)
                {
                    speedLinesParticles.Play();
                }
            }
            else
            {
                // Arrêter l'émission
                _emission.rateOverTime = 0f;

                // Arrêter complètement si plus de particules
                if (speedLinesParticles.isPlaying && speedLinesParticles.particleCount == 0)
                {
                    speedLinesParticles. Stop();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Activer/désactiver l'effet
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (speedLinesParticles != null)
            {
                if (enabled)
                {
                    speedLinesParticles. Play();
                }
                else
                {
                    speedLinesParticles.Stop();
                }
            }
        }

        #endregion
    }
}