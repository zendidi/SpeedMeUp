using UnityEngine;

namespace ArcadeRacer.Settings
{
    /// <summary>
    /// ScriptableObject contenant toutes les données d'un circuit.
    /// Compatible avec le système de checkpoints existant (ArcadeRacer.RaceSystem).
    /// </summary>
    [CreateAssetMenu(fileName = "Circuit", menuName = "Arcade Racer/Circuit Data")]
    public class CircuitData : ScriptableObject
    {
        [Header("=== CIRCUIT INFO ===")]
        [Tooltip("Nom affiché dans le menu de sélection")]
        public string circuitName = "New Circuit";
        
        [Tooltip("Thumbnail pour l'UI de sélection (recommandé: 256x256)")]
        public Sprite thumbnail;
        
        [TextArea(3, 5)]
        [Tooltip("Description du circuit affichée dans le menu")]
        public string description = "A challenging racing circuit.";
        
        [Header("=== SPLINE DATA ===")]
        [Tooltip("Points définissant la trajectoire du circuit")]
        public SplinePoint[] splinePoints = new SplinePoint[0];
        
        [Range(5f, 30f)]
        [Tooltip("Largeur de la piste en mètres")]
        public float trackWidth = 10f;
        
        [Tooltip("True si le circuit forme une boucle fermée")]
        public bool closedLoop = true;
        
        [Header("=== SPAWN ===")]
        [Tooltip("Position de départ du véhicule")]
        public Vector3 spawnPosition = Vector3.zero;
        
        [Tooltip("Rotation de départ du véhicule")]
        public Quaternion spawnRotation = Quaternion.identity;
        
        [Header("=== CHECKPOINTS (Mode existant) ===")]
        [Tooltip("Nombre de checkpoints à générer automatiquement")]
        [Range(3, 50)]
        public int autoCheckpointCount = 10;
        
        [Tooltip("Largeur des checkpoints générés")]
        public float checkpointWidth = 15f;
        
        [Tooltip("Hauteur des checkpoints générés")]
        public float checkpointHeight = 5f;
        
        [Header("=== VISUAL ===")]
        [Tooltip("Material appliqué à la surface de la route")]
        public Material roadMaterial;
        
        [Tooltip("Material appliqué aux murs latéraux")]
        public Material wallMaterial;
        
        [Range(0.5f, 5f)]
        [Tooltip("Hauteur des murs en mètres")]
        public float wallHeight = 2f;
        
        [Tooltip("Générer les murs latéraux")]
        public bool generateWalls = true;
        
        [Header("=== GAMEPLAY ===")]
        [Range(1, 10)]
        [Tooltip("Nombre de tours pour terminer la course")]
        public int targetLapCount = 3;
        
        [Tooltip("Temps pour médaille d'or (secondes)")]
        public float goldTime = 60f;
        
        [Tooltip("Temps pour médaille d'argent (secondes)")]
        public float silverTime = 75f;
        
        [Tooltip("Temps pour médaille de bronze (secondes)")]
        public float bronzeTime = 90f;
        
        [Header("=== MINIMAP ===")]
        [Tooltip("Texture de la minimap (générée automatiquement)")]
        public Texture2D minimapTexture;
        
        [Header("=== METADATA ===")]
        [SerializeField] 
        private float _totalLength;
        public float TotalLength => _totalLength;
        
        /// <summary>
        /// Calcule la longueur totale du circuit basée sur les points de spline.
        /// </summary>
        public void CalculateTotalLength()
        {
            _totalLength = 0f;
            if (splinePoints == null || splinePoints.Length < 2) return;
            
            for (int i = 0; i < splinePoints.Length - 1; i++)
            {
                _totalLength += Vector3.Distance(splinePoints[i].position, splinePoints[i + 1].position);
            }
            
            if (closedLoop && splinePoints.Length > 1)
            {
                _totalLength += Vector3.Distance(
                    splinePoints[splinePoints.Length - 1].position, 
                    splinePoints[0].position
                );
            }
        }
        
        /// <summary>
        /// Valide les données du circuit.
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrEmpty(circuitName))
            {
                errorMessage = "Circuit name is required.";
                return false;
            }
            
            if (splinePoints == null || splinePoints.Length < 3)
            {
                errorMessage = "At least 3 spline points are required.";
                return false;
            }
            
            if (trackWidth < 5f)
            {
                errorMessage = "Track width must be at least 5 meters.";
                return false;
            }
            
            errorMessage = string.Empty;
            return true;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            CalculateTotalLength();
            
            if (silverTime < goldTime) silverTime = goldTime + 15f;
            if (bronzeTime < silverTime) bronzeTime = silverTime + 15f;
        }
#endif
    }
    
    /// <summary>
    /// Point de la spline avec position et tangentes pour courbes de Bézier.
    /// Compatible avec Unity Splines.
    /// </summary>
    [System.Serializable]
    public struct SplinePoint
    {
        public Vector3 position;
        public Vector3 tangentIn;
        public Vector3 tangentOut;
        
        public static SplinePoint Create(Vector3 position)
        {
            return new SplinePoint
            {
                position = position,
                tangentIn = Vector3.back,
                tangentOut = Vector3.forward
            };
        }
    }
    
    public enum MedalType
    {
        None,
        Bronze,
        Silver,
        Gold
    }
}