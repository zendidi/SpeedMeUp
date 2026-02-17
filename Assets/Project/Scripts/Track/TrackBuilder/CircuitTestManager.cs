using UnityEngine;
using ArcadeRacer.Settings;
using ArcadeRacer.Managers;
using ArcadeRacer.Vehicle;

namespace ArcadeRacer.Managers
{
    /// <summary>
    /// Manager de test pour charger un circuit au démarrage.
    /// À utiliser dans votre scène de gameplay.
    /// </summary>
    public class CircuitTestManager : MonoBehaviour
    {
        [Header("=== TEST CONFIGURATION ===")]
        [SerializeField]
        [Tooltip("Circuit à charger au démarrage")]
        private CircuitData circuitToLoad;
        
        [SerializeField]
        [Tooltip("Référence au véhicule (optionnel, sinon auto-détecté)")]
        private VehicleController vehicleController;
        
        [SerializeField]
        [Tooltip("Charger automatiquement au Start()")]
        private bool autoLoadOnStart = true;
        
        [Header("=== DEBUG ===")]
        [SerializeField]
        private bool showDebugInfo = true;
        
        private void Start()
        {
            if (autoLoadOnStart && circuitToLoad != null)
            {
                LoadCircuit();
            }
        }
        
        /// <summary>
        /// Charge le circuit configuré.
        /// </summary>
        [ContextMenu("Load Circuit")]
        public void LoadCircuit()
        {
            if (circuitToLoad == null)
            {
                Debug.LogError("[CircuitTestManager] Aucun circuit assigné !");
                return;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[CircuitTestManager] Chargement du circuit '{circuitToLoad.circuitName}'...");
            }
            
            // Charger le circuit
            CircuitManager.Instance.LoadCircuit(circuitToLoad);
            
            // Spawner le véhicule
            if (vehicleController == null)
            {
                vehicleController = FindFirstObjectByType<VehicleController>();
            }
            
            if (vehicleController != null)
            {
                CircuitManager.Instance.SpawnVehicle(vehicleController.transform);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[CircuitTestManager] Véhicule spawné à {vehicleController.transform.position}");
                }
            }
            else
            {
                Debug.LogWarning("[CircuitTestManager] Aucun VehicleController trouvé dans la scène !");
            }
        }
        
        /// <summary>
        /// Décharge le circuit actuel.
        /// </summary>
        [ContextMenu("Unload Circuit")]
        public void UnloadCircuit()
        {
            CircuitManager.Instance.UnloadCurrentCircuit();
            
            if (showDebugInfo)
            {
                Debug.Log("[CircuitTestManager] Circuit déchargé.");
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Box("CIRCUIT TEST MANAGER");
            
            if (CircuitManager.Instance.IsCircuitLoaded)
            {
                GUILayout.Label($"Circuit: {CircuitManager.Instance.CurrentCircuit.circuitName}");
                GUILayout.Label($"Longueur: {CircuitManager.Instance.CurrentCircuit.TotalLength:F1}m");
                
                if (GUILayout.Button("Unload Circuit"))
                {
                    UnloadCircuit();
                }
            }
            else
            {
                GUILayout.Label("Aucun circuit chargé");
                
                if (circuitToLoad != null && GUILayout.Button("Load Circuit"))
                {
                    LoadCircuit();
                }
            }
            
            GUILayout.EndArea();
        }
    }
}