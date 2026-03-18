using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public string timeOfDayString; // ← Format: HH:mm:ss (heure locale au moment du tour)

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
                return !string.IsNullOrEmpty(dateString) ? dateString : System.DateTime.Now.ToString("dd/MM/yyyy");
            }
        }

        /// <summary>
        /// Retourne l'heure au format HH:mm:ss
        /// </summary>
        public string FormattedTimeOfDay
        {
            get
            {
                return !string.IsNullOrEmpty(timeOfDayString) ? timeOfDayString : "--:--:--";
            }
        }

        public HighscoreEntry(float time, string name, int position, float[] cpTimes = null, string date = null, string timeOfDay = null)
        {
            timeInSeconds = time;
            playerName = name;
            rank = position;
            checkpointTimes = cpTimes ?? new float[0];
            dateString = date ?? System.DateTime.Now.ToString("dd/MM/yyyy");
            timeOfDayString = timeOfDay ?? System.DateTime.Now.ToString("HH:mm:ss");
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
    /// Gestionnaire de highscores avec stockage local (PlayerPrefs) et
    /// synchronisation réseau optionnelle via Firebase Realtime Database REST API.
    ///
    /// Pour activer la synchronisation réseau (gratuite) :
    ///   1. Créer un projet sur https://console.firebase.google.com
    ///   2. Activer "Realtime Database" (mode test ou avec règles d'authentification)
    ///   3. Copier l'URL de la database (ex: https://mon-projet-default-rtdb.firebaseio.com)
    ///   4. Coller l'URL dans le champ "Firebase Database Url" de ce composant dans l'Inspector
    /// En laissant ce champ vide, le mode local (PlayerPrefs) reste actif.
    /// </summary>
    public class HighscoreManager : MonoBehaviour
    {
        private const int MAX_HIGHSCORES_PER_CIRCUIT = 10;
        private const string HIGHSCORE_KEY_PREFIX = "Highscore_";
        private const string HIGHSCORE_SEPARATOR = "|";
        private const float FLOAT_COMPARISON_EPSILON = 0.001f; // Tolérance pour comparaison de floats

        // In-memory cache populated by Firebase fetches.
        // When Firebase is enabled, this is the source of truth for GetHighscores().
        private readonly Dictionary<string, List<HighscoreEntry>> _networkCache =
            new Dictionary<string, List<HighscoreEntry>>();

        // Tracks circuits that have been successfully checked against Firebase
        // (including circuits with 0 scores or a 404 response).
        private readonly HashSet<string> _syncedFromNetwork = new HashSet<string>();

        #region Network Settings

        [Header("=== RÉSEAU (Firebase Realtime Database) ===")]
        [Tooltip("URL de la Firebase Realtime Database.\nEx: https://VOTRE-PROJET-default-rtdb.firebaseio.com\nLaisser vide pour mode local uniquement.")]
        [SerializeField] private string firebaseDatabaseUrl = "";

        [Tooltip("Synchroniser automatiquement les highscores depuis Firebase au démarrage.")]
        [SerializeField] private bool autoSyncOnStart = true;

        [Tooltip("Timeout des requêtes réseau en secondes.")]
        [SerializeField] private int networkTimeoutSeconds = 10;

        /// <summary>
        /// True si une URL Firebase est configurée.
        /// </summary>
        public bool IsNetworkEnabled => !string.IsNullOrEmpty(firebaseDatabaseUrl);

        /// <summary>
        /// Déclenché après le chargement réseau des highscores pour un circuit.
        /// Le paramètre est le nom du circuit mis à jour.
        /// </summary>
        public static event System.Action<string> OnNetworkHighscoresLoaded;

        #endregion

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

            if (autoSyncOnStart && IsNetworkEnabled)
            {
                StartCoroutine(SyncAllFromNetwork());
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Tente d'ajouter un score au classement.
        /// Retourne true si le score fait partie du top 10.
        /// </summary>
        public bool TryAddScore(string circuitName, float timeInSeconds, string playerName, float[] checkpointTimes = null, string timeOfDay = null)
        {
            if (string.IsNullOrEmpty(circuitName) || string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("[HighscoreManager] Circuit name ou player name invalide!");
                return false;
            }

            // Récupérer les scores actuels
            List<HighscoreEntry> scores = GetHighscores(circuitName);

            // Créer la nouvelle entrée
            HighscoreEntry newEntry = new HighscoreEntry(timeInSeconds, playerName, 0, checkpointTimes, null, timeOfDay);

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
        /// Récupère les highscores d'un circuit (max 10).
        /// Quand Firebase est activé et que le circuit a été synchronisé, retourne
        /// les données du cache réseau (source de vérité). Sinon, retourne PlayerPrefs.
        /// </summary>
        public List<HighscoreEntry> GetHighscores(string circuitName)
        {
            if (string.IsNullOrEmpty(circuitName))
                return new List<HighscoreEntry>();

            // Quand Firebase est activé et que ce circuit a été récupéré depuis Firebase,
            // utiliser le cache réseau comme source de vérité pour éviter les données
            // périmées de PlayerPrefs provenant d'une build précédente.
            if (IsNetworkEnabled && _syncedFromNetwork.Contains(circuitName))
            {
                return _networkCache.TryGetValue(circuitName, out List<HighscoreEntry> cached)
                    ? new List<HighscoreEntry>(cached)
                    : new List<HighscoreEntry>();
            }

            // Fallback: PlayerPrefs (mode hors-ligne ou avant la première synchro Firebase)
            List<HighscoreEntry> scores = new List<HighscoreEntry>();

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
        /// Récupère le dernier temps (rank 10) pour un circuit
        /// </summary>
        public HighscoreEntry? GetWorstTime(string circuitName)
        {
            List<HighscoreEntry> scores = GetHighscores(circuitName);
            
            if (scores.Count > 0)
                return scores[scores.Count - 1]; // Dernier = le plus lent
            
            return null;
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

            // Synchroniser vers Firebase si activé
            if (IsNetworkEnabled)
            {
                // Mise à jour optimiste du cache réseau : le nouveau score est immédiatement
                // visible même avant la confirmation Firebase.
                // En cas d'échec du push, le score reste dans PlayerPrefs.
                // Au prochain démarrage, la synchro Firebase rétablit Firebase comme source de vérité.
                _networkCache[circuitName] = new List<HighscoreEntry>(scores);
                _syncedFromNetwork.Add(circuitName);

                StartCoroutine(PushToNetwork(circuitName, scores));
            }
        }

        /// <summary>
        /// Formatte une entrée de highscore en string
        /// Format: "MM:SS:mmm|PlayerName|CP1,CP2,CP3...|dd/MM/yyyy|HH:mm:ss"
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
            result += $"{HIGHSCORE_SEPARATOR}{entry.dateString}";
            // Ajouter l'heure locale
            result += $"{HIGHSCORE_SEPARATOR}{entry.timeOfDayString}";
            
            return result;
        }

        /// <summary>
        /// Parse une string de highscore
        /// Format attendu: "MM:SS:mmm|PlayerName|CP1,CP2,CP3...|dd/MM/yyyy|HH:mm:ss"
        /// </summary>
        private HighscoreEntry ParseHighscoreData(string data, int rank)
        {
            if (string.IsNullOrEmpty(data))
                return new HighscoreEntry(0f, "", rank, null, null, null);

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
                
                // Parse time of day if present
                string timeOfDay = null;
                if (parts.Length >= 5 && !string.IsNullOrEmpty(parts[4]))
                {
                    timeOfDay = parts[4];
                }
                
                return new HighscoreEntry(time, playerName, rank, checkpointTimes, dateString, timeOfDay);
            }

            return new HighscoreEntry(0f, "", rank, null, null, null);
        }

        #endregion

        #region Network (Firebase Realtime Database)

        // ── JSON helpers ──────────────────────────────────────────────────────

        [System.Serializable]
        private class NetworkEntryData
        {
            public float timeInSeconds;
            public string playerName;
            public int rank;
            public string dateString;
            public string timeOfDayString;
        }

        [System.Serializable]
        private class NetworkHighscoreData
        {
            public List<NetworkEntryData> entries = new List<NetworkEntryData>();
        }

        private string SerializeEntries(List<HighscoreEntry> entries)
        {
            NetworkHighscoreData data = new NetworkHighscoreData();
            foreach (var e in entries)
            {
                data.entries.Add(new NetworkEntryData
                {
                    timeInSeconds = e.timeInSeconds,
                    playerName = e.playerName,
                    rank = e.rank,
                    dateString = e.dateString,
                    timeOfDayString = e.timeOfDayString
                });
            }
            return JsonUtility.ToJson(data);
        }

        private List<HighscoreEntry> DeserializeEntries(string json)
        {
            List<HighscoreEntry> result = new List<HighscoreEntry>();
            if (string.IsNullOrEmpty(json) || json == "null")
                return result;

            try
            {
                NetworkHighscoreData data = JsonUtility.FromJson<NetworkHighscoreData>(json);
                if (data?.entries != null)
                {
                    for (int i = 0; i < data.entries.Count; i++)
                    {
                        NetworkEntryData ed = data.entries[i];
                        result.Add(new HighscoreEntry(
                            ed.timeInSeconds,
                            ed.playerName,
                            ed.rank > 0 ? ed.rank : i + 1,
                            null,
                            ed.dateString,
                            ed.timeOfDayString
                        ));
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HighscoreManager] Erreur désérialisation JSON réseau: {ex.Message}");
            }

            return result.OrderBy(s => s.timeInSeconds).ToList();
        }

        /// <param name="withAuth">
        /// When true and FirebaseAuthManager is authenticated, appends the Firebase
        /// idToken as a query parameter (?auth=…) so that rules requiring
        /// "auth != null" are satisfied.
        /// </param>
        private string GetNetworkUrl(string circuitName, bool withAuth = false)
        {
            string clean = circuitName.Replace(" ", "_").Replace("-", "_")
                                      .Replace("/", "_").Replace(".", "_");
            string url = $"{firebaseDatabaseUrl.TrimEnd('/')}/highscores/{clean}.json";

            if (withAuth &&
                FirebaseAuthManager.Instance != null &&
                FirebaseAuthManager.Instance.IsAuthenticated)
            {
                url += $"?auth={FirebaseAuthManager.Instance.IdToken}";
            }

            return url;
        }

        // ── Fetch scores from Firebase for a single circuit ───────────────────

        private IEnumerator SyncFromNetwork(string circuitName)
        {
            string url = GetNetworkUrl(circuitName);

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = networkTimeoutSeconds;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    List<HighscoreEntry> networkScores = DeserializeEntries(req.downloadHandler.text);

                    // Mettre à jour le cache réseau et marquer le circuit comme synchronisé.
                    // Cela inclut le cas où Firebase retourne 0 scores (circuit vide) :
                    // GetHighscores() retournera une liste vide plutôt que les données
                    // périmées de PlayerPrefs.
                    _networkCache[circuitName] = networkScores;
                    _syncedFromNetwork.Add(circuitName);

                    if (networkScores.Count > 0)
                    {
                        MergeIntoLocal(circuitName, networkScores);
                        Debug.Log($"[HighscoreManager] Synchronisation réseau OK pour '{circuitName}': {networkScores.Count} scores.");
                    }
                    else
                    {
                        Debug.Log($"[HighscoreManager] Aucun score Firebase pour '{circuitName}'.");
                    }

                    OnNetworkHighscoresLoaded?.Invoke(circuitName);
                }
                else if (req.responseCode == 404)
                {
                    // Aucune donnée encore enregistrée pour ce circuit – c'est normal.
                    // On marque quand même comme synchronisé avec 0 scores pour éviter
                    // que les données PlayerPrefs périmées ne s'affichent.
                    _networkCache[circuitName] = new List<HighscoreEntry>();
                    _syncedFromNetwork.Add(circuitName);
                    Debug.Log($"[HighscoreManager] Aucun score réseau pour '{circuitName}' (404).");
                    OnNetworkHighscoresLoaded?.Invoke(circuitName);
                }
                else
                {
                    Debug.LogWarning($"[HighscoreManager] Échec synchronisation réseau pour '{circuitName}': {req.error}");
                }
            }
        }

        // ── Fetch scores for all known circuits ───────────────────────────────

        private IEnumerator SyncAllFromNetwork()
        {
            if (Settings.CircuitDatabase.Instance == null)
                yield break;

            foreach (var circuit in Settings.CircuitDatabase.Instance.AvailableCircuits)
            {
                if (circuit != null)
                    yield return SyncFromNetwork(circuit.circuitName);
            }
        }

        // ── Push local scores to Firebase ──────────────────────────────────────

        private IEnumerator PushToNetwork(string circuitName, List<HighscoreEntry> scores)
        {
            // Reads are public; writes require auth when rules enforce "auth != null".
            string url = GetNetworkUrl(circuitName, withAuth: true);
            string json = SerializeEntries(scores);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest req = new UnityWebRequest(url, "PUT"))
            {
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = networkTimeoutSeconds;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[HighscoreManager] Scores poussés vers Firebase pour '{circuitName}'.");
                }
                else
                {
                    Debug.LogWarning($"[HighscoreManager] Échec push Firebase pour '{circuitName}': {req.error}");
                }
            }
        }

        // ── Merge network scores into local cache (PlayerPrefs) ───────────────

        private void MergeIntoLocal(string circuitName, List<HighscoreEntry> networkScores)
        {
            List<HighscoreEntry> local = GetHighscores(circuitName);

            // Fusionner en dédupliquant sur (playerName, timeInSeconds)
            foreach (var ns in networkScores)
            {
                bool alreadyPresent = local.Any(l =>
                    l.playerName == ns.playerName &&
                    Mathf.Abs(l.timeInSeconds - ns.timeInSeconds) < FLOAT_COMPARISON_EPSILON);

                if (!alreadyPresent)
                    local.Add(ns);
            }

            List<HighscoreEntry> merged = local
                .OrderBy(s => s.timeInSeconds)
                .Take(MAX_HIGHSCORES_PER_CIRCUIT)
                .ToList();

            for (int i = 0; i < merged.Count; i++)
            {
                var e = merged[i];
                e.rank = i + 1;
                merged[i] = e;
            }

            // Sauvegarder localement uniquement (pas de push réseau ici pour éviter la boucle)
            for (int i = 0; i < MAX_HIGHSCORES_PER_CIRCUIT; i++)
                PlayerPrefs.DeleteKey(GetHighscoreKey(circuitName, i));

            for (int i = 0; i < merged.Count; i++)
                PlayerPrefs.SetString(GetHighscoreKey(circuitName, i), FormatHighscoreData(merged[i]));

            PlayerPrefs.Save();
        }

        // ── Public method for manual refresh from network ─────────────────────

        /// <summary>
        /// Force une synchronisation réseau pour un circuit donné.
        /// L'événement OnNetworkHighscoresLoaded est déclenché à la fin.
        /// </summary>
        public void RefreshFromNetwork(string circuitName)
        {
            if (!IsNetworkEnabled)
            {
                Debug.LogWarning("[HighscoreManager] Réseau désactivé (URL Firebase non configurée).");
                return;
            }
            if (string.IsNullOrEmpty(circuitName))
            {
                Debug.LogWarning("[HighscoreManager] RefreshFromNetwork: circuit name invalide.");
                return;
            }
            StartCoroutine(SyncFromNetwork(circuitName));
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
