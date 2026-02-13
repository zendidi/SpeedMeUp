using ArcadeRacer.Core;
using ArcadeRacer.Managers;
using ArcadeRacer.RaceSystem;
using ArcadeRacer.Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Gestionnaire de l'UI de sélection de circuits.
    /// Génère automatiquement les items de sélection dans un GridLayoutGroup.
    /// </summary>
    public class CircuitSelectionUI : MonoBehaviour
    {
        [Header("=== CONFIGURATION ===")]
        [SerializeField]
        [Tooltip("Utiliser CircuitDatabase comme source (recommandé)")]
        private bool useCircuitDatabase = true;

        [SerializeField]
        [Tooltip("Liste manuelle de circuits (si useCircuitDatabase = false)")]
        private List<CircuitData> manualCircuitList = new List<CircuitData>();

        [Header("=== UI REFERENCES ===")]
        [SerializeField]
        [Tooltip("Container avec GridLayoutGroup pour les items")]
        private Transform gridContainer;

        [SerializeField]
        [Tooltip("Prefab de l'item de sélection")]
        private GameObject itemPrefab;

        [Header("=== EVENTS ===")]
        public CircuitSelectedEvent OnCircuitSelected;

        // Runtime
        private List<CircuitSelectionItem> _spawnedItems = new List<CircuitSelectionItem>();
        private CircuitData _selectedCircuit;
        private CircuitSelectionItem _selectedItem;
        [SerializeField] private RaceManager raceManager;

        #region Unity Lifecycle

        private void Start()
        {
            GenerateCircuitItems();
            // Écouter la sélection de circuit

            OnCircuitSelected.AddListener(LoadAndStartCircuit);


        }

        private void OnDestroy()
        {
            ClearItems();
            OnCircuitSelected.RemoveListener(LoadAndStartCircuit);

        }

        private void OnValidate()
        {
            // Vérifier que le container a un GridLayoutGroup
            if (gridContainer != null)
            {
                GridLayoutGroup grid = gridContainer.GetComponent<GridLayoutGroup>();
                if (grid == null)
                {
                    Debug.LogWarning("[CircuitSelectionUI] Le gridContainer devrait avoir un GridLayoutGroup!");
                }
            }
        }

        #endregion

        #region Generation

        /// <summary>
        /// Génère les items de sélection de circuits
        /// </summary>
        public void GenerateCircuitItems()
        {
            // Nettoyer les items existants
            ClearItems();

            if (gridContainer == null)
            {
                Debug.LogError("[CircuitSelectionUI] GridContainer non assigné!");
                return;
            }

            if (itemPrefab == null)
            {
                Debug.LogError("[CircuitSelectionUI] ItemPrefab non assigné!");
                return;
            }

            // Récupérer la liste de circuits
            List<CircuitData> circuits = GetCircuitList();

            if (circuits == null || circuits.Count == 0)
            {
                Debug.LogWarning("[CircuitSelectionUI] Aucun circuit disponible!");
                return;
            }

            // Générer un item pour chaque circuit
            foreach (CircuitData circuit in circuits)
            {
                if (circuit == null) continue;

                GameObject itemGO = Instantiate(itemPrefab, gridContainer);
                CircuitSelectionItem item = itemGO.GetComponent<CircuitSelectionItem>();

                if (item != null)
                {
                    item.Setup(circuit, OnItemSelected);
                    _spawnedItems.Add(item);
                }
                else
                {
                    Debug.LogWarning("[CircuitSelectionUI] Le prefab n'a pas de composant CircuitSelectionItem!");
                    Destroy(itemGO);
                }
            }

            Debug.Log($"[CircuitSelectionUI] {_spawnedItems.Count} items de circuit générés");
        }

        /// <summary>
        /// Nettoie tous les items générés
        /// </summary>
        public void ClearItems()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            List<GameObject> th_track = new List<GameObject>();
            for (int i = 0; i < gridContainer.transform.childCount; i++)
                DestroyImmediate(gridContainer.GetChild(i).gameObject);


            _spawnedItems.Clear();
            _selectedItem = null;
            _selectedCircuit = null;
        }

        /// <summary>
        /// Recharge les items (utile après modification de la database)
        /// </summary>
        [ContextMenu("Reload Circuit Items")]
        public void ReloadItems()
        {
            ClearItems();
            GenerateCircuitItems();
        }

        #endregion

        #region Circuit List

        /// <summary>
        /// Récupère la liste de circuits à afficher
        /// </summary>
        private List<CircuitData> GetCircuitList()
        {
            if (useCircuitDatabase)
            {
                if (CircuitDatabase.Instance != null)
                {
                    return new List<CircuitData>(CircuitDatabase.Instance.AvailableCircuits);
                }
                else
                {
                    Debug.LogWarning("[CircuitSelectionUI] CircuitDatabase non trouvée! Utilisation de la liste manuelle.");
                    return manualCircuitList;
                }
            }
            else
            {
                return manualCircuitList;
            }
        }

        #endregion

        #region Selection

        /// <summary>
        /// Appelé quand un item est cliqué
        /// </summary>
        private void OnItemSelected(CircuitData circuit)
        {
            if (circuit == null) return;

            // Déselectionner l'item précédent
            if (_selectedItem != null)
            {
                _selectedItem.SetSelected(false);
            }

            // Sélectionner le nouveau
            _selectedCircuit = circuit;
            _selectedItem = _spawnedItems.Find(item => item.CircuitData == circuit);

            if (_selectedItem != null)
            {
                _selectedItem.SetSelected(true);
            }

            // Déclencher l'événement
            OnCircuitSelected?.Invoke(circuit);

        }

        private void LoadAndStartCircuit(CircuitData circuit)
        {
            CircuitManager.Instance.LoadCircuit(circuit);
            DisplayBestTime(circuit.circuitName);
            
            // Spawn the vehicle at the circuit's spawn point
            var vehicleController = FindFirstObjectByType<ArcadeRacer.Vehicle.VehicleController>();
            if (vehicleController != null)
            {
                CircuitManager.Instance.SpawnVehicle(vehicleController.transform);
                
                // Reset vehicle velocity
                Rigidbody vehicleRb = vehicleController.GetComponent<Rigidbody>();
                if (vehicleRb != null)
                {
                    vehicleRb.linearVelocity = Vector3.zero;
                    vehicleRb.angularVelocity = Vector3.zero;
                }
                
                Debug.Log($"[CircuitSelectionUI] Vehicle spawned at circuit spawn point");
            }
            else
            {
                Debug.LogWarning("[CircuitSelectionUI] No VehicleController found in scene!");
            }

            if (raceManager != null)
            {
                raceManager.RestartRace();
            }
            
            // Auto-hide the circuit selection panel after selecting a circuit
            Hide();
            Debug.Log("[CircuitSelectionUI] Circuit selection panel hidden after selection");
        }

        private void DisplayBestTime(string circuitName)
        {
            HighscoreEntry? bestTime = HighscoreManager.Instance.GetBestTime(circuitName);

            if (bestTime.HasValue)
            {
                Debug.Log($"[CircuitSystemExample] Record: {bestTime.Value.FormattedTime} par {bestTime.Value.playerName}");
            }
        }
        /// <summary>
        /// Sélectionne un circuit par programmation
        /// </summary>
        public void SelectCircuit(CircuitData circuit)
        {
            OnItemSelected(circuit);
        }

        /// <summary>
        /// Sélectionne un circuit par son index
        /// </summary>
        public void SelectCircuitByIndex(int index)
        {
            if (index >= 0 && index < _spawnedItems.Count)
            {
                CircuitData circuit = _spawnedItems[index].CircuitData;
                SelectCircuit(circuit);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Affiche l'UI de sélection
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Cache l'UI de sélection
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Circuit actuellement sélectionné
        /// </summary>
        public CircuitData SelectedCircuit => _selectedCircuit;

        #endregion
    }

    /// <summary>
    /// Événement déclenché quand un circuit est sélectionné
    /// </summary>
    [System.Serializable]
    public class CircuitSelectedEvent : UnityEngine.Events.UnityEvent<CircuitData> { }
}
