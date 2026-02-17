using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ArcadeRacer.Core;
using ArcadeRacer.Settings;
using ArcadeRacer.Managers;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Contrôleur principal pour l'affichage des highscores.
    /// Gère la liste, le dropdown de sélection de circuit, et le rafraîchissement.
    /// </summary>
    public class HighscoreDisplayUI : MonoBehaviour
    {
        [Header("=== UI REFERENCES ===")]
        [SerializeField]
        [Tooltip("Conteneur parent pour la liste des highscores (avec VerticalLayoutGroup)")]
        private Transform highscoreListContainer;

        [SerializeField]
        [Tooltip("Dropdown TMP pour sélectionner le circuit")]
        private TMP_Dropdown circuitDropdown;

        [SerializeField]
        [Tooltip("Prefab de HighscoreItemUI")]
        private GameObject highscoreItemPrefab;

        [Header("=== SETTINGS ===")]
        [SerializeField]
        [Tooltip("Rafraîchir automatiquement au OnEnable")]
        private bool refreshOnEnable = true;

        [SerializeField]
        [Tooltip("Utiliser le circuit actuel par défaut")]
        private bool useCurrentCircuitAsDefault = true;

        // Runtime
        private List<HighscoreItemUI> _activeItems = new List<HighscoreItemUI>();
        private string _currentCircuitName;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (refreshOnEnable)
            {
                InitializeDropdown();
                RefreshDisplay();
            }
        }

        private void Start()
        {
            // Setup dropdown listener
            if (circuitDropdown != null)
            {
                circuitDropdown.onValueChanged.AddListener(OnCircuitDropdownChanged);
            }

            InitializeDropdown();
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            // Cleanup
            if (circuitDropdown != null)
            {
                circuitDropdown.onValueChanged.RemoveListener(OnCircuitDropdownChanged);
            }
        }

        #endregion

        #region Dropdown Management

        /// <summary>
        /// Initialise le dropdown avec tous les circuits disponibles.
        /// </summary>
        private void InitializeDropdown()
        {
            if (circuitDropdown == null)
            {
                Debug.LogWarning("[HighscoreDisplayUI] Circuit dropdown non assigné!");
                return;
            }

            if (CircuitDatabase.Instance == null)
            {
                Debug.LogError("[HighscoreDisplayUI] CircuitDatabase.Instance est null!");
                return;
            }

            // Effacer les options existantes
            circuitDropdown.ClearOptions();

            // Créer la liste des options
            List<string> circuitNames = new List<string>();
            foreach (var circuit in CircuitDatabase.Instance.AvailableCircuits)
            {
                if (circuit != null)
                {
                    circuitNames.Add(circuit.circuitName);
                }
            }

            if (circuitNames.Count == 0)
            {
                Debug.LogWarning("[HighscoreDisplayUI] Aucun circuit disponible dans CircuitDatabase!");
                circuitNames.Add("Aucun circuit");
            }

            // Ajouter les options au dropdown
            circuitDropdown.AddOptions(circuitNames);

            // Sélectionner le circuit actuel si option activée
            if (useCurrentCircuitAsDefault)
            {
                SetDropdownToCurrentCircuit();
            }
            else
            {
                // Sinon, sélectionner le premier circuit
                circuitDropdown.value = 0;
                _currentCircuitName = circuitNames[0];
            }

            Debug.Log($"[HighscoreDisplayUI] Dropdown initialisé avec {circuitNames.Count} circuits.");
        }

        /// <summary>
        /// Définit le dropdown sur le circuit actuellement chargé.
        /// </summary>
        private void SetDropdownToCurrentCircuit()
        {
            if (CircuitManager.Instance != null && CircuitManager.Instance.CurrentCircuit != null)
            {
                string currentCircuitName = CircuitManager.Instance.CurrentCircuit.circuitName;
                
                // Trouver l'index dans le dropdown
                for (int i = 0; i < circuitDropdown.options.Count; i++)
                {
                    if (circuitDropdown.options[i].text == currentCircuitName)
                    {
                        circuitDropdown.value = i;
                        _currentCircuitName = currentCircuitName;
                        Debug.Log($"[HighscoreDisplayUI] Dropdown défini sur le circuit actuel: {currentCircuitName}");
                        return;
                    }
                }

                Debug.LogWarning($"[HighscoreDisplayUI] Circuit actuel '{currentCircuitName}' non trouvé dans le dropdown!");
            }
            else
            {
                // Pas de circuit actuel, utiliser le premier
                if (circuitDropdown.options.Count > 0)
                {
                    circuitDropdown.value = 0;
                    _currentCircuitName = circuitDropdown.options[0].text;
                }
            }
        }

        /// <summary>
        /// Callback quand la sélection du dropdown change.
        /// </summary>
        private void OnCircuitDropdownChanged(int index)
        {
            if (circuitDropdown == null || index < 0 || index >= circuitDropdown.options.Count)
                return;

            string selectedCircuitName = circuitDropdown.options[index].text;
            
            if (selectedCircuitName != _currentCircuitName)
            {
                _currentCircuitName = selectedCircuitName;
                Debug.Log($"[HighscoreDisplayUI] Circuit sélectionné: {selectedCircuitName}");
                RefreshDisplay();
            }
        }

        #endregion

        #region Display Management

        /// <summary>
        /// Rafraîchit l'affichage des highscores pour le circuit sélectionné.
        /// </summary>
        public void RefreshDisplay()
        {
            if (string.IsNullOrEmpty(_currentCircuitName))
            {
                Debug.LogWarning("[HighscoreDisplayUI] Aucun circuit sélectionné pour l'affichage!");
                return;
            }

            Debug.Log($"[HighscoreDisplayUI] Rafraîchissement de l'affichage pour: {_currentCircuitName}");

            // Récupérer les highscores
            List<HighscoreEntry> highscores = HighscoreManager.Instance.GetHighscores(_currentCircuitName);

            // Nettoyer les items existants
            ClearDisplayedItems();

            // Créer les nouveaux items
            if (highscores.Count == 0)
            {
                Debug.Log($"[HighscoreDisplayUI] Aucun highscore pour {_currentCircuitName}");
                // Optionnel: afficher un message "Aucun score"
                return;
            }

            // Afficher chaque highscore
            for (int i = 0; i < highscores.Count; i++)
            {
                CreateHighscoreItem(highscores[i]);
            }

            Debug.Log($"[HighscoreDisplayUI] {highscores.Count} highscores affichés pour {_currentCircuitName}");
        }

        /// <summary>
        /// Crée un item de highscore dans la liste.
        /// </summary>
        private void CreateHighscoreItem(HighscoreEntry entry)
        {
            if (highscoreItemPrefab == null)
            {
                Debug.LogError("[HighscoreDisplayUI] HighscoreItemPrefab non assigné!");
                return;
            }

            if (highscoreListContainer == null)
            {
                Debug.LogError("[HighscoreDisplayUI] HighscoreListContainer non assigné!");
                return;
            }

            // Instancier le prefab
            GameObject itemObj = Instantiate(highscoreItemPrefab, highscoreListContainer);
            HighscoreItemUI itemUI = itemObj.GetComponent<HighscoreItemUI>();

            if (itemUI != null)
            {
                // Parser la date si elle existe, sinon utiliser la date actuelle
                System.DateTime date = System.DateTime.Now;
                if (!string.IsNullOrEmpty(entry.dateString))
                {
                    System.DateTime.TryParseExact(entry.dateString, "dd/MM/yyyy", 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, 
                        out date);
                }
                
                itemUI.Setup(entry, date);
                _activeItems.Add(itemUI);
            }
            else
            {
                Debug.LogError("[HighscoreDisplayUI] Le prefab n'a pas de composant HighscoreItemUI!");
                Destroy(itemObj);
            }
        }

        /// <summary>
        /// Nettoie tous les items affichés.
        /// </summary>
        private void ClearDisplayedItems()
        {
            foreach (var item in _activeItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _activeItems.Clear();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Définit manuellement le circuit à afficher.
        /// </summary>
        public void SetCircuit(string circuitName)
        {
            if (string.IsNullOrEmpty(circuitName))
                return;

            // Mettre à jour le dropdown si possible
            if (circuitDropdown != null)
            {
                for (int i = 0; i < circuitDropdown.options.Count; i++)
                {
                    if (circuitDropdown.options[i].text == circuitName)
                    {
                        circuitDropdown.value = i;
                        return; // OnCircuitDropdownChanged va appeler RefreshDisplay
                    }
                }
            }

            // Si le dropdown n'existe pas ou le circuit n'est pas trouvé, rafraîchir directement
            _currentCircuitName = circuitName;
            RefreshDisplay();
        }

        /// <summary>
        /// Rafraîchit l'affichage (méthode publique pour appels externes).
        /// </summary>
        public void Refresh()
        {
            RefreshDisplay();
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [ContextMenu("Force Refresh Display")]
        private void ForceRefresh()
        {
            RefreshDisplay();
        }

        [ContextMenu("Test with Dummy Data")]
        private void TestWithDummyData()
        {
            // Créer des données de test
            ClearDisplayedItems();

            for (int i = 0; i < 10; i++)
            {
                HighscoreEntry dummyEntry = new HighscoreEntry(
                    60f + i * 5f, // Temps croissant
                    $"Player{i + 1}",
                    i + 1,
                    null
                );

                CreateHighscoreItem(dummyEntry);
            }

            Debug.Log("[HighscoreDisplayUI] Données de test créées!");
        }
#endif

        #endregion
    }
}
