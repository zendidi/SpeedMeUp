using UnityEngine;
using UnityEditor;
using System.IO;

namespace ArcadeRacer.Settings
{
    /// <summary>
    /// Outil Editor pour générer automatiquement des thumbnails de circuits
    /// à partir des splinePoints. Crée un sprite 256x256 avec tracé noir sur fond blanc.
    /// </summary>
    public static class CircuitThumbnailGenerator
    {
        private const int THUMBNAIL_SIZE = 256;
        private const float BACKGROUND_ALPHA = 0.5f;
        private const float MARGIN_PERCENTAGE = 0.1f; // 10% de marge
        private const string THUMBNAIL_FOLDER = "Assets/Circuits/Thumbnails";

        /// <summary>
        /// Génère un thumbnail pour un CircuitData
        /// </summary>
        public static Sprite GenerateThumbnail(CircuitData circuitData, bool autoAssign = true)
        {
            if (circuitData == null)
            {
                Debug.LogError("[CircuitThumbnailGenerator] CircuitData est null!");
                return null;
            }

            if (circuitData.splinePoints == null || circuitData.splinePoints.Length < 3)
            {
                Debug.LogError("[CircuitThumbnailGenerator] Pas assez de spline points (minimum 3)!");
                return null;
            }

            // Créer le dossier de destination si nécessaire
            if (!Directory.Exists(THUMBNAIL_FOLDER))
            {
                Directory.CreateDirectory(THUMBNAIL_FOLDER);
                AssetDatabase.Refresh();
            }

            // Générer la texture
            Texture2D texture = GenerateTexture(circuitData.splinePoints, circuitData.closedLoop);

            // Sauvegarder en PNG
            string filename = $"{circuitData.circuitName.Replace(" ", "_")}_Thumbnail.png";
            string path = Path.Combine(THUMBNAIL_FOLDER, filename);
            
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(path, pngData);
            
            // Refresh pour que Unity détecte le nouveau fichier
            AssetDatabase.Refresh();

            // Configurer les import settings
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = THUMBNAIL_SIZE;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            // Charger le sprite créé
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            // Auto-assigner au CircuitData si demandé
            if (autoAssign && sprite != null)
            {
                circuitData.thumbnail = sprite;
                EditorUtility.SetDirty(circuitData);
                AssetDatabase.SaveAssets();
                Debug.Log($"[CircuitThumbnailGenerator] Thumbnail généré et assigné: {path}");
            }

            return sprite;
        }

        /// <summary>
        /// Génère une texture 256x256 du tracé du circuit
        /// </summary>
        private static Texture2D GenerateTexture(SplinePoint[] points, bool closedLoop)
        {
            Texture2D texture = new Texture2D(THUMBNAIL_SIZE, THUMBNAIL_SIZE, TextureFormat.RGBA32, false);

            // Fond blanc avec alpha 0.5
            Color backgroundColor = new Color(1f, 1f, 1f, BACKGROUND_ALPHA);
            Color[] pixels = new Color[THUMBNAIL_SIZE * THUMBNAIL_SIZE];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }
            texture.SetPixels(pixels);

            // Calculer le bounding box des points
            Bounds bounds = CalculateBounds(points);

            // Calculer la transformation pour centrer et mettre à l'échelle
            float margin = THUMBNAIL_SIZE * MARGIN_PERCENTAGE;
            float availableSize = THUMBNAIL_SIZE - (2 * margin);
            
            float scaleX = availableSize / bounds.size.x;
            float scaleY = availableSize / bounds.size.z; // On utilise Z car c'est un circuit 3D
            float scale = Mathf.Min(scaleX, scaleY); // Garder le ratio

            Vector2 offset = new Vector2(
                THUMBNAIL_SIZE / 2f - bounds.center.x * scale,
                THUMBNAIL_SIZE / 2f - bounds.center.z * scale
            );

            // Dessiner les lignes entre les points
            Color lineColor = Color.black;
            
            for (int i = 0; i < points.Length; i++)
            {
                int nextIndex = (i + 1) % points.Length;
                
                // Ne pas dessiner la dernière ligne si le circuit n'est pas fermé
                if (!closedLoop && i == points.Length - 1)
                    break;

                Vector2 start = WorldToTexture(points[i].position, offset, scale);
                Vector2 end = WorldToTexture(points[nextIndex].position, offset, scale);

                DrawLine(texture, start, end, lineColor, 2);
            }

            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Calcule le bounding box de tous les points
        /// </summary>
        private static Bounds CalculateBounds(SplinePoint[] points)
        {
            if (points.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            Vector3 min = points[0].position;
            Vector3 max = points[0].position;

            foreach (var point in points)
            {
                min = Vector3.Min(min, point.position);
                max = Vector3.Max(max, point.position);
            }

            Vector3 center = (min + max) / 2f;
            Vector3 size = max - min;

            return new Bounds(center, size);
        }

        /// <summary>
        /// Convertit une position world en coordonnées texture
        /// </summary>
        private static Vector2 WorldToTexture(Vector3 worldPos, Vector2 offset, float scale)
        {
            return new Vector2(
                worldPos.x * scale + offset.x,
                worldPos.z * scale + offset.y
            );
        }

        /// <summary>
        /// Dessine une ligne sur une texture (algorithme de Bresenham)
        /// </summary>
        private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            int x0 = Mathf.RoundToInt(start.x);
            int y0 = Mathf.RoundToInt(start.y);
            int x1 = Mathf.RoundToInt(end.x);
            int y1 = Mathf.RoundToInt(end.y);

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                // Dessiner le pixel avec épaisseur
                for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                {
                    for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                    {
                        int px = x0 + tx;
                        int py = y0 + ty;
                        
                        if (px >= 0 && px < THUMBNAIL_SIZE && py >= 0 && py < THUMBNAIL_SIZE)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }

    /// <summary>
    /// Custom Inspector pour CircuitData avec bouton de génération de thumbnail
    /// </summary>
    [CustomEditor(typeof(CircuitData))]
    public class CircuitDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Thumbnail Generator", EditorStyles.boldLabel);

            CircuitData circuitData = (CircuitData)target;

            if (GUILayout.Button("Generate Thumbnail", GUILayout.Height(30)))
            {
                CircuitThumbnailGenerator.GenerateThumbnail(circuitData, true);
            }

            EditorGUILayout.HelpBox(
                "Génère automatiquement un sprite 256x256 du tracé du circuit.\n" +
                "Tracé noir sur fond blanc (alpha 0.5).\n" +
                "Sauvegardé dans: Assets/Circuits/Thumbnails/",
                MessageType.Info
            );
        }
    }

    /// <summary>
    /// Menu contextuel pour générer le thumbnail depuis l'asset
    /// </summary>
    public class CircuitThumbnailContextMenu
    {
        [MenuItem("Assets/Generate Circuit Thumbnail", true)]
        private static bool ValidateGenerateThumbnail()
        {
            return Selection.activeObject is CircuitData;
        }

        [MenuItem("Assets/Generate Circuit Thumbnail")]
        private static void GenerateThumbnail()
        {
            CircuitData circuitData = Selection.activeObject as CircuitData;
            if (circuitData != null)
            {
                CircuitThumbnailGenerator.GenerateThumbnail(circuitData, true);
            }
        }
    }
}
