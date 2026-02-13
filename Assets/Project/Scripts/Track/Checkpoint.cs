using UnityEngine;
using UnityEngine.Events;
using ArcadeRacer.Vehicle;

namespace ArcadeRacer.RaceSystem
{
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

        [Header("=== MESH VISUAL ===")]
        [SerializeField, Tooltip("Material pour le mesh visible")]
        private Material checkpointMaterial;

        [SerializeField, Tooltip("Afficher le mesh en jeu")]
        private bool showMeshInGame = true;

        public UnityEvent<VehicleController> OnVehiclePassed;

        private BoxCollider _trigger;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public int Index => checkpointIndex;
        public bool IsStartFinishLine => isStartFinishLine;

        private void Awake()
        {
            SetupTrigger();
            SetupVisualMesh(); // ← NOUVEAU
        }

        private void OnTriggerEnter(Collider other)
        {
            VehicleController vehicle = other.GetComponentInParent<VehicleController>();

            if (vehicle != null)
            {
                OnVehiclePassedCheckpoint(vehicle);
            }
        }

        private void SetupTrigger()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;

            if (_trigger.size == Vector3.one)
            {
                _trigger.size = new Vector3(15f, 5f, 2f);
            }
        }

        // ← NOUVEAU : Créer un mesh visible
        private void SetupVisualMesh()
        {
            // Récupérer ou créer MeshFilter
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();

            // Récupérer ou créer MeshRenderer
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();

            // Créer un Quad manuel
            _meshFilter.mesh = CreateQuadMesh();

            // Assigner le material
            if (checkpointMaterial != null)
            {
                _meshRenderer.material = checkpointMaterial;
            }
            else
            {
                // Créer un material par défaut si aucun n'est assigné
                Material defaultMat = new Material(Shader.Find("Standard"));
                defaultMat.color = isStartFinishLine ? new Color(0, 1, 1, 0.5f) : new Color(0, 1, 0, 0.5f); // Cyan ou Vert transparent
                _meshRenderer.material = defaultMat;
            }

            _meshRenderer.enabled = showMeshInGame;
        }

        // Créer un mesh Quad adapté au BoxCollider
        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();

            float width = _trigger.size.x;
            float height = _trigger.size.y;

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-width/2, -height/2, 0),
                new Vector3(width/2, -height/2, 0),
                new Vector3(-width/2, height/2, 0),
                new Vector3(width/2, height/2, 0)
            };

            int[] triangles = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            return mesh;
        }

        public void Setup(int index, bool isFinishLine = false)
        {
            checkpointIndex = index;
            isStartFinishLine = isFinishLine;
            gameObject.name = isFinishLine ? "Checkpoint_Start_Finish" : $"Checkpoint_{index}";

            gizmoColor = isFinishLine ? Color.cyan : Color.green;
        }

        private void OnVehiclePassedCheckpoint(VehicleController vehicle)
        {
            OnVehiclePassed?.Invoke(vehicle);

            CheckpointManager manager = FindFirstObjectByType<CheckpointManager>();
            if (manager != null)
            {
                manager.OnCheckpointPassed(vehicle, this);
            }

            Debug.Log($"[Checkpoint {checkpointIndex}] {vehicle.name} passed!");
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) return;

            Gizmos.color = isStartFinishLine ? Color.cyan : gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);

            Color fillColor = gizmoColor;
            fillColor.a = 0.2f;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(col.center, col.size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(Vector3.zero, Vector3.forward * 3f);
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                $"Checkpoint {checkpointIndex}" + (isStartFinishLine ? " (START/FINISH)" : ""));
#endif
        }
    }
}