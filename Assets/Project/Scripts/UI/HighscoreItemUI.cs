using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArcadeRacer.Core;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Représente une ligne dans la liste des highscores.
    /// Affiche: Rang, Nom du joueur, Temps, Date.
    /// </summary>
    public class HighscoreItemUI : MonoBehaviour
    {
        [Header("=== UI REFERENCES ===")]
        [SerializeField]
        [Tooltip("RawImage pour le code couleur de fond")]
        private RawImage backgroundImage;

        [SerializeField]
        [Tooltip("TextMeshPro pour afficher toutes les informations")]
        private TextMeshProUGUI infoText;

        [Header("=== COLOR CODING ===")]
        [Tooltip("Couleur pour le rang 1 (RECORD - Mauve/Purple)")]
        [SerializeField] private Color recordColor = new Color(0.7f, 0.3f, 1f, 0.117f); // Purple avec alpha 30/255

        [Tooltip("Couleur pour les rangs 2-3 (Vert)")]
        [SerializeField] private Color topColor = new Color(0.3f, 1f, 0.3f, 0.117f); // Green avec alpha 30/255

        [Tooltip("Couleur pour les rangs 4-10 (Bleu)")]
        [SerializeField] private Color midColor = new Color(0.3f, 0.5f, 1f, 0.117f); // Blue avec alpha 30/255

        [Tooltip("Couleur pour hors top 10 (Rouge)")]
        [SerializeField] private Color lowColor = new Color(1f, 0.3f, 0.3f, 0.117f); // Red avec alpha 30/255

        /// <summary>
        /// Configure l'item avec les données d'une entrée de highscore.
        /// </summary>
        /// <param name="entry">L'entrée de highscore à afficher</param>
        /// <param name="date">Date du record (optionnel, format DateTime)</param>
        public void Setup(HighscoreEntry entry, System.DateTime? date = null)
        {
            // Formatage du texte: Rang | Nom | Temps | Date
            string dateString = date.HasValue ? date.Value.ToString("dd/MM/yyyy") : System.DateTime.Now.ToString("dd/MM/yyyy");
            
            string displayText = $"#{entry.rank}  |  {entry.playerName}  |  {entry.FormattedTime}  |  {dateString}";
            
            if (infoText != null)
            {
                infoText.text = displayText;
            }

            // Appliquer le code couleur selon le rang
            if (backgroundImage != null)
            {
                Color backgroundColor = GetColorForRank(entry.rank);
                backgroundImage.color = backgroundColor;
            }
        }

        /// <summary>
        /// Détermine la couleur de fond selon le rang.
        /// </summary>
        private Color GetColorForRank(int rank)
        {
            if (rank == 1)
            {
                // Rang 1: RECORD (Mauve/Purple)
                return recordColor;
            }
            else if (rank >= 2 && rank <= 3)
            {
                // Rangs 2-3: Vert
                return topColor;
            }
            else if (rank >= 4 && rank <= 10)
            {
                // Rangs 4-10: Bleu
                return midColor;
            }
            else
            {
                // Hors top 10: Rouge
                return lowColor;
            }
        }

        /// <summary>
        /// Réinitialise l'item (pour object pooling si nécessaire).
        /// </summary>
        public void Clear()
        {
            if (infoText != null)
            {
                infoText.text = "";
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = Color.clear;
            }
        }

        #region Unity Editor
        
#if UNITY_EDITOR
        // Permet de tester visuellement les couleurs dans l'Inspector
        [ContextMenu("Test Rank 1 Color")]
        private void TestRank1()
        {
            if (backgroundImage != null)
                backgroundImage.color = recordColor;
        }

        [ContextMenu("Test Rank 2-3 Color")]
        private void TestRank2()
        {
            if (backgroundImage != null)
                backgroundImage.color = topColor;
        }

        [ContextMenu("Test Rank 4-10 Color")]
        private void TestRank4()
        {
            if (backgroundImage != null)
                backgroundImage.color = midColor;
        }

        [ContextMenu("Test Out of Top 10 Color")]
        private void TestOutOfTop()
        {
            if (backgroundImage != null)
                backgroundImage.color = lowColor;
        }
#endif

        #endregion
    }
}
