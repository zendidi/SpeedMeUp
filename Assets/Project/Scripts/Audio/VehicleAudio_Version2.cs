using UnityEngine;
using ArcadeRacer.Vehicle;

namespace ArcadeRacer.Audio
{
    /// <summary>
    /// Gère tous les sons d'un véhicule :  moteur, drift, collisions. 
    /// Le pitch du moteur varie selon la vitesse pour un effet réaliste.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public class VehicleAudio : MonoBehaviour
    {
        [Header("=== AUDIO SOURCES ===")]
        [SerializeField, Tooltip("AudioSource pour le son moteur (loop)")]
        private AudioSource engineAudioSource;

        [SerializeField, Tooltip("AudioSource pour le drift (loop)")]
        private AudioSource driftAudioSource;

        [SerializeField, Tooltip("AudioSource pour les collisions (one-shot)")]
        private AudioSource collisionAudioSource;

        [Header("=== AUDIO CLIPS ===")]
        [SerializeField] private AudioClip engineIdleClip;
        [SerializeField] private AudioClip engineRevClip;
        [SerializeField] private AudioClip driftClip;
        [SerializeField] private AudioClip[] collisionClips;

        [Header("=== ENGINE SETTINGS ===")]
        [SerializeField, Range(0.5f, 2f), Tooltip("Pitch minimum (idle)")]
        private float minPitch = 0.8f;

        [SerializeField, Range(1f, 3f), Tooltip("Pitch maximum (vitesse max)")]
        private float maxPitch = 2.0f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume du moteur")]
        private float engineVolume = 0.5f;

        [SerializeField, Tooltip("Vitesse de transition du pitch")]
        private float pitchTransitionSpeed = 2f;

        [Header("=== DRIFT SETTINGS ===")]
        [SerializeField, Range(0f, 1f), Tooltip("Volume du drift")]
        private float driftVolume = 0.4f;

        [SerializeField, Tooltip("Seuil de drift pour déclencher le son (angle de glisse)")]
        private float driftThreshold = 15f;

        [SerializeField, Tooltip("Vitesse de fade in/out du drift")]
        private float driftFadeSpeed = 3f;

        [Header("=== COLLISION SETTINGS ===")]
        [SerializeField, Range(0f, 1f), Tooltip("Volume des collisions")]
        private float collisionVolume = 0.6f;

        [SerializeField, Tooltip("Force minimum pour jouer un son de collision")]
        private float minCollisionForce = 5f;

        [SerializeField, Tooltip("Délai entre deux sons de collision")]
        private float collisionCooldown = 0.2f;

        // Runtime
        private VehicleController _vehicle;
        private float _targetEnginePitch;
        private float _targetDriftVolume;
        private float _lastCollisionTime;

        #region Unity Lifecycle

        private void Awake()
        {
            _vehicle = GetComponent<VehicleController>();
            SetupAudioSources();
        }

        private void Start()
        {
            StartEngineSound();
        }

        private void Update()
        {
            UpdateEngineSound();
            UpdateDriftSound();
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        #endregion

        #region Setup

        private void SetupAudioSources()
        {
            if (engineAudioSource != null && driftAudioSource != null && collisionAudioSource != null)
            {
                return;
            }

            // Créer les AudioSources s'ils n'existent pas
            if (engineAudioSource == null)
            {
                engineAudioSource = gameObject.AddComponent<AudioSource>();
                engineAudioSource.loop = true;
                engineAudioSource. playOnAwake = false;
                engineAudioSource.spatialBlend = 1f; // 3D sound
                engineAudioSource.minDistance = 5f;
                engineAudioSource.maxDistance = 50f;
            }

            if (driftAudioSource == null)
            {
                driftAudioSource = gameObject.AddComponent<AudioSource>();
                driftAudioSource.loop = true;
                driftAudioSource.playOnAwake = false;
                driftAudioSource. spatialBlend = 1f;
                driftAudioSource. minDistance = 5f;
                driftAudioSource.maxDistance = 40f;
            }

            if (collisionAudioSource == null)
            {
                collisionAudioSource = gameObject.AddComponent<AudioSource>();
                collisionAudioSource.loop = false;
                collisionAudioSource.playOnAwake = false;
                collisionAudioSource. spatialBlend = 1f;
                collisionAudioSource.minDistance = 3f;
                collisionAudioSource.maxDistance = 30f;
            }

            // Assigner les clips
            if (engineIdleClip != null)
            {
                engineAudioSource.clip = engineIdleClip;
            }

            if (driftClip != null)
            {
                driftAudioSource.clip = driftClip;
            }
        }

        #endregion

        #region Engine Sound

        private void StartEngineSound()
        {
            if (engineAudioSource != null && engineAudioSource.clip != null)
            {
                engineAudioSource.volume = engineVolume;
                engineAudioSource.pitch = minPitch;
                engineAudioSource.Play();
            }
        }

        private void UpdateEngineSound()
        {
            if (engineAudioSource == null || _vehicle == null) return;

            // Calculer le pitch cible basé sur la vitesse
            float speedPercentage = _vehicle.Physics.SpeedPercentage;
            _targetEnginePitch = Mathf.Lerp(minPitch, maxPitch, speedPercentage);

            // Smooth transition
            engineAudioSource.pitch = Mathf.Lerp(
                engineAudioSource.pitch,
                _targetEnginePitch,
                Time.deltaTime * pitchTransitionSpeed
            );

            // Ajuster le volume si le moteur ralentit
            float targetVolume = Mathf.Lerp(engineVolume * 0.7f, engineVolume, speedPercentage);
            engineAudioSource.volume = Mathf.Lerp(
                engineAudioSource.volume,
                targetVolume,
                Time.deltaTime * 2f
            );
        }

        #endregion

        #region Drift Sound

        private void UpdateDriftSound()
        {
            if (driftAudioSource == null || _vehicle == null) return;

            // Déterminer si on drift
            bool isDrifting = IsDrifting();

            // Volume cible
            _targetDriftVolume = isDrifting ? driftVolume : 0f;

            // Fade in/out
            float currentVolume = driftAudioSource.volume;
            driftAudioSource.volume = Mathf.Lerp(
                currentVolume,
                _targetDriftVolume,
                Time.deltaTime * driftFadeSpeed
            );

            // Démarrer/arrêter le son
            if (isDrifting && ! driftAudioSource.isPlaying)
            {
                driftAudioSource.Play();
            }
            else if (!isDrifting && driftAudioSource.volume < 0.01f && driftAudioSource.isPlaying)
            {
                driftAudioSource.Stop();
            }

            // Pitch variant selon la vitesse de drift
            if (isDrifting)
            {
                float speedPercentage = _vehicle. Physics.SpeedPercentage;
                driftAudioSource.pitch = Mathf.Lerp(0.8f, 1.2f, speedPercentage);
            }
        }

        private bool IsDrifting()
        {
            if (_vehicle == null || _vehicle.Physics == null) return false;

            // Vérifier si la voiture glisse (drift)
            // Tu peux ajuster cette logique selon ton système de drift
            float speed = _vehicle.Physics.CurrentSpeedKMH;
            
            // Simple détection :  vitesse > 20 km/h ET input de steering
            bool hasSpeed = speed > 20f;
            bool isSteering = Mathf.Abs(_vehicle._input.Steering) > 0.3f;

            return hasSpeed && isSteering;
        }

        #endregion

        #region Collision Sound

        private void HandleCollision(Collision collision)
        {
            if (collisionAudioSource == null || collisionClips. Length == 0) return;

            // Cooldown entre collisions
            if (Time.time - _lastCollisionTime < collisionCooldown) return;

            // Vérifier la force de l'impact
            float impactForce = collision.relativeVelocity.magnitude;
            
            if (impactForce < minCollisionForce) return;

            // Choisir un clip aléatoire
            AudioClip clip = collisionClips[Random. Range(0, collisionClips.Length)];

            // Volume proportionnel à la force (clampé)
            float volume = Mathf.Clamp01(impactForce / 20f) * collisionVolume;

            // Jouer le son
            collisionAudioSource.PlayOneShot(clip, volume);

            _lastCollisionTime = Time.time;

            Debug.Log($"[VehicleAudio] Collision!  Force: {impactForce: F1}, Volume: {volume:F2}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Jouer un son custom (ex: boost, power-up)
        /// </summary>
        public void PlayCustomSound(AudioClip clip, float volume = 1f)
        {
            if (collisionAudioSource != null && clip != null)
            {
                collisionAudioSource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>
        /// Changer le volume du moteur
        /// </summary>
        public void SetEngineVolume(float volume)
        {
            engineVolume = Mathf.Clamp01(volume);
            if (engineAudioSource != null)
            {
                engineAudioSource.volume = engineVolume;
            }
        }

        /// <summary>
        /// Activer/désactiver tous les sons
        /// </summary>
        public void SetMuted(bool muted)
        {
            if (engineAudioSource != null) engineAudioSource.mute = muted;
            if (driftAudioSource != null) driftAudioSource.mute = muted;
            if (collisionAudioSource != null) collisionAudioSource.mute = muted;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnValidate()
        {
            // S'assurer que les valeurs sont cohérentes
            if (minPitch > maxPitch) minPitch = maxPitch - 0.2f;
        }

        private void OnDrawGizmosSelected()
        {
            // Visualiser la portée du son
            if (engineAudioSource != null)
            {
                Gizmos. color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, engineAudioSource.maxDistance);
            }
        }
#endif

        #endregion
    }
}