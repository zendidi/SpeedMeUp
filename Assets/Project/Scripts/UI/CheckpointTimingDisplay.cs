using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcadeRacer.Core;
using ArcadeRacer.RaceSystem;
using System.Collections.Generic;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Affiche les temps intermédiaires aux checkpoints avec code couleur selon la performance
    /// Vert: Meilleur que le rank 1
    /// Bleu: Dans la moyenne des 9 autres (ranks 2-10)
    /// Rouge: Au-delà de la moyenne des 9 autres
    /// </summary>
    public class CheckpointTimingDisplay : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private TextMeshProUGUI[] checkpointTimeTexts;
        [SerializeField] private LapTimer lapTimer;
        
        [Header("=== COLORS ===")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color betterThanRank1Color = Color.green; // Meilleur que rank 1
        [SerializeField] private Color averageColor = Color.blue; // Dans la moyenne des 9 autres
        [SerializeField] private Color worseColor = Color.red; // Au-delà de la moyenne
        
        [Header("=== SETTINGS ===")]
        [SerializeField] private string circuitName = "";
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 0.1f;
        
        private float _lastUpdateTime;
        private List<float> _lastDisplayedTimes = new List<float>();
        
        // Cache des temps de référence pour comparaison
        private float[] _rank1CheckpointTimes;
        private float[] _averageCheckpointTimes;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (lapTimer == null)
            {
                lapTimer = FindFirstObjectByType<LapTimer>();
            }
            
            // Charger le circuit actuel si disponible
            if (string.IsNullOrEmpty(circuitName))
            {
                var circuitManager = ArcadeRacer.Managers.CircuitManager.Instance;
                if (circuitManager != null && circuitManager.CurrentCircuit != null)
                {
                    circuitName = circuitManager.CurrentCircuit.circuitName;
                }
            }
            
            // S'abonner à l'événement de chargement de circuit
            SubscribeToCircuitManager();
            
            // Charger les temps de référence depuis les highscores
            LoadReferenceTimesFromHighscores();
            
            ClearDisplay();
        }
        
        private void OnDestroy()
        {
            // Se désabonner des événements
            UnsubscribeFromCircuitManager();
        }
        
        private void Update()
        {
            if (!autoUpdate || lapTimer == null) return;
            
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                UpdateDisplay();
                _lastUpdateTime = Time.time;
            }
        }
        
        #endregion
        
        #region Circuit Manager Integration
        
        private void SubscribeToCircuitManager()
        {
            var circuitManager = ArcadeRacer.Managers.CircuitManager.Instance;
            if (circuitManager != null)
            {
                circuitManager.OnCircuitLoaded += OnCircuitLoadedHandler;
                Debug.Log("[CheckpointTimingDisplay] Subscribed to CircuitManager events.");
            }
        }
        
        private void UnsubscribeFromCircuitManager()
        {
            var circuitManager = ArcadeRacer.Managers.CircuitManager.Instance;
            if (circuitManager != null)
            {
                circuitManager.OnCircuitLoaded -= OnCircuitLoadedHandler;
            }
        }
        
        private void OnCircuitLoadedHandler(ArcadeRacer.Settings.CircuitData circuitData)
        {
            Debug.Log($"[CheckpointTimingDisplay] Circuit loaded: '{circuitData.circuitName}'. Reloading reference times...");
            SetCircuitName(circuitData.circuitName);
        }
        
        #endregion
        
        #region Highscore Reference Loading
        
        /// <summary>
        /// Charge les temps de référence depuis les highscores
        /// </summary>
        private void LoadReferenceTimesFromHighscores()
        {
            if (string.IsNullOrEmpty(circuitName))
            {
                Debug.LogWarning("[CheckpointTimingDisplay] Circuit name not set, cannot load reference times.");
                return;
            }
            
            // Charger le temps du rank 1
            var bestTime = HighscoreManager.Instance.GetBestTime(circuitName);
            if (bestTime.HasValue && bestTime.Value.checkpointTimes != null)
            {
                _rank1CheckpointTimes = bestTime.Value.checkpointTimes;
                Debug.Log($"[CheckpointTimingDisplay] Loaded rank 1 checkpoint times for {circuitName}: {_rank1CheckpointTimes.Length} checkpoints");
            }
            else
            {
                _rank1CheckpointTimes = null;
                Debug.Log($"[CheckpointTimingDisplay] No rank 1 checkpoint times found for {circuitName}");
            }
            
            // Charger les temps moyens des ranks 2-10
            _averageCheckpointTimes = HighscoreManager.Instance.GetAverageCheckpointTimes(circuitName);
            if (_averageCheckpointTimes != null)
            {
                Debug.Log($"[CheckpointTimingDisplay] Loaded average checkpoint times for {circuitName}: {_averageCheckpointTimes.Length} checkpoints");
            }
            else
            {
                Debug.Log($"[CheckpointTimingDisplay] No average checkpoint times found for {circuitName}");
            }
        }
        
        #endregion
        
        #region Display Update
        
        /// <summary>
        /// Met à jour l'affichage des temps intermédiaires
        /// </summary>
        public void UpdateDisplay()
        {
            if (lapTimer == null || checkpointTimeTexts == null) return;
            
            var currentCheckpointTimes = lapTimer.CurrentLapCheckpointTimes;
            
            for (int i = 0; i < checkpointTimeTexts.Length; i++)
            {
                if (checkpointTimeTexts[i] == null) continue;
                
                if (i < currentCheckpointTimes.Count)
                {
                    float checkpointTime = currentCheckpointTimes[i];
                    string formattedTime = LapTimer.FormatTime(checkpointTime);
                    
                    // Déterminer la couleur basée sur la comparaison avec les highscores
                    Color timeColor = GetComparisonColor(i, checkpointTime);
                    
                    checkpointTimeTexts[i].text = $"CP{i + 1}: {formattedTime}";
                    checkpointTimeTexts[i].color = timeColor;
                    checkpointTimeTexts[i].enabled = true;
                }
                else
                {
                    checkpointTimeTexts[i].text = $"CP{i + 1}: --:--.---";
                    checkpointTimeTexts[i].color = defaultColor;
                    checkpointTimeTexts[i].enabled = true;
                }
            }
            
            _lastDisplayedTimes = new List<float>(currentCheckpointTimes);
        }
        
        /// <summary>
        /// Détermine la couleur du temps basée sur la comparaison avec les highscores
        /// Vert: Meilleur que rank 1
        /// Bleu: Dans la moyenne des 9 autres
        /// Rouge: Au-delà de la moyenne
        /// </summary>
        private Color GetComparisonColor(int checkpointIndex, float checkpointTime)
        {
            // Si pas de temps de référence, utiliser la couleur par défaut
            if (_rank1CheckpointTimes == null || checkpointIndex >= _rank1CheckpointTimes.Length)
            {
                return defaultColor;
            }
            
            float rank1Time = _rank1CheckpointTimes[checkpointIndex];
            
            // Si meilleur que le rank 1: VERT
            if (checkpointTime < rank1Time)
            {
                return betterThanRank1Color;
            }
            
            // Si on a les temps moyens, comparer avec la moyenne
            if (_averageCheckpointTimes != null && checkpointIndex < _averageCheckpointTimes.Length)
            {
                float averageTime = _averageCheckpointTimes[checkpointIndex];
                
                // Si dans la moyenne ou meilleur: BLEU
                if (checkpointTime <= averageTime)
                {
                    return averageColor;
                }
                // Si au-delà de la moyenne: ROUGE
                else
                {
                    return worseColor;
                }
            }
            
            // Si pas de moyenne disponible, comparer juste avec rank 1
            // Si égal ou moins bon que rank 1 mais pas de moyenne: utiliser couleur moyenne
            return averageColor;
        }
        
        /// <summary>
        /// Efface l'affichage
        /// </summary>
        public void ClearDisplay()
        {
            if (checkpointTimeTexts == null) return;
            
            foreach (var text in checkpointTimeTexts)
            {
                if (text != null)
                {
                    text.text = "--:--.---";
                    text.color = defaultColor;
                    text.enabled = false;
                }
            }
            
            _lastDisplayedTimes.Clear();
        }
        
        /// <summary>
        /// Définir le nom du circuit pour la comparaison des highscores
        /// </summary>
        public void SetCircuitName(string name)
        {
            circuitName = name;
            LoadReferenceTimesFromHighscores(); // Recharger les temps de référence
            UpdateDisplay();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Force une mise à jour immédiate de l'affichage
        /// </summary>
        public void ForceUpdate()
        {
            UpdateDisplay();
        }
        
        /// <summary>
        /// Active/désactive l'affichage
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (checkpointTimeTexts == null) return;
            
            foreach (var text in checkpointTimeTexts)
            {
                if (text != null)
                {
                    text.enabled = visible;
                }
            }
        }
        
        #endregion
    }
}
