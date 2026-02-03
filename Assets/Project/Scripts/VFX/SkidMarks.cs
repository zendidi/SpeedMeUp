using UnityEngine;

namespace ArcadeRacer. VFX
{
    /// <summary>
    /// Crée des traînées de pneus au sol pendant le drift. 
    /// Utilise un TrailRenderer pour dessiner les marques.
    /// </summary>
    public class SkidMarks : MonoBehaviour
    {
        [Header("=== TRAIL RENDERERS ===")]
        [SerializeField] private TrailRenderer leftSkidTrail;
        [SerializeField] private TrailRenderer rightSkidTrail;

        [Header("=== SETTINGS ===")]
        [SerializeField] private float trailWidth = 0.15f;
        [SerializeField] private float trailTime = 5f;
        [SerializeField] private Color trailColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] private Material trailMaterial;

        private bool _isEmitting = false;

        #region Unity Lifecycle

        private void Awake()
        {
            SetupTrailRenderers();
        }

        #endregion

        #region Setup

        private void SetupTrailRenderers()
        {
            // Créer les trail renderers s'ils n'existent pas
            if (leftSkidTrail == null)
            {
                leftSkidTrail = CreateSkidTrail("SkidMark_Left");
            }

            if (rightSkidTrail == null)
            {
                rightSkidTrail = CreateSkidTrail("SkidMark_Right");
            }
        }

        private TrailRenderer CreateSkidTrail(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;

            TrailRenderer trail = go.AddComponent<TrailRenderer>();

            // Configuration
            trail.time = trailTime;
            trail.startWidth = trailWidth;
            trail.endWidth = trailWidth * 0.5f;
            trail. material = trailMaterial != null ? trailMaterial : CreateDefaultMaterial();
            trail. startColor = trailColor;
            trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.minVertexDistance = 0.1f;
            trail.emitting = false;

            return trail;
        }

        private Material CreateDefaultMaterial()
        {
            // Créer un matériau simple si pas assigné
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = trailColor;
            return mat;
        }

        #endregion

        #region Skid Control

        /// <summary>
        /// Activer/désactiver les traînées
        /// </summary>
        public void SetDrifting(bool isDrifting)
        {
            if (leftSkidTrail == null || rightSkidTrail == null) return;

            if (isDrifting != _isEmitting)
            {
                _isEmitting = isDrifting;
                leftSkidTrail.emitting = isDrifting;
                rightSkidTrail.emitting = isDrifting;
            }
        }

        /// <summary>
        /// Positionner les trails sur les roues arrière
        /// </summary>
        public void SetWheelPositions(Transform leftWheel, Transform rightWheel)
        {
            if (leftSkidTrail != null && leftWheel != null)
            {
                leftSkidTrail.transform.position = leftWheel.position;
            }

            if (rightSkidTrail != null && rightWheel != null)
            {
                rightSkidTrail.transform.position = rightWheel.position;
            }
        }

        /// <summary>
        /// Effacer toutes les traînées
        /// </summary>
        public void ClearTrails()
        {
            if (leftSkidTrail != null) leftSkidTrail.Clear();
            if (rightSkidTrail != null) rightSkidTrail.Clear();
        }

        #endregion
    }
}