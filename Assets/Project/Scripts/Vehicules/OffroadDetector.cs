using UnityEngine;

namespace ArcadeRacer.Vehicle
{
    /// <summary>
    /// Détecte si les roues du véhicule sont sur la route ou hors route (offroad).
    /// Calcule une pénalité basée sur le nombre de roues hors route.
    /// </summary>
    public class OffroadDetector : MonoBehaviour
    {
        [Header("=== WHEEL REFERENCES ===")]
        [SerializeField, Tooltip("Transformations des 4 roues")]
        private Transform[] wheels = new Transform[4];
        
        [Header("=== LAYERS ===")]
        [SerializeField, Tooltip("Layer(s) de la route")]
        private LayerMask roadLayer;
        
        [SerializeField, Tooltip("Layer(s) du terrain offroad")]
        private LayerMask offroadLayer;
        
        [Header("=== DETECTION SETTINGS ===")]
        [SerializeField, Tooltip("Distance de détection au sol")]
        private float raycastDistance = 0.5f;
        
        [SerializeField, Tooltip("Priorité de détection: si une roue touche la route ET l'offroad, considérer comme route")]
        private bool roadHasPriority = true;
        
        [Header("=== PENALTY SETTINGS ===")]
        [SerializeField, Tooltip("Mode de pénalité offroad")]
        private OffroadPenaltyMode penaltyMode = OffroadPenaltyMode.ReducedAcceleration;
        
        [SerializeField, Tooltip("Multiplicateur d'accélération quand toutes les roues sont offroad (0-1)")]
        [Range(0f, 1f)]
        private float fullOffroadAccelerationMultiplier = 0.5f;
        
        [SerializeField, Tooltip("Drag additionnel quand toutes les roues sont offroad")]
        [Range(0f, 5f)]
        private float fullOffroadDragIncrease = 2f;
        
        [Header("=== DEBUG ===")]
        [SerializeField]
        private bool showDebug = false;
        
        // Runtime state
        private int _wheelsOffroad = 0;
        private float _offroadRatio = 0f;
        
        public enum OffroadPenaltyMode
        {
            ReducedAcceleration,
            IncreasedDrag,
            Both
        }
        
        #region Properties
        
        /// <summary>
        /// Nombre de roues actuellement hors route (0-4)
        /// </summary>
        public int WheelsOffroad => _wheelsOffroad;
        
        /// <summary>
        /// Ratio de roues hors route (0 = toutes sur route, 1 = toutes hors route)
        /// </summary>
        public float OffroadRatio => _offroadRatio;
        
        /// <summary>
        /// Est-ce que le véhicule est complètement hors route?
        /// </summary>
        public bool IsFullyOffroad => _wheelsOffroad == wheels.Length;
        
        /// <summary>
        /// Multiplicateur d'accélération basé sur l'offroad (1 = normal, <1 = pénalité)
        /// </summary>
        public float AccelerationMultiplier
        {
            get
            {
                if (penaltyMode == OffroadPenaltyMode.IncreasedDrag)
                    return 1f;
                
                return Mathf.Lerp(1f, fullOffroadAccelerationMultiplier, _offroadRatio);
            }
        }
        
        /// <summary>
        /// Drag additionnel basé sur l'offroad (0 = normal, >0 = pénalité)
        /// </summary>
        public float AdditionalDrag
        {
            get
            {
                if (penaltyMode == OffroadPenaltyMode.ReducedAcceleration)
                    return 0f;
                
                return Mathf.Lerp(0f, fullOffroadDragIncrease, _offroadRatio);
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void FixedUpdate()
        {
            DetectOffroadWheels();
        }
        
        #endregion
        
        #region Detection
        
        private void DetectOffroadWheels()
        {
            if (wheels == null || wheels.Length == 0)
            {
                _wheelsOffroad = 0;
                _offroadRatio = 0f;
                return;
            }
            
            int offroadCount = 0;

            foreach (Transform wheel in wheels)
            {
                if (wheel == null) continue;

                bool isOnRoad = CheckWheelOnRoad(wheel);

                if (!isOnRoad)
                {

                    wheel.GetComponent<Renderer>().material.color = Color.red;
                    offroadCount++;
                }
                else
                {
                    wheel.GetComponent<Renderer>().material.color = Color.green;
                }
            }
            _wheelsOffroad = offroadCount;
            _offroadRatio = (float)offroadCount / wheels.Length;
            
            if (showDebug && Time.frameCount % 30 == 0 && _wheelsOffroad > 0)
            {
                Debug.Log($"[OffroadDetector] {_wheelsOffroad}/{wheels.Length} wheels offroad | Ratio: {_offroadRatio:P0} | Accel: {AccelerationMultiplier:F2}x | Drag: +{AdditionalDrag:F2}");
            }
        }
        
        private bool CheckWheelOnRoad(Transform wheel)
        {
            Vector3 rayOrigin = wheel.position;
            Vector3 rayDirection = Vector3.down;
            
            bool hitRoad = false;
            bool hitOffroad = false;
            
            // Vérifier la route
            if (UnityEngine.Physics.Raycast(rayOrigin, rayDirection, out RaycastHit roadHit, raycastDistance, roadLayer))
            {
                hitRoad = true;
                
                if (showDebug)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * raycastDistance, Color.green, 0.1f);
                }
            }
            
            // Vérifier l'offroad
            if (UnityEngine.Physics.Raycast(rayOrigin, rayDirection, out RaycastHit offroadHit, raycastDistance, offroadLayer))
            {
                hitOffroad = true;
                
                if (showDebug && !hitRoad)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * raycastDistance, Color.red, 0.1f);
                }
            }
            
            // Si les deux sont détectés (mesh de route sur terrain offroad)
            if (hitRoad && hitOffroad)
            {
                // Si la route a priorité, on considère la roue sur route
                if (roadHasPriority)
                    return true;
                
                // Sinon, comparer les distances (le plus proche gagne)
                return roadHit.distance <= offroadHit.distance;
            }
            
            // Retourner true seulement si on a touché la route
            return hitRoad;
        }
        
        #endregion
        
        #region Debug Visualization
        
        private void OnDrawGizmos()
        {
            if (!showDebug || wheels == null) return;
            
            foreach (Transform wheel in wheels)
            {
                if (wheel == null) continue;
                
                // Visualiser les roues
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(wheel.position, 0.1f);
            }
        }
        
        #endregion
    }
}
