using UnityEngine;
using ArcadeRacer.RaceSystem;
using System.Collections;

namespace ArcadeRacer.Audio
{
    /// <summary>
    /// Gère la musique de fond du jeu. 
    /// Change de musique selon l'état de la course.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        [Header("=== MUSIC CLIPS ===")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip raceMusic;
        [SerializeField] private AudioClip victoryMusic;

        [Header("=== SETTINGS ===")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
        [SerializeField] private float fadeInDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 1f;

        [Header("=== REFERENCES ===")]
        [SerializeField] private RaceManager raceManager;

        private AudioSource _audioSource;
        private Coroutine _fadeCoroutine;

        // Singleton
        public static MusicManager Instance { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupAudioSource();
        }

        private void Start()
        {
            FindReferences();
            SubscribeToRaceEvents();
            
            // Jouer la musique de menu au démarrage
            PlayMusic(menuMusic);
        }

        private void OnDestroy()
        {
            UnsubscribeFromRaceEvents();
        }

        #endregion

        #region Setup

        private void SetupAudioSource()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0f;
            _audioSource.spatialBlend = 0f; // 2D sound
        }

        private void FindReferences()
        {
            if (raceManager == null)
            {
                raceManager = FindFirstObjectByType<RaceManager>();
            }
        }

        private void SubscribeToRaceEvents()
        {
            if (raceManager != null)
            {
                raceManager.OnCountdownStarted += HandleCountdownStarted;
                raceManager.OnRaceStarted += HandleRaceStarted;
                raceManager.OnRaceFinished += HandleRaceFinished;
            }
        }

        private void UnsubscribeFromRaceEvents()
        {
            if (raceManager != null)
            {
                raceManager.OnCountdownStarted -= HandleCountdownStarted;
                raceManager.OnRaceStarted -= HandleRaceStarted;
                raceManager.OnRaceFinished -= HandleRaceFinished;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleCountdownStarted()
        {
            // Fade out menu music pendant le countdown
            FadeOut(fadeOutDuration * 0.5f);
        }

        private void HandleRaceStarted()
        {
            // Démarrer la musique de course
            PlayMusic(raceMusic, fadeInDuration);
        }

        private void HandleRaceFinished()
        {
            // Jouer la musique de victoire
            if (victoryMusic != null)
            {
                PlayMusic(victoryMusic, fadeInDuration);
            }
        }

        #endregion

        #region Music Control

        /// <summary>
        /// Jouer une musique avec fade in
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeDuration = -1f)
        {
            if (clip == null) return;

            // Utiliser la durée par défaut si non spécifiée
            if (fadeDuration < 0) fadeDuration = fadeInDuration;

            // Arrêter le fade en cours
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            // Si c'est la même musique, ne rien faire
            if (_audioSource.clip == clip && _audioSource.isPlaying)
            {
                return;
            }

            // Changer de musique
            _audioSource. clip = clip;
            _audioSource.Play();

            // Fade in
            _fadeCoroutine = StartCoroutine(FadeVolume(0f, musicVolume, fadeDuration));

            Debug.Log($"[MusicManager] Playing:  {clip.name}");
        }

        /// <summary>
        /// Arrêter la musique avec fade out
        /// </summary>
        public void StopMusic(float fadeDuration = -1f)
        {
            if (fadeDuration < 0) fadeDuration = fadeOutDuration;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
        }

        /// <summary>
        /// Fade out rapide
        /// </summary>
        public void FadeOut(float duration)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeVolume(_audioSource.volume, 0f, duration));
        }

        /// <summary>
        /// Fade in rapide
        /// </summary>
        public void FadeIn(float duration)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeVolume(_audioSource.volume, musicVolume, duration));
        }

        #endregion

        #region Coroutines

        private IEnumerator FadeVolume(float startVolume, float targetVolume, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            _audioSource.volume = targetVolume;
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            yield return FadeVolume(_audioSource.volume, 0f, duration);
            _audioSource.Stop();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Définir le volume de la musique
        /// </summary>
        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            _audioSource.volume = musicVolume;
        }

        /// <summary>
        /// Mettre en pause
        /// </summary>
        public void Pause()
        {
            _audioSource.Pause();
        }

        /// <summary>
        /// Reprendre
        /// </summary>
        public void Resume()
        {
            _audioSource.UnPause();
        }

        /// <summary>
        /// Mute/Unmute
        /// </summary>
        public void SetMuted(bool muted)
        {
            _audioSource.mute = muted;
        }

        #endregion
    }
}