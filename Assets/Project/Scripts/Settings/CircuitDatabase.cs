using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ArcadeRacer.Settings
{
    /// <summary>
    /// Base de données centralisée de tous les circuits disponibles.
    /// Source unique de vérité pour tous les circuits du jeu.
    /// </summary>
    [CreateAssetMenu(fileName = "CircuitDatabase", menuName = "Arcade Racer/Circuit Database")]
    public class CircuitDatabase : ScriptableObject
    {
        [Header("=== CIRCUITS DISPONIBLES ===")]
        [SerializeField]
        [Tooltip("Liste de tous les circuits disponibles dans le jeu")]
        private List<CircuitData> availableCircuits = new List<CircuitData>();

        #region Singleton-like Access

        private static CircuitDatabase _instance;

        /// <summary>
        /// Instance singleton chargée via Resources
        /// </summary>
        public static CircuitDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<CircuitDatabase>("CircuitDatabase");
                    
                    if (_instance == null)
                    {
                        Debug.LogWarning("[CircuitDatabase] Aucune instance trouvée dans Resources! Créez un CircuitDatabase.asset dans Assets/Resources/");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Liste de tous les circuits disponibles (lecture seule)
        /// </summary>
        public IReadOnlyList<CircuitData> AvailableCircuits => availableCircuits;

        /// <summary>
        /// Nombre de circuits disponibles
        /// </summary>
        public int CircuitCount => availableCircuits != null ? availableCircuits.Count : 0;

        #endregion

        #region Public API

        /// <summary>
        /// Récupère un circuit par son nom
        /// </summary>
        public CircuitData GetCircuitByName(string circuitName)
        {
            if (availableCircuits == null || string.IsNullOrEmpty(circuitName))
            {
                return null;
            }

            return availableCircuits.FirstOrDefault(c => c != null && c.circuitName == circuitName);
        }

        /// <summary>
        /// Récupère un circuit par son index
        /// </summary>
        public CircuitData GetCircuitByIndex(int index)
        {
            if (availableCircuits == null || index < 0 || index >= availableCircuits.Count)
            {
                return null;
            }

            return availableCircuits[index];
        }

        /// <summary>
        /// Récupère l'index d'un circuit
        /// </summary>
        public int GetCircuitIndex(CircuitData circuit)
        {
            if (availableCircuits == null || circuit == null)
            {
                return -1;
            }

            return availableCircuits.IndexOf(circuit);
        }

        /// <summary>
        /// Vérifie si un circuit existe dans la base de données
        /// </summary>
        public bool ContainsCircuit(CircuitData circuit)
        {
            return availableCircuits != null && availableCircuits.Contains(circuit);
        }

        /// <summary>
        /// Vérifie si un circuit avec ce nom existe
        /// </summary>
        public bool ContainsCircuitByName(string circuitName)
        {
            return GetCircuitByName(circuitName) != null;
        }

        #endregion

        #region Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Retirer les nulls de la liste
            if (availableCircuits != null)
            {
                availableCircuits.RemoveAll(c => c == null);
            }

            // Vérifier les doublons
            if (availableCircuits != null && availableCircuits.Count > 0)
            {
                var duplicates = availableCircuits
                    .GroupBy(c => c.circuitName)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicates.Count > 0)
                {
                    Debug.LogWarning($"[CircuitDatabase] Circuits dupliqués détectés: {string.Join(", ", duplicates)}");
                }
            }
        }
#endif

        #endregion
    }
}
