using UnityEngine;
using UnityEngine.Events;
using ArcadeRacer.Vehicle;

namespace ArcadeRacer.RaceSystem
{
    /// <summary>
    /// Représente un checkpoint sur le circuit.  
    /// Détecte le passage des véhicules et notifie le CheckpointManager. 
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("=== CHECKPOINT INFO ===")]
        [SerializeField, Tooltip("Index du checkpoint dans le circuit (0 = start/finish)")]
        private int checkpointIndex;

        [SerializeField, Tooltip("Est-ce la ligne de départ/arrivée ?")]
        private bool isStartFinishLine = false;

        [Header("=== VISUAL ===")]
        [SerializeField, Tooltip("Afficher le gizmo en mode jeu")]
        private bool showGizmo = true;

        [SerializeField, Tooltip("Couleur du checkpoint")]
        private Color gizmoColor = Color.green;

        // Events
        public UnityEvent<VehicleController> OnVehiclePassed;

        private BoxCollider _trigger;

        #region Properties

        public int Index => checkpointIndex;
        public bool IsStartFinishLine => isStartFinishLine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Vérifier si c'est un véhicule
            VehicleController vehicle = other.GetComponentInParent<VehicleController>();
            
            if (vehicle != null)
            {
                OnVehiclePassedCheckpoint(vehicle);
            }
        }

        #endregion

        #region Setup

        private void SetupTrigger()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;

            // Taille par défaut si pas configuré
            if (_trigger.size == Vector3.one)
            {
                _trigger.size = new Vector3(15f, 5f, 2f); // Largeur, hauteur, épaisseur
            }
        }

        /// <summary>
        /// Configurer le checkpoint
        /// </summary>
        public void Setup(int index, bool isFinishLine = false)
        {
            checkpointIndex = index;
            isStartFinishLine = isFinishLine;
            gameObject.name = isFinishLine ? "Checkpoint_Start_Finish" : $"Checkpoint_{index}";

            // Couleur différente pour la ligne d'arrivée
            gizmoColor = isFinishLine ? Color.cyan : Color.green;
        }

        #endregion

        #region Vehicle Detection

        private void OnVehiclePassedCheckpoint(VehicleController vehicle)
        {
            // Notifier l'event
            OnVehiclePassed?.Invoke(vehicle);

            // Notifier le CheckpointManager
            CheckpointManager manager = FindFirstObjectByType<CheckpointManager>();
            if (manager != null)
            {
                manager. OnCheckpointPassed(vehicle, this);
            }

            Debug.Log($"[Checkpoint {checkpointIndex}] {vehicle.name} passed!");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (! showGizmo) return;

            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) return;

            // Couleur différente si c'est la ligne d'arrivée
            Gizmos.color = isStartFinishLine ? Color.cyan : gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);

            // Remplissage transparent
            Color fillColor = gizmoColor;
            fillColor.a = 0.2f;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(col.center, col. size);

            // Flèche indiquant la direction
            Gizmos.color = Color.yellow;
            Gizmos. DrawRay(Vector3.zero, Vector3.forward * 3f);
        }

        private void OnDrawGizmosSelected()
        {
            // Afficher l'index en mode sélectionné
            #if UNITY_EDITOR
            UnityEditor. Handles.Label(transform.position + Vector3.up * 3f, 
                $"Checkpoint {checkpointIndex}" + (isStartFinishLine ?  " (START/FINISH)" : ""));
            #endif
        }

        #endregion
    }
}