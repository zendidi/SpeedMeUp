using ArcadeRacer.Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.Rendering.STP;

namespace ArcadeRacer.Utilities
{
    /// <summary>
    /// Utilitaire pour la génération procédurale de mesh de piste.
    /// Crée la route et optionnellement les murs latéraux à partir des données de spline.
    /// </summary>
    public static class CircuitMeshGenerator
    {

        public const int DEFAULT_SEGMENTS = 25;
        public const float DEFAULT_CURVE_QUALITY = 5f;
        /// <summary>
        /// Résultat de la génération de mesh.
        /// </summary>
        public struct GenerationResult
        {
            public Mesh roadMesh;
            public Mesh leftWallMesh;
            public Mesh rightWallMesh;
            public Vector3[] leftEdgePoints;
            public Vector3[] rightEdgePoints;
            public bool success;
            public string errorMessage;
        }

        /// <summary>
        /// Configuration de génération.
        /// </summary>
        public struct GenerationConfig
        {
            public int segmentsPerSplinePoint;
            public float uvTilingX;
            public float uvTilingY;
            public bool generateCollider;
            public bool optimizeMesh;
            public float curveQualityMultiplier;

            public static GenerationConfig Default => new GenerationConfig
            {
                segmentsPerSplinePoint = 10,
                uvTilingX = 1f,
                uvTilingY = 0.5f,
                generateCollider = true,
                optimizeMesh = true,
                curveQualityMultiplier = 2f
            };
        }

        /// <summary>
        /// Données d'un checkpoint généré automatiquement.
        /// </summary>
        public struct CheckpointInfo
        {
            public Vector3 position;
            public Quaternion rotation;
            public float width;
            public int index;
        }

        /// <summary>
        /// Génère les meshes de route et murs à partir des données de circuit.
        /// </summary>
        public static GenerationResult Generate(CircuitData circuitData, GenerationConfig config)
        {

            var result = new GenerationResult();

            // Validation
            if (circuitData == null || circuitData.splinePoints == null || circuitData.splinePoints.Length < 2)
            {
                result.success = false;
                result.errorMessage = "Invalid circuit data or insufficient spline points.";
                return result;
            }

            try
            {
                // Générer les points interpolés de la spline
                var interpolatedPoints = InterpolateSpline(
                    circuitData.splinePoints,
                    config.segmentsPerSplinePoint,
                    circuitData.closedLoop,
                    config.curveQualityMultiplier
                );

                // Calculer les edges gauche/droite
                CalculateEdges(
                    interpolatedPoints,
                    circuitData.trackWidth,
                    out result.leftEdgePoints,
                    out result.rightEdgePoints
                );

                // Générer le mesh de route
                result.roadMesh = GenerateRoadMesh(
                    result.leftEdgePoints,
                    result.rightEdgePoints,
                    circuitData.closedLoop,
                    config.uvTilingX,
                    config.uvTilingY
                );
                result.roadMesh.name = $"{circuitData.circuitName}_Road";
                //Debug.Log($"[CircuitMeshGenerator] Mesh créé: {result.roadMesh.name} - Appelé par: {new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name}");
                // Générer les murs si demandé
                if (circuitData.generateWalls)
                {
                    result.leftWallMesh = GenerateWallMesh(
                        result.leftEdgePoints,
                        circuitData.wallHeight,
                        true, // isLeftWall
                        circuitData.closedLoop
                    );
                    result.leftWallMesh.name = $"{circuitData.circuitName}_LeftWall";

                    result.rightWallMesh = GenerateWallMesh(
                        result.rightEdgePoints,
                        circuitData.wallHeight,
                        false, // isLeftWall
                        circuitData.closedLoop
                    );
                    result.rightWallMesh.name = $"{circuitData.circuitName}_RightWall";
                }

                // Optimiser si demandé
                if (config.optimizeMesh)
                {
                    result.roadMesh.Optimize();
                    result.roadMesh.RecalculateBounds();

                    if (result.leftWallMesh != null)
                    {
                        result.leftWallMesh.Optimize();
                        result.leftWallMesh.RecalculateBounds();
                    }

                    if (result.rightWallMesh != null)
                    {
                        result.rightWallMesh.Optimize();
                        result.rightWallMesh.RecalculateBounds();
                    }
                }

                result.success = true;
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.errorMessage = $"Mesh generation failed: {e.Message}";
            }

            return result;
        }

        /// <summary>
        /// Interpole les points de spline avec subdivision adaptative.
        /// Plus de segments dans les virages serrés, moins dans les lignes droites.
        /// </summary>
        private static List<Vector3> InterpolateSpline(SplinePoint[] splinePoints, int segmentsPerPoint, bool closedLoop, float curveQualityMultiplier)
        {
            var interpolatedPoints = new List<Vector3>();

            // Créer une Spline Unity temporaire
            var tempSpline = new UnityEngine.Splines.Spline();

            foreach (var point in splinePoints)
            {
                var knot = new BezierKnot(
                    point.position,
                    point.tangentIn,
                    point.tangentOut,
                    point.rotation
                );
                tempSpline.Add(knot);
            }

            tempSpline.Closed = closedLoop;

            // Subdivision adaptative : Plus de segments dans les courbes
            int totalPoints = 0;

            for (int i = 0; i < splinePoints.Length; i++)
            {
                int nextIndex = (i + 1) % splinePoints.Length;
                if (!closedLoop && nextIndex == 0) break;

                // Calculer la courbure entre deux points
                float curvature = CalculateCurvature(splinePoints[i], splinePoints[nextIndex]);

                // Adapter le nombre de segments selon la courbure
                // Virage serré (curvature élevée) → Plus de segments
                int adaptiveSegments = Mathf.CeilToInt(segmentsPerPoint * (1f + curvature * curveQualityMultiplier));
                adaptiveSegments = Mathf.Clamp(adaptiveSegments, segmentsPerPoint, segmentsPerPoint * 4);

                totalPoints += adaptiveSegments;
            }

            // Évaluer la spline avec la méthode native d'Unity
            float totalLength = tempSpline.GetLength();
            float step = totalLength / totalPoints;


            for (int i = 0; i <= totalPoints; i++)
            {
                float t = i / (float)totalPoints;
                Vector3 position = tempSpline.EvaluatePosition(t);

                // ✅ FORCER Y = 0 pour un circuit 100% plat
                position.y = 0f;

                interpolatedPoints.Add(position);
            }
            return interpolatedPoints;
        }

        /// <summary>
        /// Calcule la courbure approximative entre deux points de spline.
        /// Retourne 0 pour ligne droite, >0 pour virage (plus c'est élevé, plus c'est serré).
        /// </summary>
        private static float CalculateCurvature(SplinePoint p0, SplinePoint p1)
        {
            // Vecteur direct entre les deux points
            Vector3 direct = (p1.position - p0.position).normalized;

            // Direction des tangentes
            Vector3 tangent0 = p0.tangentOut.normalized;
            Vector3 tangent1 = p1.tangentIn.normalized;

            // Calculer l'angle de déviation (0 = ligne droite, 1 = virage à 90°)
            float deviation0 = 1f - Mathf.Max(0f, Vector3.Dot(direct, tangent0));
            float deviation1 = 1f - Mathf.Max(0f, Vector3.Dot(direct, tangent1));

            // Retourner la courbure moyenne
            return (deviation0 + deviation1) * 0.5f;
        }

        /// <summary>
        /// Évalue un point sur une courbe de Bézier cubique entre deux SplinePoints.
        /// </summary>
        private static Vector3 EvaluateBezier(SplinePoint p0, SplinePoint p1, float t)
        {
            // ATTENTION : Unity Splines utilise des tangentes SORTANTES et ENTRANTES
            // p0.tangentOut = direction de la courbe qui SORT de p0
            // p1.tangentIn = direction de la courbe qui ENTRE dans p1

            // Points de contrôle pour Bézier cubique :
            // P0 = position de départ
            // P1 = position de départ + tangente sortante
            // P2 = position d'arrivée + tangente entrante (ATTENTION: c'est déjà un offset !)
            // P3 = position d'arrivée

            Vector3 P0 = p0.position;
            Vector3 P1 = p0.position + p0.tangentOut;  // Sortie de p0
            Vector3 P2 = p1.position + p1.tangentIn;   // Entrée vers p1 (déjà un offset !)
            Vector3 P3 = p1.position;

            // Formule de Bézier cubique : B(t) = (1-t)³P0 + 3(1-t)²tP1 + 3(1-t)t²P2 + t³P3
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 point = uuu * P0;              // (1-t)³ * P0
            point += 3f * uu * t * P1;             // 3(1-t)²t * P1
            point += 3f * u * tt * P2;             // 3(1-t)t² * P2
            point += ttt * P3;                     // t³ * P3

            return point;
        }

        /// <summary>
        /// Calcule les bords gauche et droit de la piste.
        /// </summary>
        private static void CalculateEdges(
            List<Vector3> centerPoints,
            float trackWidth,
            out Vector3[] leftEdge,
            out Vector3[] rightEdge)
        {
            int count = centerPoints.Count;
            leftEdge = new Vector3[count];
            rightEdge = new Vector3[count];

            float halfWidth = trackWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                Vector3 current = centerPoints[i];
                Vector3 forward;

                // Calculer la direction tangente
                if (i == 0)
                {
                    forward = (centerPoints[1] - centerPoints[0]).normalized;
                }
                else if (i == count - 1)
                {
                    forward = (centerPoints[i] - centerPoints[i - 1]).normalized;
                }
                else
                {
                    forward = (centerPoints[i + 1] - centerPoints[i - 1]).normalized;
                }

                // Calculer la normale (perpendiculaire sur le plan horizontal)
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                // Si la normale est nulle (pente verticale), utiliser une alternative
                if (right.sqrMagnitude < 0.001f)
                {
                    right = Vector3.Cross(Vector3.forward, forward).normalized;
                }

                leftEdge[i] = current - right * halfWidth;
                rightEdge[i] = current + right * halfWidth;
            }
        }

        /// <summary>
        /// Génère le mesh de la surface de route.
        /// </summary>
        private static Mesh GenerateRoadMesh(
            Vector3[] leftEdge,
            Vector3[] rightEdge,
            bool closedLoop,
            float uvTilingX,
            float uvTilingY)
        {
            var mesh = new Mesh();

            int segmentCount = leftEdge.Length - 1;
            int vertexCount = leftEdge.Length * 2;
            int triangleCount = segmentCount * 6;

            var vertices = new Vector3[vertexCount];
            var triangles = new int[triangleCount];
            var uvs = new Vector2[vertexCount];
            var normals = new Vector3[vertexCount];

            // Calculer la longueur totale pour les UVs
            float totalLength = 0f;
            var segmentLengths = new float[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 center = (leftEdge[i] + rightEdge[i]) / 2f;
                Vector3 nextCenter = (leftEdge[i + 1] + rightEdge[i + 1]) / 2f;
                segmentLengths[i] = Vector3.Distance(center, nextCenter);
                totalLength += segmentLengths[i];
            }

            // Générer vertices et UVs
            float accumulatedLength = 0f;
            for (int i = 0; i < leftEdge.Length; i++)
            {
                int leftIndex = i * 2;
                int rightIndex = i * 2 + 1;

                vertices[leftIndex] = leftEdge[i];
                vertices[rightIndex] = rightEdge[i];

                // UVs basés sur la distance parcourue
                float v = (accumulatedLength / totalLength) * uvTilingY * segmentCount;
                uvs[leftIndex] = new Vector2(0, v);
                uvs[rightIndex] = new Vector2(uvTilingX, v);

                // Normales vers le haut
                normals[leftIndex] = Vector3.up;
                normals[rightIndex] = Vector3.up;

                if (i < segmentCount)
                {
                    accumulatedLength += segmentLengths[i];
                }
            }

            // Générer triangles
            int triIndex = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                int bl = i * 2;      // Bottom left
                int br = i * 2 + 1;  // Bottom right
                int tl = (i + 1) * 2;    // Top left
                int tr = (i + 1) * 2 + 1; // Top right

                // Triangle 1
                triangles[triIndex++] = bl;
                triangles[triIndex++] = tl;
                triangles[triIndex++] = br;

                // Triangle 2
                triangles[triIndex++] = br;
                triangles[triIndex++] = tl;
                triangles[triIndex++] = tr;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.RecalculateTangents();

            return mesh;
        }

        /// <summary>
        /// Génère le mesh d'un mur latéral.
        /// </summary>
        private static Mesh GenerateWallMesh(
            Vector3[] edgePoints,
            float wallHeight,
            bool isLeftWall,
            bool closedLoop)
        {
            var mesh = new Mesh();

            int segmentCount = edgePoints.Length - 1;
            int vertexCount = edgePoints.Length * 2;
            int triangleCount = segmentCount * 6;

            var vertices = new Vector3[vertexCount];
            var triangles = new int[triangleCount];
            var uvs = new Vector2[vertexCount];
            var normals = new Vector3[vertexCount];

            // Générer vertices
            for (int i = 0; i < edgePoints.Length; i++)
            {
                int bottomIndex = i * 2;
                int topIndex = i * 2 + 1;

                vertices[bottomIndex] = edgePoints[i];
                vertices[topIndex] = edgePoints[i] + Vector3.up * wallHeight;

                // UVs
                float u = i / (float)segmentCount;
                uvs[bottomIndex] = new Vector2(u * segmentCount * 0.5f, 0);
                uvs[topIndex] = new Vector2(u * segmentCount * 0.5f, 1);
            }

            // Calculer normales et triangles
            for (int i = 0; i < segmentCount; i++)
            {
                int bl = i * 2;
                int br = (i + 1) * 2;
                int tl = i * 2 + 1;
                int tr = (i + 1) * 2 + 1;

                // Calculer normale du segment
                Vector3 forward = (edgePoints[i + 1] - edgePoints[i]).normalized;
                Vector3 normal = isLeftWall
                    ? Vector3.Cross(Vector3.up, forward).normalized
                    : Vector3.Cross(forward, Vector3.up).normalized;

                normals[bl] = normal;
                normals[br] = normal;
                normals[tl] = normal;
                normals[tr] = normal;

                int triIndex = i * 6;

                if (isLeftWall)
                {
                    // Face vers l'intérieur (droite)
                    triangles[triIndex] = bl;
                    triangles[triIndex + 1] = tl;
                    triangles[triIndex + 2] = br;
                    triangles[triIndex + 3] = br;
                    triangles[triIndex + 4] = tl;
                    triangles[triIndex + 5] = tr;
                }
                else
                {
                    // Face vers l'intérieur (gauche)
                    triangles[triIndex] = bl;
                    triangles[triIndex + 1] = br;
                    triangles[triIndex + 2] = tl;
                    triangles[triIndex + 3] = tl;
                    triangles[triIndex + 4] = br;
                    triangles[triIndex + 5] = tr;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.RecalculateTangents();

            return mesh;
        }

        /// <summary>
        /// Génère les données de checkpoint automatiquement le long de la spline.
        /// Retourne des CheckpointInfo au lieu de CheckpointData.
        /// </summary>
        public static CheckpointInfo[] GenerateAutoCheckpoints(
            CircuitData circuitData,
            int checkpointCount)
        {
            if (circuitData.splinePoints == null || circuitData.splinePoints.Length < 2)
                return new CheckpointInfo[0];

            var checkpoints = new CheckpointInfo[checkpointCount];
           var config = GenerationConfig.Default;
            // Interpoler la spline
            var points = InterpolateSpline(circuitData.splinePoints, 10, circuitData.closedLoop, config.curveQualityMultiplier);

            // Distribuer uniformément
            float step = (points.Count - 1) / (float)checkpointCount;

            for (int i = 0; i < checkpointCount; i++)
            {
                int index = Mathf.RoundToInt(i * step);
                index = Mathf.Clamp(index, 0, points.Count - 2);

                Vector3 position = points[index];
                Vector3 forward = (points[index + 1] - points[index]).normalized;

                // Rotation face à la direction de passage
                Quaternion rotation = forward != Vector3.zero
                    ? Quaternion.LookRotation(forward, Vector3.up)
                    : Quaternion.identity;

                checkpoints[i] = new CheckpointInfo
                {
                    position = position,
                    rotation = rotation,
                    width = circuitData.trackWidth + 2f,
                    index = i
                };
            }

            return checkpoints;
        }

        /// <summary>
        /// Calcule les bounds du circuit pour la minimap.
        /// </summary>
        public static Bounds CalculateCircuitBounds(CircuitData circuitData)
        {
            if (circuitData.splinePoints == null || circuitData.splinePoints.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one * 100);

            Vector3 min = circuitData.splinePoints[0].position;
            Vector3 max = circuitData.splinePoints[0].position;

            foreach (var point in circuitData.splinePoints)
            {
                min = Vector3.Min(min, point.position);
                max = Vector3.Max(max, point.position);
            }

            // Ajouter la largeur de piste
            float padding = circuitData.trackWidth;
            min -= new Vector3(padding, 0, padding);
            max += new Vector3(padding, 0, padding);

            return new Bounds((min + max) / 2f, max - min);
        }
    }
}