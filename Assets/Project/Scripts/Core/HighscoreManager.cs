using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ArcadeRacer.Core
{
    /// <summary>
    /// Entrée de highscore avec temps et nom du joueur
    /// </summary>
    [System.Serializable]
    public struct HighscoreEntry
    {
        public float timeInSeconds;
        public string playerName;
        public int rank;
        public float[] checkpointTimes; // ← NOUVEAU: temps intermédiaires aux checkpoints
        public string dateString; // ← Format: dd/MM/yyyy

        /// <summary>
        /// Formatte le temps en MM:SS:mmm
        /// </summary>
        public string FormattedTime => FormatTime(timeInSeconds);

        /// <summary>
        /// Retourne la date au format dd/MM/yyyy
        /// </summary>
        public string FormattedDate
        {
            get
            {
                if (!string.IsNullOrEmpty(dateString))
                    return dateString;
                return System.DateTime.Now.ToString("dd/MM/yyyy");
            }
        }

        public HighscoreEntry(float time, string name, int position, float[] cpTimes = null, string date = null)
        {
            timeInSeconds = time;
            playerName = name;
            rank = position;
            checkpointTimes = cpTimes ?? new float[0];
            dateString = date ?? System.DateTime.Now.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Formater un temps en MM:SS:mmm (minutes:secondes:millièmes)
        /// </summary>
        public static string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000f) % 1000f);

            return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
        }

        /// <summary>
        /// Parse un temps formaté MM:SS:mmm vers float
        /// </summary>
        public static float ParseTime(string formattedTime)
        {
            if (string.IsNullOrEmpty(formattedTime))
                return 0f;

            string[] parts = formattedTime.Split(':');
            if (parts.Length != 3)
                return 0f;

            if (int.TryParse(parts[0], out int minutes) &&
                int.TryParse(parts[1], out int seconds) &&
                int.TryParse(parts[2], out int milliseconds))
            {
                return minutes * 60f + seconds + milliseconds / 1000f;
            }

            return 0f;
        }
    }

    /// <summary>
    /// Gestionnaire de highscores local (PlayerPrefs).
    /// Gère le top 10 des meilleurs temps par circuit avec noms des joueurs.
    /// </summary>
    public class HighscoreManager : MonoBehaviour
    {
        private const int MAX_HIGHSCORES_PER_CIRCUIT = 10;
        private const string HIGHSCORE_KEY_PREFIX = "Highscore_";
        private const string HIGHSCORE_SEPARATOR = "|";
        private const float FLOAT_COMPARISON_EPSILON = 0.001f; // Tolérance pour comparaison de floats

        #region Singleton

        private static HighscoreManager _instance;

        public static HighscoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HighscoreManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("HighscoreManager");
                        _instance = go.AddComponent<HighscoreManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Tente d'ajouter un score au classement.
        /// Retourne true si le score fait partie du top 10.
        /// </summary>
        public bool TryAddScore(string circuitName, float timeInSeconds, string playerName, float[] checkpointTimes = null)
        {
            if (string.IsNullOrEmpty(circuitName) || string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("[HighscoreManager] Circuit name ou player name invalide!");
                return false;
            }

            // Récupérer les scores actuels
            List<HighscoreEntry> scores = GetHighscores(circuitName);

            // Créer la nouvelle entrée
            HighscoreEntry newEntry = new HighscoreEntry(timeInSeconds, playerName, 0, checkpointTimes);

            // Ajouter et trier
            scores.Add(newEntry);
            scores = scores.OrderBy(s => s.timeInSeconds).Take(MAX_HIGHSCORES_PER_CIRCUIT).ToList();

            // Vérifier si le nouveau score est dans le top 10
            // Utiliser une comparaison epsilon pour éviter les problèmes de précision float
            bool isTopScore = scores.Any(s => Mathf.Abs(s.timeInSeconds - timeInSeconds) < FLOAT_COMPARISON_EPSILON && s.playerName == playerName);

            if (isTopScore)
            {
                // Mettre à jour les rangs
                for (int i = 0; i < scores.Count; i++)
                {
                    var entry = scores[i];
                    entry.rank = i + 1;
                    scores[i] = entry;
                }

                // Sauvegarder
                SaveHighscores(circuitName, scores);

                // Trouver le rang avec comparaison epsilon
                int entryIndex = scores.FindIndex(s => Mathf.Abs(s.timeInSeconds - timeInSeconds) < FLOAT_COMPARISON_EPSILON && s.playerName == playerName);
                int entryRank = entryIndex >= 0 ? entryIndex + 1 : 0;

                Debug.Log($"[HighscoreManager] Nouveau highscore pour {circuitName}: {newEntry.FormattedTime} - {playerName} (Rang: {entryRank})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Récupère les highscores d'un circuit (max 10)
        /// </summary>
        public List<HighscoreEntry> GetHighscores(string circuitName)
        {
            List<HighscoreEntry> scores = new List<HighscoreEntry>();

            if (string.IsNullOrEmpty(circuitName))
                return scores;

            // Charger depuis PlayerPrefs
            for (int i = 0; i < MAX_HIGHSCORES_PER_CIRCUIT; i++)
            {
                string key = GetHighscoreKey(circuitName, i);
                if (PlayerPrefs.HasKey(key))
                {
                    string data = PlayerPrefs.GetString(key);
                    HighscoreEntry entry = ParseHighscoreData(data, i + 1);
                    
                    if (entry.timeInSeconds > 0)
                    {
                        scores.Add(entry);
                    }
                }
            }

            return scores.OrderBy(s => s.timeInSeconds).ToList();
        }

        /// <summary>
        /// Efface tous les highscores d'un circuit
        /// </summary>
        public void ClearHighscores(string circuitName)
        {
            if (string.IsNullOrEmpty(circuitName))
                return;

            for (int i = 0; i < MAX_HIGHSCORES_PER_CIRCUIT; i++)
            {
                string key = GetHighscoreKey(circuitName, i);
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }

            PlayerPrefs.Save();
            Debug.Log($"[HighscoreManager] Highscores effacés pour {circuitName}");
        }

        /// <summary>
        /// Efface TOUS les highscores de tous les circuits
        /// </summary>
        public void ClearAllHighscores()
        {
            // On ne peut pas itérer sur toutes les clés PlayerPrefs facilement
            // Donc on va effacer pour tous les circuits connus
            if (Settings.CircuitDatabase.Instance != null)
            {
                foreach (var circuit in Settings.CircuitDatabase.Instance.AvailableCircuits)
                {
                    if (circuit != null)
                    {
                        ClearHighscores(circuit.circuitName);
                    }
                }
            }

            Debug.Log("[HighscoreManager] Tous les highscores ont été effacés");
        }

        /// <summary>
        /// Vérifie si un temps ferait partie du top 10 (sans l'ajouter)
        /// </summary>
        public bool WouldBeTopScore(string circuitName, float timeInSeconds)
        {
            List<HighscoreEntry> scores = GetHighscores(circuitName);

            // Si moins de 10 scores, c'est automatiquement un top score
            if (scores.Count < MAX_HIGHSCORES_PER_CIRCUIT)
                return true;

            // Sinon, vérifier si le temps est meilleur que le 10ème
            return timeInSeconds < scores[scores.Count - 1].timeInSeconds;
        }

        /// <summary>
        /// Récupère le meilleur temps pour un circuit
        /// </summary>
        public HighscoreEntry? GetBestTime(string circuitName)
        {
            List<HighscoreEntry> scores = GetHighscores(circuitName);
            
            if (scores.Count > 0)
                return scores[0];
            
            return null;
        }
        
        /// <summary>
        /// Récupère les temps de checkpoint moyens du top 10
        /// Pour chaque checkpoint (index i), calcule la moyenne des temps de toutes les entrées à cet index
        /// Utilisé pour comparer la performance du joueur: si meilleur que rank 1 → vert,
        /// si dans la moyenne → bleu, si au-delà de la moyenne → rouge
        /// </summary>
        public float[] GetAverageCheckpointTimes(string circuitName)
        {
            List<HighscoreEntry> scores = GetHighscores(circuitName);
            
            if (scores.Count == 0)
                return null; // Pas de données pour calculer une moyenne
            
            // Trouver le nombre maximum de checkpoints
            int maxCheckpoints = 0;
            foreach (var score in scores)
            {
                if (score.checkpointTimes != null && score.checkpointTimes.Length > maxCheckpoints)
                {
                    maxCheckpoints = score.checkpointTimes.Length;
                }
            }
            
            if (maxCheckpoints == 0)
                return null;
            
            // Calculer les moyennes pour chaque checkpoint (index i) à travers toutes les entrées
            float[] averages = new float[maxCheckpoints];
            for (int i = 0; i < maxCheckpoints; i++)
            {
                float sum = 0f;
                int count = 0;
                
                // Moyenner le temps au checkpoint i pour toutes les entrées qui ont ce checkpoint
                foreach (var score in scores)
                {
                    if (score.checkpointTimes != null && i < score.checkpointTimes.Length)
                    {
                        sum += score.checkpointTimes[i];
                        count++;
                    }
                }
                
                averages[i] = count > 0 ? sum / count : 0f;
            }
            
            return averages;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Génère une clé PlayerPrefs pour un circuit et un index
        /// </summary>
        private string GetHighscoreKey(string circuitName, int index)
        {
            // Nettoyer le nom du circuit (enlever espaces et caractères spéciaux)
            string cleanName = circuitName.Replace(" ", "_").Replace("-", "_");
            return $"{HIGHSCORE_KEY_PREFIX}{cleanName}_{index}";
        }

        /// <summary>
        /// Sauvegarde les highscores dans PlayerPrefs
        /// Format: "MM:SS:mmm|PlayerName"
        /// </summary>
        private void SaveHighscores(string circuitName, List<HighscoreEntry> scores)
        {
            // Effacer les anciennes entrées
            for (int i = 0; i < MAX_HIGHSCORES_PER_CIRCUIT; i++)
            {
                string key = GetHighscoreKey(circuitName, i);
                PlayerPrefs.DeleteKey(key);
            }

            // Sauvegarder les nouvelles
            for (int i = 0; i < scores.Count && i < MAX_HIGHSCORES_PER_CIRCUIT; i++)
            {
                string key = GetHighscoreKey(circuitName, i);
                string data = FormatHighscoreData(scores[i]);
                PlayerPrefs.SetString(key, data);
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Formatte une entrée de highscore en string
        /// Format: "MM:SS:mmm|PlayerName|CP1,CP2,CP3...|dd/MM/yyyy"
        /// </summary>
        private string FormatHighscoreData(HighscoreEntry entry)
        {
            string result = $"{entry.FormattedTime}{HIGHSCORE_SEPARATOR}{entry.playerName}";
            
            // Ajouter les temps de checkpoints si présents
            if (entry.checkpointTimes != null && entry.checkpointTimes.Length > 0)
            {
                string cpTimes = string.Join(",", System.Array.ConvertAll(entry.checkpointTimes, t => t.ToString("F3")));
                result += $"{HIGHSCORE_SEPARATOR}{cpTimes}";
            }
            else
            {
                result += $"{HIGHSCORE_SEPARATOR}"; // Séparateur vide pour les checkpoints
            }
            
            // Ajouter la date
            result += $"{HIGHSCORE_SEPARATOR}{entry.FormattedDate}";
            
            return result;
        }

        /// <summary>
        /// Parse une string de highscore
        /// Format attendu: "MM:SS:mmm|PlayerName|CP1,CP2,CP3...|dd/MM/yyyy"
        /// </summary>
        private HighscoreEntry ParseHighscoreData(string data, int rank)
        {
            if (string.IsNullOrEmpty(data))
                return new HighscoreEntry(0f, "", rank, null, null);

            string[] parts = data.Split(new[] { HIGHSCORE_SEPARATOR }, System.StringSplitOptions.None);
            
            if (parts.Length >= 2)
            {
                float time = HighscoreEntry.ParseTime(parts[0]);
                string playerName = parts[1];
                
                // Parse checkpoint times if present
                float[] checkpointTimes = null;
                if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                {
                    string[] cpTimesStr = parts[2].Split(',');
                    checkpointTimes = new float[cpTimesStr.Length];
                    for (int i = 0; i < cpTimesStr.Length; i++)
                    {
                        float.TryParse(cpTimesStr[i], out checkpointTimes[i]);
                    }
                }
                
                // Parse date if present
                string dateString = null;
                if (parts.Length >= 4 && !string.IsNullOrEmpty(parts[3]))
                {
                    dateString = parts[3];
                }
                
                return new HighscoreEntry(time, playerName, rank, checkpointTimes, dateString);
            }

            return new HighscoreEntry(0f, "", rank, null, null);
        }

        #endregion

        #region Debug

        /// <summary>
        /// Affiche tous les highscores dans la console (debug)
        /// </summary>
        [ContextMenu("Debug: Display All Highscores")]
        public void DebugDisplayAllHighscores()
        {
            if (Settings.CircuitDatabase.Instance == null)
            {
                Debug.Log("[HighscoreManager] CircuitDatabase non trouvée!");
                return;
            }

            Debug.Log("========== HIGHSCORES ==========");
            
            foreach (var circuit in Settings.CircuitDatabase.Instance.AvailableCircuits)
            {
                if (circuit == null) continue;

                List<HighscoreEntry> scores = GetHighscores(circuit.circuitName);
                
                if (scores.Count > 0)
                {
                    Debug.Log($"\n--- {circuit.circuitName} ---");
                    for (int i = 0; i < scores.Count; i++)
                    {
                        Debug.Log($"{i + 1}. {scores[i].FormattedTime} - {scores[i].playerName}");
                    }
                }
            }
            
            Debug.Log("================================");
        }

        #endregion
    }
}
