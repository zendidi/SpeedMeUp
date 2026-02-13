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
    /// Rouge: hors du top 10
    /// Bleu: top 4-10
    /// Vert: top 2-3  
    /// Mauve: RECORD (rang 1)
    /// </summary>
    public class CheckpointTimingDisplay : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private TextMeshProUGUI[] checkpointTimeTexts;
        [SerializeField] private LapTimer lapTimer;
        
        [Header("=== COLORS ===")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color outsideTop10Color = Color.red;
        [SerializeField] private Color top4to10Color = Color.blue;
        [SerializeField] private Color top2to3Color = Color.green;
        [SerializeField] private Color recordColor = new Color(0.8f, 0f, 0.8f); // Mauve
        
        [Header("=== SETTINGS ===")]
        [SerializeField] private string circuitName = "";
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 0.1f;
        
        private float _lastUpdateTime;
        private List<float> _lastDisplayedTimes = new List<float>();
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (lapTimer == null)
            {
                lapTimer = FindFirstObjectByType<LapTimer>();
            }
            
            ClearDisplay();
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
        
        #region Display Update
        
        /// <summary>
        /// Met à jour l'affichage des temps intermédiaires
        /// </summary>
        public void UpdateDisplay()
        {
            if (lapTimer == null || checkpointTimeTexts == null) return;
            
            var currentCheckpointTimes = lapTimer.CurrentLapCheckpointTimes;
            
            // Obtenir les highscores pour comparaison
            var highscores = HighscoreManager.Instance.GetHighscores(circuitName);
            
            for (int i = 0; i < checkpointTimeTexts.Length; i++)
            {
                if (checkpointTimeTexts[i] == null) continue;
                
                if (i < currentCheckpointTimes.Count)
                {
                    float checkpointTime = currentCheckpointTimes[i];
                    string formattedTime = LapTimer.FormatTime(checkpointTime);
                    
                    // Déterminer la couleur basée sur la performance
                    Color timeColor = GetTimeColor(i, checkpointTime, highscores);
                    
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
        /// </summary>
        private Color GetTimeColor(int checkpointIndex, float checkpointTime, List<HighscoreEntry> highscores)
        {
            if (highscores == null || highscores.Count == 0)
            {
                return defaultColor; // Pas de comparaison possible
            }
            
            // Compter combien de highscores ont un meilleur temps à ce checkpoint
            int betterScoresCount = 0;
            int totalComparableScores = 0;
            
            foreach (var score in highscores)
            {
                // Vérifier si ce highscore a des temps de checkpoints
                if (score.checkpointTimes == null || checkpointIndex >= score.checkpointTimes.Length)
                    continue;
                
                totalComparableScores++;
                
                if (score.checkpointTimes[checkpointIndex] < checkpointTime)
                {
                    betterScoresCount++;
                }
            }
            
            if (totalComparableScores == 0)
            {
                return defaultColor; // Pas de données de comparaison
            }
            
            // Déterminer le "rang" approximatif
            int approximateRank = betterScoresCount + 1;
            
            // Appliquer le code couleur
            if (approximateRank == 1)
            {
                return recordColor; // RECORD! Mauve
            }
            else if (approximateRank >= 2 && approximateRank <= 3)
            {
                return top2to3Color; // Top 2-3: Vert
            }
            else if (approximateRank >= 4 && approximateRank <= 10)
            {
                return top4to10Color; // Top 4-10: Bleu
            }
            else
            {
                return outsideTop10Color; // Hors top 10: Rouge
            }
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
