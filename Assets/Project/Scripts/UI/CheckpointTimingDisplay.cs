using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcadeRacer.Core;
using ArcadeRacer.RaceSystem;
using System.Collections.Generic;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Affiche le temps du dernier checkpoint passé avec code couleur selon la performance
    /// Bleu: Meilleur que le rank 1
    /// Vert: Entre rank 1 et rank 10 (dernier)
    /// Rouge: Au-delà du rank 10
    /// </summary>
    public class CheckpointTimingDisplay : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private TextMeshProUGUI checkpointTimeText; // Un seul champ pour le dernier checkpoint
        [SerializeField] private LapTimer lapTimer;
        
        [Header("=== COLORS ===")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color betterThanRank1Color = Color.blue; // Meilleur que rank 1
        [SerializeField] private Color betweenRanksColor = Color.green; // Entre rank 1 et rank 10
        [SerializeField] private Color worseColor = Color.red; // Au-delà du rank 10
        
        [Header("=== SETTINGS ===")]
        [SerializeField] private string circuitName = "";
        
        // Cache des temps de référence pour comparaison
        private float[] _rank1CheckpointTimes;
        private float[] _rank10CheckpointTimes;
        
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
            
            // Charger le temps du rank 1 (meilleur)
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
            
            // Charger le temps du rank 10 (dernier/pire)
            var worstTime = HighscoreManager.Instance.GetWorstTime(circuitName);
            if (worstTime.HasValue && worstTime.Value.checkpointTimes != null)
            {
                _rank10CheckpointTimes = worstTime.Value.checkpointTimes;
                Debug.Log($"[CheckpointTimingDisplay] Loaded rank 10 checkpoint times for {circuitName}: {_rank10CheckpointTimes.Length} checkpoints");
            }
            else
            {
                _rank10CheckpointTimes = null;
                Debug.Log($"[CheckpointTimingDisplay] No rank 10 checkpoint times found for {circuitName}");
            }
        }
        
        #endregion
        
        #region Display Update
        
        /// <summary>
        /// Appelé par LapTimer quand un checkpoint est enregistré
        /// Affiche le temps du dernier checkpoint passé avec la couleur appropriée
        /// </summary>
        public void OnCheckpointRecorded(int checkpointIndex, float checkpointTime)
        {
            Debug.Log($"[CheckpointTimingDisplay] Checkpoint {checkpointIndex + 1} recorded with time: {checkpointTime}");

            if (checkpointTimeText == null)
            {
                Debug.LogWarning("[CheckpointTimingDisplay] checkpointTimeText is null!");
                return;
            }
            Debug.Log($"[CheckpointTimingDisplay] Checkpoint {checkpointIndex + 1}");
            string formattedTime = LapTimer.FormatTime(checkpointTime);
            
            // Déterminer la couleur basée sur la comparaison avec les highscores
            Color timeColor = GetComparisonColor(checkpointIndex, checkpointTime);
            
            checkpointTimeText.text = $"CP{checkpointIndex + 1}: {formattedTime}";
            checkpointTimeText.color = timeColor;
            checkpointTimeText.enabled = true;
            
#if UNITY_EDITOR
            Debug.Log($"[CheckpointTimingDisplay] CP{checkpointIndex + 1}: {formattedTime} - Color: {timeColor}");
#endif
        }
        
        /// <summary>
        /// Détermine la couleur du temps basée sur la comparaison avec les highscores
        /// Bleu: Meilleur que rank 1
        /// Vert: Entre rank 1 et rank 10 (dernier)
        /// Rouge: Au-delà du rank 10
        /// </summary>
        private Color GetComparisonColor(int checkpointIndex, float checkpointTime)
        {
            // Si pas de temps de référence, utiliser la couleur par défaut
            if (_rank1CheckpointTimes == null || checkpointIndex >= _rank1CheckpointTimes.Length)
            {
                return defaultColor;
            }
            
            float rank1Time = _rank1CheckpointTimes[checkpointIndex];
            
            // Si meilleur que le rank 1: BLEU
            if (checkpointTime < rank1Time)
            {
                return betterThanRank1Color;
            }
            
            // Si on a les temps du rank 10, comparer
            if (_rank10CheckpointTimes != null && checkpointIndex < _rank10CheckpointTimes.Length)
            {
                float rank10Time = _rank10CheckpointTimes[checkpointIndex];
                
                // Si entre rank 1 et rank 10: VERT
                if (checkpointTime >= rank1Time && checkpointTime <= rank10Time)
                {
                    return betweenRanksColor;
                }
                // Si au-delà du rank 10: ROUGE
                else if (checkpointTime > rank10Time)
                {
                    return worseColor;
                }
            }
            
            // Par défaut, si pas de rank 10 disponible mais >= rank 1: couleur verte
            return betweenRanksColor;
        }
        
        /// <summary>
        /// Efface l'affichage
        /// </summary>
        public void ClearDisplay()
        {
            if (checkpointTimeText != null)
            {
                checkpointTimeText.text = "--:--.---";
                checkpointTimeText.color = defaultColor;
                checkpointTimeText.enabled = false;
            }
        }
        
        /// <summary>
        /// Définir le nom du circuit pour la comparaison des highscores
        /// </summary>
        public void SetCircuitName(string name)
        {
            circuitName = name;
            LoadReferenceTimesFromHighscores(); // Recharger les temps de référence
            ClearDisplay(); // Effacer l'affichage actuel
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Active/désactive l'affichage
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (checkpointTimeText != null)
            {
                checkpointTimeText.enabled = visible;
            }
        }
        
        #endregion
    }
}
