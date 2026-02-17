using UnityEngine;
using UnityEngine.UI;
using ArcadeRacer.Settings;
using ArcadeRacer.Vehicle;
using System.Collections.Generic;

namespace ArcadeRacer.Managers
{
    /// <summary>
    /// Permet de charger et changer de circuit au runtime depuis la scène de jeu.
    /// Fournit une interface de navigation simple et un debug UI pour tester rapidement.
    /// </summary>
    public class RuntimeCircuitLoader : MonoBehaviour
    {
        #region Serialized Fields

        [Header("=== CIRCUIT LIBRARY ===")]
        [SerializeField]
        [Tooltip("Liste des circuits disponibles pour le chargement runtime")]
        private List<CircuitData> availableCircuits = new List<CircuitData>();

        [Header("=== VEHICLE ===")]
        [SerializeField]
        [Tooltip("Contrôleur du véhicule à téléporter (auto-détecté si non assigné)")]
        private VehicleController vehicleController;

        [Header("=== UI INTEGRATION (Optional) ===")]
        [SerializeField]
        [Tooltip("Dropdown UI pour sélectionner un circuit (optionnel)")]
        private Dropdown circuitDropdown;

        [SerializeField]
        [Tooltip("Bouton UI pour charger le circuit sélectionné (optionnel)")]
        private Button loadButton;

        [Header("=== DEBUG ===")]
        [SerializeField]
        [Tooltip("Afficher l'interface de debug OnGUI")]
        private bool showDebugUI = true;

        #endregion

        #region Private Fields

        private int _selectedCircuitIndex = 0;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Auto-détecter le VehicleController si non assigné
            if (vehicleController == null)
            {
                vehicleController = FindFirstObjectByType<VehicleController>();
                
                if (vehicleController != null)
                {
                    Debug.Log("[RuntimeCircuitLoader] VehicleController auto-détecté.");
                }
                else
                {
                    Debug.LogWarning("[RuntimeCircuitLoader] Aucun VehicleController trouvé dans la scène!");
                }
            }

            // Initialiser le Dropdown UI si présent
            InitializeDropdownUI();

            // Initialiser le bouton de chargement si présent
            if (loadButton != null)
            {
                loadButton.onClick.AddListener(() => LoadCircuit(_selectedCircuitIndex));
            }
        }

        private void Update()
        {
            // Support des raccourcis clavier
            if (Input.GetKeyDown(KeyCode.N))
            {
                LoadNextCircuit();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                LoadPreviousCircuit();
            }
        }

        private void OnGUI()
        {
            if (!showDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            
            GUILayout.Label("=== RUNTIME CIRCUIT LOADER ===", GUI.skin.box);
            
            // Afficher le circuit actuellement chargé
            if (CircuitManager.Instance.IsCircuitLoaded)
            {
                CircuitData currentCircuit = CircuitManager.Instance.CurrentCircuit;
                GUILayout.Label($"Circuit actuel: {currentCircuit.circuitName}");
                GUILayout.Label($"Longueur: {currentCircuit.TotalLength:F1}m");
            }
            else
            {
                GUILayout.Label("Circuit actuel: Aucun");
                GUILayout.Label("Longueur: N/A");
            }

            GUILayout.Space(10);

            // Afficher le circuit sélectionné
            if (availableCircuits.Count > 0 && _selectedCircuitIndex >= 0 && _selectedCircuitIndex < availableCircuits.Count)
            {
                GUILayout.Label($"Circuit sélectionné: {availableCircuits[_selectedCircuitIndex].circuitName}");
            }
            else
            {
                GUILayout.Label("Circuit sélectionné: Aucun");
            }

            // Boutons de navigation
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("◀ Précédent", GUILayout.Width(120)))
            {
                LoadPreviousCircuit();
            }
            
            if (GUILayout.Button("Charger", GUILayout.Width(120)))
            {
                LoadCircuit(_selectedCircuitIndex);
            }
            
            if (GUILayout.Button("Suivant ▶", GUILayout.Width(120)))
            {
                LoadNextCircuit();
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Charge un circuit par son index dans la liste des circuits disponibles.
        /// </summary>
        /// <param name="circuitIndex">Index du circuit à charger</param>
        public void LoadCircuit(int circuitIndex)
        {
            // Validation de l'index
            if (availableCircuits == null || availableCircuits.Count == 0)
            {
                Debug.LogError("[RuntimeCircuitLoader] La liste des circuits disponibles est vide!");
                return;
            }

            if (circuitIndex < 0 || circuitIndex >= availableCircuits.Count)
            {
                Debug.LogError($"[RuntimeCircuitLoader] Index de circuit invalide: {circuitIndex} (total: {availableCircuits.Count})");
                return;
            }

            CircuitData circuit = availableCircuits[circuitIndex];
            LoadCircuit(circuit);
        }

        /// <summary>
        /// Charge un circuit directement via sa référence CircuitData.
        /// Place automatiquement le véhicule au startPoint du circuit.
        /// </summary>
        /// <param name="circuit">Données du circuit à charger</param>
        public void LoadCircuit(CircuitData circuit)
        {
            if (circuit == null)
            {
                Debug.LogError("[RuntimeCircuitLoader] CircuitData est null!");
                return;
            }

            if (vehicleController == null)
            {
                Debug.LogError("[RuntimeCircuitLoader] VehicleController non assigné!");
                return;
            }

            Debug.Log($"[RuntimeCircuitLoader] Chargement du circuit: {circuit.circuitName}");

            // Charger le circuit via le CircuitManager
            CircuitManager.Instance.LoadCircuit(circuit);

            // Placer le véhicule au startPoint
            CircuitManager.Instance.SpawnVehicle(vehicleController.transform);

            // Réinitialiser la vélocité du Rigidbody
            Rigidbody vehicleRigidbody = vehicleController.GetComponent<Rigidbody>();
            if (vehicleRigidbody != null)
            {
                vehicleRigidbody.linearVelocity = Vector3.zero;
                vehicleRigidbody.angularVelocity = Vector3.zero;
            }

            // Logger les informations de debug
            if (CircuitManager.Instance.SpawnPoint != null)
            {
                Debug.Log($"[RuntimeCircuitLoader] Véhicule téléporté à: {CircuitManager.Instance.SpawnPoint.position}");
            }

            Debug.Log($"[RuntimeCircuitLoader] Circuit chargé avec succès: {circuit.circuitName} ({circuit.TotalLength:F1}m)");
        }

        /// <summary>
        /// Charge le circuit suivant dans la liste (boucle cyclique).
        /// </summary>
        [ContextMenu("Load Next Circuit")]
        public void LoadNextCircuit()
        {
            if (availableCircuits == null || availableCircuits.Count == 0)
            {
                Debug.LogWarning("[RuntimeCircuitLoader] Aucun circuit disponible.");
                return;
            }

            _selectedCircuitIndex = (_selectedCircuitIndex + 1) % availableCircuits.Count;
            
            Debug.Log($"[RuntimeCircuitLoader] Circuit suivant sélectionné: {availableCircuits[_selectedCircuitIndex].circuitName}");
            
            // Mettre à jour le dropdown si présent
            if (circuitDropdown != null)
            {
                circuitDropdown.value = _selectedCircuitIndex;
            }
        }

        /// <summary>
        /// Charge le circuit précédent dans la liste (boucle cyclique).
        /// </summary>
        [ContextMenu("Load Previous Circuit")]
        public void LoadPreviousCircuit()
        {
            if (availableCircuits == null || availableCircuits.Count == 0)
            {
                Debug.LogWarning("[RuntimeCircuitLoader] Aucun circuit disponible.");
                return;
            }

            _selectedCircuitIndex--;
            if (_selectedCircuitIndex < 0)
            {
                _selectedCircuitIndex = availableCircuits.Count - 1;
            }
            
            Debug.Log($"[RuntimeCircuitLoader] Circuit précédent sélectionné: {availableCircuits[_selectedCircuitIndex].circuitName}");
            
            // Mettre à jour le dropdown si présent
            if (circuitDropdown != null)
            {
                circuitDropdown.value = _selectedCircuitIndex;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialise le Dropdown UI avec la liste des circuits disponibles.
        /// </summary>
        private void InitializeDropdownUI()
        {
            if (circuitDropdown == null) return;

            circuitDropdown.ClearOptions();

            if (availableCircuits == null || availableCircuits.Count == 0)
            {
                Debug.LogWarning("[RuntimeCircuitLoader] Aucun circuit disponible pour le Dropdown.");
                return;
            }

            // Créer la liste des options
            List<string> options = new List<string>();
            foreach (CircuitData circuit in availableCircuits)
            {
                string circuitName = circuit != null ? circuit.circuitName : "Invalid Circuit";
                options.Add(circuitName);
            }

            circuitDropdown.AddOptions(options);
            circuitDropdown.value = _selectedCircuitIndex;

            // Ajouter un listener pour mettre à jour l'index sélectionné
            circuitDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

            Debug.Log($"[RuntimeCircuitLoader] Dropdown initialisé avec {options.Count} circuits.");
        }

        /// <summary>
        /// Appelé quand la valeur du dropdown change.
        /// </summary>
        private void OnDropdownValueChanged(int newIndex)
        {
            _selectedCircuitIndex = newIndex;
            Debug.Log($"[RuntimeCircuitLoader] Circuit sélectionné via Dropdown: {availableCircuits[_selectedCircuitIndex].circuitName}");
        }

        #endregion
    }
}
