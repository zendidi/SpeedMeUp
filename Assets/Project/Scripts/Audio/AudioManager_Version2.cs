using UnityEngine;
using UnityEngine.Audio;

namespace ArcadeRacer.Audio
{
    /// <summary>
    /// Gestionnaire global de l'audio du jeu. 
    /// Gère le mixer, les volumes, et les préférences audio.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("=== AUDIO MIXER ===")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("=== VOLUME SETTINGS ===")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("=== MIXER GROUPS ===")]
        private const string MASTER_VOLUME = "MasterVolume";
        private const string MUSIC_VOLUME = "MusicVolume";
        private const string SFX_VOLUME = "SFXVolume";

        // Singleton
        public static AudioManager Instance { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern
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

            LoadAudioSettings();
            ApplyAudioSettings();
        }

        #endregion

        #region Volume Control

        /// <summary>
        /// Définir le volume master
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf. Clamp01(volume);
            SetMixerVolume(MASTER_VOLUME, masterVolume);
            SaveAudioSettings();
        }

        /// <summary>
        /// Définir le volume de la musique
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            SetMixerVolume(MUSIC_VOLUME, musicVolume);
            SaveAudioSettings();
        }

        /// <summary>
        /// Définir le volume des SFX
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SetMixerVolume(SFX_VOLUME, sfxVolume);
            SaveAudioSettings();
        }

        private void SetMixerVolume(string parameterName, float volume)
        {
            if (audioMixer == null) return;

            // Convertir 0-1 en décibels (-80 à 0)
            float db = volume > 0 ?  Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat(parameterName, db);
        }

        #endregion

        #region Settings Persistence

        private void ApplyAudioSettings()
        {
            SetMixerVolume(MASTER_VOLUME, masterVolume);
            SetMixerVolume(MUSIC_VOLUME, musicVolume);
            SetMixerVolume(SFX_VOLUME, sfxVolume);
        }

        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }

        private void LoadAudioSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        #endregion

        #region Public API

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;

        #endregion
    }
}