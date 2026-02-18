using ArcadeRacer.Utilities;

namespace ArcadeRacer.Settings
{
    /// <summary>
    /// Configuration partagée entre éditeur et runtime pour la génération de circuits.
    /// Garantit que le preview de l'éditeur correspond exactement au résultat en jeu.
    /// </summary>
    public static class CircuitGenerationConstants
    {
        #region Mesh Generation Parameters
        
        /// <summary>
        /// Nombre de segments par point de spline pour l'interpolation.
        /// Valeur identique éditeur/runtime pour garantir le même mesh.
        /// </summary>
        public const int SEGMENTS_PER_SPLINE_POINT = 50;
        
        /// <summary>
        /// Multiplicateur de qualité des courbes (plus élevé = courbes plus lisses).
        /// Valeur identique éditeur/runtime.
        /// </summary>
        public const float CURVE_QUALITY_MULTIPLIER =20f;
        
        /// <summary>
        /// Tiling UV en X (largeur de la texture route).
        /// </summary>
        public const float UV_TILING_X = 1f;
        
        /// <summary>
        /// Tiling UV en Y (longueur de la texture route).
        /// </summary>
        public const float UV_TILING_Y = 0.5f;
        
        #endregion
        
        #region Collider Settings
        
        /// <summary>
        /// Générer colliders dans l'éditeur (preview) - généralement false pour performance.
        /// </summary>
        public const bool GENERATE_COLLIDER_EDITOR = false;
        
        /// <summary>
        /// Générer colliders au runtime (jeu) - doit être true pour physique.
        /// </summary>
        public const bool GENERATE_COLLIDER_RUNTIME = true;
        
        #endregion
        
        #region Configuration Properties
        
        /// <summary>
        /// Configuration pour le preview dans l'éditeur.
        /// Utilise les mêmes paramètres que le runtime (sauf colliders).
        /// </summary>
        public static CircuitMeshGenerator.GenerationConfig EditorConfig => new CircuitMeshGenerator.GenerationConfig
        {
            segmentsPerSplinePoint = SEGMENTS_PER_SPLINE_POINT,
            curveQualityMultiplier = CURVE_QUALITY_MULTIPLIER,
            uvTilingX = UV_TILING_X,
            uvTilingY = UV_TILING_Y,
            generateCollider = GENERATE_COLLIDER_EDITOR,
            optimizeMesh = true
        };
        
        /// <summary>
        /// Configuration pour le runtime (en jeu).
        /// Utilise les mêmes paramètres que l'éditeur (sauf colliders).
        /// </summary>
        public static CircuitMeshGenerator.GenerationConfig RuntimeConfig => new CircuitMeshGenerator.GenerationConfig
        {
            segmentsPerSplinePoint = SEGMENTS_PER_SPLINE_POINT,
            curveQualityMultiplier = CURVE_QUALITY_MULTIPLIER,
            uvTilingX = UV_TILING_X,
            uvTilingY = UV_TILING_Y,
            generateCollider = GENERATE_COLLIDER_RUNTIME,
            optimizeMesh = true
        };
        
        #endregion
    }
}
