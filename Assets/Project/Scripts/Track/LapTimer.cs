using UnityEngine;
using System.Collections.Generic;

namespace ArcadeRacer.RaceSystem
{
    /// <summary>
    /// Gère le chronomètre pour un véhicule spécifique.  
    /// Track les temps par tour et le meilleur temps.
    /// </summary>
    public class LapTimer : MonoBehaviour
    {
        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugInfo = true;

        // Runtime
        private float _raceStartTime;
        private float _currentLapStartTime;
        private List<float> _lapTimes = new List<float>();
        private List<float> _currentLapCheckpointTimes = new List<float>(); // ← NOUVEAU: temps intermédiaires du tour actuel
        private List<List<float>> _allLapsCheckpointTimes = new List<List<float>>(); // ← NOUVEAU: tous les tours
        private bool _isRacing = false;

        #region Properties

        /// <summary>
        /// Temps total de course
        /// </summary>
        public float TotalRaceTime => _isRacing ? Time.time - _raceStartTime : 0f;

        /// <summary>
        /// Temps du tour actuel
        /// </summary>
        public float CurrentLapTime => _isRacing ? Time.time - _currentLapStartTime : 0f;

        /// <summary>
        /// Meilleur temps au tour
        /// </summary>
        public float BestLapTime
        {
            get
            {
                if (_lapTimes. Count == 0) return 0f;
                float best = float.MaxValue;
                foreach (float time in _lapTimes)
                {
                    if (time < best) best = time;
                }
                return best;
            }
        }

        /// <summary>
        /// Dernier temps au tour
        /// </summary>
        public float LastLapTime => _lapTimes.Count > 0 ? _lapTimes[_lapTimes.Count - 1] : 0f;

        /// <summary>
        /// Nombre de tours complétés
        /// </summary>
        public int CompletedLaps => _lapTimes.Count;

        /// <summary>
        /// Liste de tous les temps
        /// </summary>
        public List<float> LapTimes => new List<float>(_lapTimes);
        
        /// <summary>
        /// Temps intermédiaires du tour actuel
        /// </summary>
        public List<float> CurrentLapCheckpointTimes => new List<float>(_currentLapCheckpointTimes);
        
        /// <summary>
        /// Temps intermédiaires de tous les tours complétés
        /// </summary>
        public List<List<float>> AllLapsCheckpointTimes => new List<List<float>>(_allLapsCheckpointTimes);

        #endregion

        #region Race Control

        /// <summary>
        /// Démarrer la course
        /// </summary>
        public void StartRace()
        {
            _raceStartTime = Time.time;
            _currentLapStartTime = Time.time;
            _lapTimes.Clear();
            _currentLapCheckpointTimes.Clear();
            _allLapsCheckpointTimes.Clear();
            _isRacing = true;

            Debug.Log($"[LapTimer] {gameObject.name} - Race started!");
        }
        
        /// <summary>
        /// Enregistrer le passage d'un checkpoint intermédiaire
        /// </summary>
        public void RecordCheckpoint()
        {
            if (!_isRacing) return;
            
            float checkpointTime = Time.time - _currentLapStartTime;
            _currentLapCheckpointTimes.Add(checkpointTime);
            
            if (showDebugInfo)
            {
                Debug.Log($"[LapTimer] Checkpoint {_currentLapCheckpointTimes.Count}: {FormatTime(checkpointTime)}");
            }
        }

        /// <summary>
        /// Terminer un tour
        /// </summary>
        public void CompleteLap()
        {
            if (! _isRacing) return;

            float lapTime = Time.time - _currentLapStartTime;
            string lapTimeFormatted = FormatTime(lapTime);
            Debug.Log($"[LapTimer] {lapTime} seconds - completed in {lapTimeFormatted}");
            Debug.Log($" [LapTimer] {gameObject.name} - Lap {_lapTimes.Count} completed in {FormatTime(lapTime)}");
            _lapTimes.Add(lapTime);
            
            // Sauvegarder les temps intermédiaires de ce tour
            _allLapsCheckpointTimes.Add(new List<float>(_currentLapCheckpointTimes));


            // Démarrer le nouveau tour
            _currentLapStartTime = Time.time;
            _currentLapCheckpointTimes.Clear(); // Réinitialiser pour le prochain tour
        }

        /// <summary>
        /// Terminer la course
        /// </summary>
        public void FinishRace()
        {
            _isRacing = false;
            Debug.Log($"[LapTimer] {gameObject.name} - Race finished!  Total time: {FormatTime(TotalRaceTime)}");
        }

        /// <summary>
        /// Réinitialiser
        /// </summary>
        public void Reset()
        {
            _lapTimes.Clear();
            _currentLapCheckpointTimes.Clear();
            _allLapsCheckpointTimes.Clear();
            _isRacing = false;
            _raceStartTime = 0f;
            _currentLapStartTime = 0f;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Formater un temps en MM:SS. mmm
        /// </summary>
        public static string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000f) % 1000f);

            return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [System.Serializable]
        private class DebugInfo
        {
            public bool isRacing;
            public string currentLapTime;
            public string totalRaceTime;
            public string bestLapTime;
            public int completedLaps;
        }

        [SerializeField]
        private DebugInfo _debugInfo = new DebugInfo();

        private void Update()
        {
            if (! showDebugInfo || ! Application.isPlaying) return;

            _debugInfo.isRacing = _isRacing;
            _debugInfo.currentLapTime = FormatTime(CurrentLapTime);
            _debugInfo.totalRaceTime = FormatTime(TotalRaceTime);
            _debugInfo.bestLapTime = BestLapTime > 0 ? FormatTime(BestLapTime) : "--: --.  ---";
            _debugInfo.completedLaps = CompletedLaps;
        }
#endif

        #endregion
    }
}