using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using ArcadeRacer.Settings;
using ArcadeRacer.Utilities;
using Unity.Mathematics;

namespace ArcadeRacer.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Outil éditeur pour créer et exporter des circuits.
    /// Convertit une SplineContainer en CircuitData.asset.
    /// </summary>
    [ExecuteInEditMode]
    public class CircuitBuilder : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("=== CIRCUIT DATA ===")]

        [SerializeField]
        [Tooltip("CircuitData asset à remplir avec les données de la spline")]
        private CircuitData circuitData;

        // ⬇️ NOUVEAU : Pour créer un nouveau CircuitData
        [SerializeField]
        [Tooltip("Nom pour un NOUVEAU circuit (si circuitData est vide)")]
        private string newCircuitName = "";

        [Header("=== SPLINE SOURCE ===")]
        [SerializeField]
        [Tooltip("SplineContainer contenant la trajectoire du circuit")]
        private SplineContainer splineContainer;
        
        [Header("=== SPAWN POINT ===")]
        [SerializeField]
        [Tooltip("Transform définissant le point de spawn (optionnel, sinon = début de spline)")]
        private Transform spawnPoint;
        
        [Header("=== PREVIEW SETTINGS ===")]
        [SerializeField]
        private bool showPreview = true;
        
        [SerializeField]
        private bool showSplinePoints = true;
        
        [SerializeField]
        private bool showCheckpoints = true;
        
        [SerializeField]
        private bool showSpawnPoint = true;
        
        [SerializeField]
        [Range(5, 50)]
        private int previewSegmentsPerPoint = CircuitMeshGenerator.DEFAULT_SEGMENTS;

        [Header("=== PREVIEW OBJECTS ===")]
        [SerializeField]
        private GameObject previewRoadObject;
        
        [SerializeField]
        private GameObject previewWallsObject;
        
        [SerializeField]
        private GameObject previewCheckpointsObject;

        [Header("=== DÉCOR ===")]
        [SerializeField]
        [Tooltip("Container racine pour les objets de décor. " +
                 "Doit être placé à Vector3.zero de la scène (créé automatiquement).")]
        private GameObject decorContainer;

        [SerializeField]
        [Tooltip("Graine aléatoire pour la génération automatique du décor. " +
                 "Changez-la pour obtenir un placement différent.")]
        private int autoDecorSeed = 42;

        #endregion
        
        #region Mode Detection
        
        /// <summary>
        /// Modes de fonctionnement du CircuitBuilder.
        /// </summary>
        public enum CircuitBuilderMode
        {
            None,       // Pas de CircuitData assigné
            Creation,   // CircuitData vide (nouveau circuit)
            Edition     // CircuitData avec données existantes
        }
        
        /// <summary>
        /// Détecte automatiquement le mode actuel.
        /// </summary>
        public CircuitBuilderMode GetCurrentMode()
        {
            if (circuitData == null)
                return CircuitBuilderMode.None;
            
            if (circuitData.splinePoints == null || circuitData.splinePoints.Length == 0)
                return CircuitBuilderMode.Creation;
            
            return CircuitBuilderMode.Edition;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnValidate()
        {
            // Auto-detect SplineContainer si pas assigné
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }
            
            // Auto-detect SpawnPoint si pas assigné
            if (spawnPoint == null)
            {
                Transform child = transform.Find("SpawnPoint");
                if (child != null)
                {
                    spawnPoint = child;
                }
            }

            // Auto-detect DecorContainer si pas assigné
            if (decorContainer == null)
            {
                Transform child = transform.Find(DECOR_CONTAINER_NAME);
                if (child != null)
                {
                    decorContainer = child.gameObject;
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showPreview) return;
            
            DrawSplinePreview();
            DrawCheckpointsPreview();
            DrawSpawnPointPreview();
        }
        
        #endregion
        
        #region Public Methods (appelés par les boutons Inspector)
        
        /// <summary>
        /// Exporte la spline vers le CircuitData asset.
        /// </summary>
        public void ExportToCircuitData()
        {
            if (!ValidateBeforeExport(out string errorMessage))
            {
                EditorUtility.DisplayDialog("Export Failed", errorMessage, "OK");
                return;
            }
            
            // Convertir SplineContainer → SplinePoint[]
            var splinePoints = ConvertSplineToPoints(splineContainer);
            circuitData.splinePoints = splinePoints;
            
            // Définir le spawn point
            if (spawnPoint != null)
            {
                circuitData.spawnPosition = spawnPoint.position;
                circuitData.spawnRotation = spawnPoint.rotation;
            }
            else
            {
                // Utiliser le début de la spline
                var firstPoint = splinePoints[0];
                circuitData.spawnPosition = firstPoint.position;
                
                // Calculer rotation basée sur la tangente
                if (splinePoints.Length > 1)
                {
                    Vector3 forward = (splinePoints[1].position - splinePoints[0].position).normalized;
                    circuitData.spawnRotation = Quaternion.LookRotation(forward, Vector3.up);
                }
            }
            
            // Calculer la longueur totale
            circuitData.CalculateTotalLength();
            
            // Marquer comme modifié
            EditorUtility.SetDirty(circuitData);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[CircuitBuilder] Circuit exporté avec succès !\n" +
                      $"  - {splinePoints.Length} points de spline\n" +
                      $"  - Longueur totale : {circuitData.TotalLength:F1}m");
            
            EditorUtility.DisplayDialog("Export Success", 
                $"Circuit '{circuitData.circuitName}' exporté avec succès !\n\n" +
                $"Points: {splinePoints.Length}\n" +
                $"Longueur: {circuitData.TotalLength:F1}m", 
                "OK");
        }
        
        /// <summary>
        /// Génère un aperçu du mesh sans sauvegarder.
        /// </summary>
        public void GeneratePreview()
        {
            if (!ValidateBeforeExport(out string errorMessage))
            {
                EditorUtility.DisplayDialog("Preview Failed", errorMessage, "OK");
                return;
            }
            
            // Nettoyer l'ancien aperçu
            ClearPreview();
            
            // Créer un CircuitData temporaire
            var tempCircuitData = ScriptableObject.CreateInstance<CircuitData>();
            tempCircuitData.splinePoints = ConvertSplineToPoints(splineContainer);
            tempCircuitData.trackWidth = circuitData.trackWidth;
            tempCircuitData.wallHeight = circuitData.wallHeight;
            tempCircuitData.generateWalls = circuitData.generateWalls;
            tempCircuitData.closedLoop = circuitData.closedLoop;
            tempCircuitData.circuitName = "Preview";

            // Générer le mesh avec configuration unifiée
            // GARANTIE: Même résultat que le runtime
            var config = CircuitGenerationConstants.EditorConfig; 
            
            var result = CircuitMeshGenerator.Generate(tempCircuitData, config);
            
            if (!result.success)
            {
                EditorUtility.DisplayDialog("Preview Failed", result.errorMessage, "OK");
                return;
            }
            
            // Créer les GameObjects de preview
            previewRoadObject = new GameObject("PREVIEW_Road");
            previewRoadObject.transform.SetParent(transform);
            previewRoadObject.hideFlags = HideFlags.DontSave;
            
            var roadFilter = previewRoadObject.AddComponent<MeshFilter>();
            roadFilter.sharedMesh = result.roadMesh;
            
            var roadRenderer = previewRoadObject.AddComponent<MeshRenderer>();
            roadRenderer.sharedMaterial = circuitData.roadMaterial != null 
                ? circuitData.roadMaterial 
                : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            
            // Murs
            if (circuitData.generateWalls && result.leftWallMesh != null)
            {
                previewWallsObject = new GameObject("PREVIEW_Walls");
                previewWallsObject.transform.SetParent(transform);
                previewWallsObject.hideFlags = HideFlags.DontSave;
                
                var leftWall = new GameObject("LeftWall");
                leftWall.transform.SetParent(previewWallsObject.transform);
                var leftFilter = leftWall.AddComponent<MeshFilter>();
                leftFilter.sharedMesh = result.leftWallMesh;
                var leftRenderer = leftWall.AddComponent<MeshRenderer>();
                leftRenderer.sharedMaterial = circuitData.wallMaterial != null 
                    ? circuitData.wallMaterial 
                    : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                
                var rightWall = new GameObject("RightWall");
                rightWall.transform.SetParent(previewWallsObject.transform);
                var rightFilter = rightWall.AddComponent<MeshFilter>();
                rightFilter.sharedMesh = result.rightWallMesh;
                var rightRenderer = rightWall.AddComponent<MeshRenderer>();
                rightRenderer.sharedMaterial = circuitData.wallMaterial != null 
                    ? circuitData.wallMaterial 
                    : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }
            
            Debug.Log("[CircuitBuilder] Preview généré avec succès !");
        }
        
        /// <summary>
        /// Nettoie l'aperçu.
        /// </summary>
        public void ClearPreview()
        {
            if (previewRoadObject != null)
            {
                DestroyImmediate(previewRoadObject);
                previewRoadObject = null;
            }
            
            if (previewWallsObject != null)
            {
                DestroyImmediate(previewWallsObject);
                previewWallsObject = null;
            }
            
            ClearCheckpointPreview();
            
            Debug.Log("[CircuitBuilder] Preview nettoyé.");
        }
        
        /// <summary>
        /// Crée automatiquement un SpawnPoint enfant.
        /// </summary>
        public void CreateSpawnPoint()
        {
            if (spawnPoint != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "SpawnPoint existe déjà", 
                    "Un SpawnPoint existe déjà. Le remplacer ?", 
                    "Oui", 
                    "Non"
                );
                
                if (!replace) return;
                
                DestroyImmediate(spawnPoint.gameObject);
            }
            
            GameObject spawnGO = new GameObject("SpawnPoint");
            spawnGO.transform.SetParent(transform);
            
            // Placer au début de la spline
            if (splineContainer != null && splineContainer.Spline.Count > 0)
            {
                splineContainer.Evaluate(0, out float3 position, out float3 tangent, out float3 up);
                spawnGO.transform.position = position;
                spawnGO.transform.rotation = Quaternion.LookRotation(tangent, up);
            }
            
            spawnPoint = spawnGO.transform;
            
            Debug.Log("[CircuitBuilder] SpawnPoint créé au début de la spline.");
        }

        /// <summary>
        /// Crée un nouveau CircuitData asset et l'assigne automatiquement.
        /// </summary>
        public void CreateNewCircuitData()
        {
            if (string.IsNullOrWhiteSpace(newCircuitName))
            {
                EditorUtility.DisplayDialog("Erreur",
                    "Veuillez entrer un nom pour le nouveau circuit !",
                    "OK");
                return;
            }

            // Vérifier les doublons
            string assetPath = $"Assets/Project/Settings/Circuits/{newCircuitName}.asset";

            if (AssetDatabase.LoadAssetAtPath<CircuitData>(assetPath) != null)
            {
                EditorUtility.DisplayDialog("Erreur",
                    $"Un circuit nommé '{newCircuitName}' existe déjà !\n\n" +
                    "Choisissez un autre nom ou chargez le circuit existant.",
                    "OK");
                return;
            }

            // Créer les dossiers si nécessaire
            if (!AssetDatabase.IsValidFolder("Assets/Project/Settings/Circuits"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Project/Settings"))
                {
                    AssetDatabase.CreateFolder("Assets/Project", "Settings");
                }
                AssetDatabase.CreateFolder("Assets/Project/Settings", "Circuits");
            }

            // Créer le CircuitData
            var newData = ScriptableObject.CreateInstance<CircuitData>();
            newData.circuitName = newCircuitName;
            newData.trackWidth = 10f;
            newData.closedLoop = true;
            newData.autoCheckpointCount = 10;
            newData.targetLapCount = 3;

            AssetDatabase.CreateAsset(newData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Assigner automatiquement
            circuitData = newData;

            // Effacer le décor du circuit précédent
            ClearDecor();

            // Nettoyer le champ de nom
            newCircuitName = "";

            Debug.Log($"[CircuitBuilder] CircuitData créé : {assetPath}");
            EditorUtility.DisplayDialog("Succès",
                $"Circuit '{newData.circuitName}' créé et assigné !\n\n" +
                "Vous pouvez maintenant éditer votre spline et exporter.",
                "OK");
        }
        
        /// <summary>
        /// Charge un CircuitData existant dans la spline pour édition.
        /// MODE ÉDITION: Reconstruit la spline depuis CircuitData.splinePoints.
        /// </summary>
        public void LoadCircuitDataIntoSpline()
        {
            if (circuitData == null)
            {
                EditorUtility.DisplayDialog("Erreur", 
                    "Aucun CircuitData assigné !", 
                    "OK");
                return;
            }
            
            if (circuitData.splinePoints == null || circuitData.splinePoints.Length == 0)
            {
                EditorUtility.DisplayDialog("Erreur", 
                    "CircuitData ne contient aucun point de spline !\n\n" +
                    "Ce circuit n'a pas encore été exporté.", 
                    "OK");
                return;
            }
            
            // Vérifier si une spline existe déjà
            if (splineContainer != null && splineContainer.Spline != null && splineContainer.Spline.Count > 0)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Spline existante détectée",
                    "Une spline existe déjà. La remplacer par les données du CircuitData ?\n\n" +
                    "Cette action ne peut pas être annulée.",
                    "Oui, charger",
                    "Annuler"
                );
                
                if (!confirm) return;
            }
            
            // S'assurer qu'on a un SplineContainer
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
                if (splineContainer == null)
                {
                    splineContainer = gameObject.AddComponent<SplineContainer>();
                }
            }
            
            // Reconstruire la spline depuis CircuitData
            var spline = splineContainer.Spline;
            spline.Clear();
            
            foreach (var point in circuitData.splinePoints)
            {
                // Convertir world position → local position
                Vector3 localPos = splineContainer.transform.InverseTransformPoint(point.position);
                
                // Convertir world tangents → local tangents
                Vector3 tangentInLocal = splineContainer.transform.InverseTransformDirection(point.tangentIn);
                Vector3 tangentOutLocal = splineContainer.transform.InverseTransformDirection(point.tangentOut);
                
                var knot = new BezierKnot(
                    localPos,
                    tangentInLocal,
                    tangentOutLocal,
                    point.rotation
                );
                
                spline.Add(knot, TangentMode.Mirrored);
            }
            
            spline.Closed = circuitData.closedLoop;
            
            // Repositionner ou créer le spawn point
            if (spawnPoint == null)
            {
                GameObject spawnGO = new GameObject("SpawnPoint");
                spawnGO.transform.SetParent(transform);
                spawnPoint = spawnGO.transform;
            }
            
            spawnPoint.position = circuitData.spawnPosition;
            spawnPoint.rotation = circuitData.spawnRotation;

            // Charger le décor du circuit (remplace l'ancien décor)
            LoadDecorFromCircuitData();
            
            Debug.Log($"[CircuitBuilder] Circuit '{circuitData.circuitName}' chargé dans l'éditeur !\n" +
                      $"  - {circuitData.splinePoints.Length} points de spline\n" +
                      $"  - Longueur: {circuitData.TotalLength:F1}m");
            
            EditorUtility.DisplayDialog("Succès",
                $"Circuit '{circuitData.circuitName}' chargé !\n\n" +
                $"Points: {circuitData.splinePoints.Length}\n" +
                $"Longueur: {circuitData.TotalLength:F1}m\n\n" +
                "Vous pouvez maintenant modifier la spline.",
                "OK");
        }
        
        /// <summary>
        /// Génère un aperçu visuel des checkpoints dans l'éditeur.
        /// </summary>
        public void GenerateCheckpointPreview()
        {
            if (circuitData == null)
            {
                EditorUtility.DisplayDialog("Erreur", 
                    "Aucun CircuitData assigné !", 
                    "OK");
                return;
            }
            
            // Nettoyer les anciens checkpoints preview
            ClearCheckpointPreview();
            
            // Créer un CircuitData temporaire avec les points actuels
            var tempData = ScriptableObject.CreateInstance<CircuitData>();
            tempData.splinePoints = ConvertSplineToPoints(splineContainer);
            tempData.trackWidth = circuitData.trackWidth;
            tempData.closedLoop = circuitData.closedLoop;
            tempData.autoCheckpointCount = circuitData.autoCheckpointCount;
            
            // Générer les checkpoints
            var checkpoints = CircuitMeshGenerator.GenerateAutoCheckpoints(
                tempData,
                circuitData.autoCheckpointCount
            );
            
            // Créer un parent pour organiser
            previewCheckpointsObject = new GameObject("PREVIEW_Checkpoints");
            previewCheckpointsObject.transform.SetParent(transform);
            previewCheckpointsObject.hideFlags = HideFlags.DontSave;
            
            // Créer les GameObjects de checkpoints
            for (int i = 0; i < checkpoints.Length; i++)
            {
                var cp = checkpoints[i];
                
                GameObject cpGO = new GameObject($"Checkpoint_{i}");
                cpGO.transform.SetParent(previewCheckpointsObject.transform);
                cpGO.transform.position = cp.position;
                cpGO.transform.rotation = cp.rotation;
                
                // Ajouter un BoxCollider pour visualisation (pas de trigger)
                var collider = cpGO.AddComponent<BoxCollider>();
                collider.isTrigger = false;
                collider.size = new Vector3(cp.width, 5f, 0.5f);
                
                // Tag pour identification
                cpGO.tag = "EditorOnly";
            }
            
            Debug.Log($"[CircuitBuilder] {checkpoints.Length} checkpoints preview générés !");
            EditorUtility.DisplayDialog("Succès",
                $"{checkpoints.Length} checkpoints générés !\n\n" +
                "Vous pouvez les ajuster manuellement dans la scène,\n" +
                "puis cliquer 'Save Checkpoints' pour sauvegarder.",
                "OK");
        }
        
        /// <summary>
        /// Sauvegarde les positions des checkpoints dans CircuitData.
        /// </summary>
        public void SaveCheckpointsToCircuitData()
        {
            if (circuitData == null)
            {
                EditorUtility.DisplayDialog("Erreur", 
                    "Aucun CircuitData assigné !", 
                    "OK");
                return;
            }
            
            if (spawnPoint == null)
            {
                EditorUtility.DisplayDialog("Erreur", 
                    "SpawnPoint manquant !\n\n" +
                    "Le spawn point est nécessaire comme référence pour les positions relatives.",
                    "OK");
                return;
            }
            
            // Trouver tous les checkpoints dans la scène
            var checkpointObjects = new System.Collections.Generic.List<Transform>();
            
            if (previewCheckpointsObject != null)
            {
                // Chercher dans le container de preview
                for (int i = 0; i < previewCheckpointsObject.transform.childCount; i++)
                {
                    checkpointObjects.Add(previewCheckpointsObject.transform.GetChild(i));
                }
            }
            else
            {
                // Chercher manuellement les GameObjects "Checkpoint_X"
                foreach (Transform child in transform)
                {
                    if (child.name.StartsWith("Checkpoint_"))
                    {
                        checkpointObjects.Add(child);
                    }
                }
            }
            
            if (checkpointObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Erreur",
                    "Aucun checkpoint trouvé !\n\n" +
                    "Générez d'abord les checkpoints avec 'Generate Checkpoint Preview'.",
                    "OK");
                return;
            }
            
            // Trier par nom (Checkpoint_0, Checkpoint_1, etc.)
            checkpointObjects.Sort((a, b) => a.name.CompareTo(b.name));
            
            // Convertir en CheckpointData[]
            var checkpointDataList = new System.Collections.Generic.List<CheckpointData>();
            
            for (int i = 0; i < checkpointObjects.Count; i++)
            {
                var cpTransform = checkpointObjects[i];
                
                var cpData = CheckpointData.CreateRelativeToSpawn(
                    cpTransform.position,
                    cpTransform.rotation,
                    spawnPoint.position,
                    spawnPoint.rotation,
                    i,
                    i == 0  // Premier checkpoint = start/finish
                );
                
                checkpointDataList.Add(cpData);
            }
            
            // Sauvegarder dans CircuitData
            circuitData.checkpointData = checkpointDataList.ToArray();
            
            EditorUtility.SetDirty(circuitData);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[CircuitBuilder] {checkpointDataList.Count} checkpoints sauvegardés dans {circuitData.name}");
            EditorUtility.DisplayDialog("Succès",
                $"{checkpointDataList.Count} checkpoints sauvegardés !\n\n" +
                $"Fichier: {AssetDatabase.GetAssetPath(circuitData)}",
                "OK");
        }
        
        /// <summary>
        /// Nettoie le preview des checkpoints.
        /// </summary>
        public void ClearCheckpointPreview()
        {
            if (previewCheckpointsObject != null)
            {
                DestroyImmediate(previewCheckpointsObject);
                previewCheckpointsObject = null;
            }
        }

        /// <summary>
        /// Valide le circuit et le marque comme roulable s'il passe toutes les vérifications.
        /// Un circuit roulable apparaîtra dans la sélection de circuit en jeu.
        /// </summary>
        public void ValidateAndMarkAsRaceable()
        {
            if (circuitData == null)
            {
                EditorUtility.DisplayDialog("Erreur",
                    "Aucun CircuitData assigné !",
                    "OK");
                return;
            }

            bool hasSplinePoints = circuitData.splinePoints != null && circuitData.splinePoints.Length >= 3;
            bool hasCheckpoints = circuitData.checkpointData != null && circuitData.checkpointData.Length > 0;

            if (!hasSplinePoints)
            {
                EditorUtility.DisplayDialog("Validation échouée",
                    "Le circuit n'a pas assez de points de spline (minimum 3).\n\n" +
                    "Veuillez d'abord exporter la spline via 'Export to CircuitData'.",
                    "OK");
                return;
            }

            if (!hasCheckpoints)
            {
                bool markAnyway = EditorUtility.DisplayDialog("Checkpoints manquants",
                    "Le circuit n'a pas de checkpoints sauvegardés.\n\n" +
                    "Il est recommandé de sauvegarder les checkpoints avant de marquer le circuit comme roulable.\n\n" +
                    "Voulez-vous quand même le marquer comme roulable ?",
                    "Oui, marquer quand même",
                    "Non, annuler");
                if (!markAnyway) return;
            }

            circuitData.isRaceable = true;
            EditorUtility.SetDirty(circuitData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CircuitBuilder] Circuit '{circuitData.circuitName}' marqué comme ROULABLE ✅");
            EditorUtility.DisplayDialog("Circuit roulable !",
                $"✅ Le circuit '{circuitData.circuitName}' est maintenant marqué comme ROULABLE.\n\n" +
                $"Il apparaîtra dans la sélection de circuit en jeu.",
                "OK");
        }

        /// <summary>
        /// Marque le circuit comme NON roulable (retiré de la sélection en jeu).
        /// </summary>
        public void MarkAsNotRaceable()
        {
            if (circuitData == null)
            {
                EditorUtility.DisplayDialog("Erreur",
                    "Aucun CircuitData assigné !",
                    "OK");
                return;
            }

            circuitData.isRaceable = false;
            EditorUtility.SetDirty(circuitData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CircuitBuilder] Circuit '{circuitData.circuitName}' marqué comme NON ROULABLE ❌");
            EditorUtility.DisplayDialog("Circuit retiré",
                $"❌ Le circuit '{circuitData.circuitName}' est maintenant marqué comme NON ROULABLE.\n\n" +
                $"Il n'apparaîtra plus dans la sélection de circuit en jeu.",
                "OK");
        }

        // ─────────────────────────────────────────────────────────────────────
        #region Gestion Décor

        private const string DECOR_CONTAINER_NAME = "Decor";

        /// <summary>
        /// Renvoie le container de décor, le crée s'il n'existe pas encore.
        /// Le container est un GO enfant du CircuitBuilder, positionné à (0,0,0) local
        /// (= position monde 0 si le CircuitBuilder est lui-même à l'origine).
        /// </summary>
        private GameObject GetOrCreateDecorContainer()
        {
            if (decorContainer != null) return decorContainer;

            // Chercher parmi les enfants du CircuitBuilder
            Transform found = transform.Find(DECOR_CONTAINER_NAME);
            if (found != null)
            {
                decorContainer = found.gameObject;
                return decorContainer;
            }

            // Créer le container
            decorContainer = new GameObject(DECOR_CONTAINER_NAME);
            decorContainer.transform.SetParent(transform);
            decorContainer.transform.localPosition = Vector3.zero;
            decorContainer.transform.localRotation = Quaternion.identity;
            decorContainer.transform.localScale = Vector3.one;

            Debug.Log("[CircuitBuilder] Container de décor créé.");
            return decorContainer;
        }

        /// <summary>
        /// Supprime tous les objets de décor de la scène (vide le container).
        /// Appeler avant de charger un nouveau circuit ou de régénérer le décor.
        /// </summary>
        public void ClearDecor()
        {
            var container = GetOrCreateDecorContainer();
            int count = container.transform.childCount;

            for (int i = count - 1; i >= 0; i--)
            {
                DestroyImmediate(container.transform.GetChild(i).gameObject);
            }

            if (count > 0)
                Debug.Log($"[CircuitBuilder] {count} objet(s) de décor supprimés.");
        }

        /// <summary>
        /// Sauvegarde la totalité du décor présent dans le container dans CircuitData.
        ///
        /// Pourquoi positions MONDE (et non relatives au spawn) ?
        ///   • Le décor est une propriété de la géométrie du circuit, pas du point de départ.
        ///   • Le container est toujours à Vector3.zero → position locale = position monde.
        ///   • Si le spawn est déplacé, le décor ne bouge pas (comportement souhaité).
        ///   • Cohérence parfaite éditeur ↔ runtime (les deux recréent le container à l'origine).
        /// </summary>
        public void SaveDecorToCircuitData()
        {
            if (circuitData == null)
            {
                EditorUtility.DisplayDialog("Erreur", "Aucun CircuitData assigné !", "OK");
                return;
            }

            var container = GetOrCreateDecorContainer();
            int count = container.transform.childCount;

            var list = new System.Collections.Generic.List<DecorObjectData>(count);

            for (int i = 0; i < count; i++)
            {
                Transform child = container.transform.GetChild(i);

                PrimitiveType prim = GetPrimitiveTypeFromName(child.name);

                Color col = Color.white;
                var rend = child.GetComponent<MeshRenderer>();
                if (rend != null && rend.sharedMaterial != null)
                    col = rend.sharedMaterial.color;

                list.Add(new DecorObjectData
                {
                    primitiveType = prim,
                    position      = child.position,
                    rotation      = child.rotation,
                    scale         = child.localScale,
                    color         = col
                });
            }

            circuitData.decorObjects = list.ToArray();
            EditorUtility.SetDirty(circuitData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CircuitBuilder] {count} objet(s) de décor sauvegardés dans '{circuitData.circuitName}'.");
            EditorUtility.DisplayDialog("Décor sauvegardé",
                $"✅ {count} objet(s) de décor sauvegardés dans '{circuitData.circuitName}'.",
                "OK");
        }

        /// <summary>
        /// Charge le décor sauvegardé depuis CircuitData dans la scène d'édition.
        /// Efface d'abord le décor précédent (circuit précédemment ouvert).
        /// </summary>
        public void LoadDecorFromCircuitData()
        {
            if (circuitData == null) return;

            ClearDecor();

            if (circuitData.decorObjects == null || circuitData.decorObjects.Length == 0)
            {
                Debug.Log($"[CircuitBuilder] Pas de décor sauvegardé pour '{circuitData.circuitName}'.");
                return;
            }

            var container = GetOrCreateDecorContainer();

            Color[] palette = circuitData.decorPalette;
            var matCache = new System.Collections.Generic.Dictionary<Color, Material>();
            for (int i = 0; i < circuitData.decorObjects.Length; i++)
            {
                var data  = circuitData.decorObjects[i];
                Color col = ResolveDecorColor(i, data.color, palette);
                CreateDecorGameObject(container, data.primitiveType, data.position,
                    data.rotation, data.scale, col, i, matCache);
            }

            Debug.Log($"[CircuitBuilder] {circuitData.decorObjects.Length} objet(s) de décor chargés depuis '{circuitData.circuitName}'.");
        }

        /// <summary>
        /// Génère automatiquement le décor autour du circuit.
        /// Efface l'ancien décor, puis crée trois couches d'objets :
        ///   • Tier 1 – Pylônes de vitesse : petits cylindres fins très proches du bord
        ///              (effet parallaxe fort → sensation de vitesse élevée)
        ///   • Tier 2 – Marqueurs de bord  : cubes/capsules à 4-10 m du bord
        ///   • Tier 3 – Structures de fond : grands cubes (bâtiments) et cylindres (arbres/pylônes)
        ///              à 15-40 m, sur les deux côtés
        /// </summary>
        public void GenerateAutoDecor()
        {
            if (circuitData == null)
            {
                EditorUtility.DisplayDialog("Erreur", "Aucun CircuitData assigné !", "OK");
                return;
            }

            // Obtenir les points de spline (spline en cours ou points exportés)
            SplinePoint[] splinePoints = null;
            if (splineContainer != null && splineContainer.Spline != null && splineContainer.Spline.Count >= 3)
            {
                splinePoints = ConvertSplineToPoints(splineContainer);
            }
            else if (circuitData.splinePoints != null && circuitData.splinePoints.Length >= 3)
            {
                splinePoints = circuitData.splinePoints;
            }

            if (splinePoints == null || splinePoints.Length < 3)
            {
                EditorUtility.DisplayDialog("Erreur",
                    "Pas assez de points de spline.\n\n" +
                    "Éditez la spline ou exportez-la d'abord via 'Export to CircuitData'.",
                    "OK");
                return;
            }

            // Générer les bords gauche/droite via CircuitMeshGenerator
            var tempData = ScriptableObject.CreateInstance<CircuitData>();
            tempData.splinePoints   = splinePoints;
            tempData.trackWidth     = circuitData.trackWidth;
            tempData.closedLoop     = circuitData.closedLoop;
            tempData.generateWalls  = false;

            var config = CircuitGenerationConstants.EditorConfig;
            var result = CircuitMeshGenerator.Generate(tempData, config);
            DestroyImmediate(tempData);

            if (!result.success)
            {
                EditorUtility.DisplayDialog("Erreur",
                    $"Impossible de générer les bords du circuit : {result.errorMessage}", "OK");
                return;
            }

            // Effacer l'ancien décor, puis recréer
            ClearDecor();
            var container = GetOrCreateDecorContainer();

            Vector3[] leftEdge  = result.leftEdgePoints;
            Vector3[] rightEdge = result.rightEdgePoints;

            UnityEngine.Random.InitState(autoDecorSeed);
            int totalObjects = 0;
            var matCache = new System.Collections.Generic.Dictionary<Color, Material>();

            // ── TIER 1 : Pylônes de vitesse ──────────────────────────────────────
            // Cylindres fins placés à 0.5-2 m du bord de route, toutes les ~10 m.
            // L'effet de parallaxe intense à courte distance maximise la sensation de vitesse.
            float tier1Spacing = 10f;
            float accDist1 = 0f;
            float trackHW = (circuitData != null ? circuitData.trackWidth : 10f) * 0.5f;

            for (int i = 0; i < leftEdge.Length - 1; i++)
            {
                float seg = Vector3.Distance(leftEdge[i], leftEdge[i + 1]);
                accDist1 += seg;
                if (accDist1 < tier1Spacing) continue;
                accDist1 = 0f;

                Vector3 fwd      = (leftEdge[i + 1] - leftEdge[i]).normalized;
                Vector3 rightDir = Vector3.Cross(Vector3.up, fwd).normalized;

                // Côté gauche
                float offsetL  = UnityEngine.Random.Range(0.5f, 2f);
                Vector3 posL   = leftEdge[i] - rightDir * offsetL;
                float hL       = UnityEngine.Random.Range(0.6f, 1.4f);
                var scaleL     = new Vector3(0.25f, hL, 0.25f);
                float groundYL = GetGroundY(PrimitiveType.Cylinder, scaleL);
                posL.y = groundYL;
                posL   = PushOffTrack(posL, leftEdge, rightEdge, trackHW);
                posL.y = groundYL; // restaurer Y après déplacement horizontal par PushOffTrack
                CreateDecorGameObject(container, PrimitiveType.Cylinder, posL,
                    Quaternion.identity, scaleL, DarkCyan, totalObjects++, matCache);

                // Côté droit
                float offsetR  = UnityEngine.Random.Range(0.5f, 2f);
                Vector3 posR   = rightEdge[i] + rightDir * offsetR;
                float hR       = UnityEngine.Random.Range(0.6f, 1.4f);
                var scaleR     = new Vector3(0.25f, hR, 0.25f);
                float groundYR = GetGroundY(PrimitiveType.Cylinder, scaleR);
                posR.y = groundYR;
                posR   = PushOffTrack(posR, leftEdge, rightEdge, trackHW);
                posR.y = groundYR;
                CreateDecorGameObject(container, PrimitiveType.Cylinder, posR,
                    Quaternion.identity, scaleR, DarkCyan, totalObjects++, matCache);
            }

            // ── TIER 2 : Marqueurs de bord ───────────────────────────────────────
            // Cubes/capsules à 4-10 m du bord, alternance gauche/droite toutes les ~22 m.
            float tier2Spacing = 22f;
            float accDist2 = 0f;
            int tier2Index = 0;

            for (int i = 0; i < leftEdge.Length - 1; i++)
            {
                float seg = Vector3.Distance(leftEdge[i], leftEdge[i + 1]);
                accDist2 += seg;
                if (accDist2 < tier2Spacing) continue;
                accDist2 = 0f;
                tier2Index++;

                Vector3 fwd      = (leftEdge[i + 1] - leftEdge[i]).normalized;
                Vector3 rightDir = Vector3.Cross(Vector3.up, fwd).normalized;

                bool isLeft   = (tier2Index % 2 == 0);
                float offset  = UnityEngine.Random.Range(4f, 10f);
                Vector3 edge  = isLeft ? leftEdge[i]  : rightEdge[i];
                Vector3 perp  = isLeft ? (-rightDir)  : rightDir;
                Vector3 pos   = edge + perp * offset;

                PrimitiveType prim = (tier2Index % 3) switch
                {
                    0 => PrimitiveType.Capsule,
                    1 => PrimitiveType.Cube,
                    _ => PrimitiveType.Sphere
                };

                float h = UnityEngine.Random.Range(1f, 3.5f);
                float w = UnityEngine.Random.Range(0.8f, 2.5f);
                Vector3 scale = (prim == PrimitiveType.Sphere)
                    ? Vector3.one * w
                    : new Vector3(w, h, w);
                pos.y = GetGroundY(prim, scale);
                Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                CreateDecorGameObject(container, prim, pos, rot, scale, DarkCyan, totalObjects++, matCache);
            }

            // ── TIER 3 : Structures de fond ──────────────────────────────────────
            // Grands bâtiments (cubes) et arbres/pylônes (cylindres) à 15-40 m du bord.
            // Placés des deux côtés toutes les ~55 m → horizon varié, impression de monde peuplé.
            float tier3Spacing = 55f;
            float accDist3 = 0f;

            for (int i = 0; i < leftEdge.Length - 1; i++)
            {
                float seg = Vector3.Distance(leftEdge[i], leftEdge[i + 1]);
                accDist3 += seg;
                if (accDist3 < tier3Spacing) continue;
                accDist3 = 0f;

                Vector3 fwd      = (leftEdge[i + 1] - leftEdge[i]).normalized;
                Vector3 rightDir = Vector3.Cross(Vector3.up, fwd).normalized;

                for (int side = 0; side < 2; side++)
                {
                    bool   isLeft  = (side == 0);
                    float  offsetD = UnityEngine.Random.Range(15f, 40f);
                    Vector3 edge   = isLeft ? leftEdge[i] : rightEdge[i];
                    Vector3 perp   = isLeft ? (-rightDir) : rightDir;
                    Vector3 pos    = edge + perp * offsetD;

                    bool isBuilding = (UnityEngine.Random.value > 0.35f);
                    PrimitiveType prim;
                    Vector3 scale;

                    if (isBuilding)
                    {
                        prim  = PrimitiveType.Cube;
                        scale = new Vector3(
                            UnityEngine.Random.Range(4f, 14f),
                            UnityEngine.Random.Range(3f, 10f),
                            UnityEngine.Random.Range(4f, 14f));
                    }
                    else
                    {
                        prim = PrimitiveType.Cylinder;
                        float w = UnityEngine.Random.Range(0.4f, 1.5f);
                        scale = new Vector3(w, UnityEngine.Random.Range(5f, 15f), w);
                    }

                    pos.y = GetGroundY(prim, scale);
                    Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    CreateDecorGameObject(container, prim, pos, rot, scale, DarkCyan, totalObjects++, matCache);
                }
            }

            Debug.Log($"[CircuitBuilder] ✅ {totalObjects} objets de décor générés (seed: {autoDecorSeed}).");
            EditorUtility.DisplayDialog("Décor généré !",
                $"✅ {totalObjects} objets de décor générés !\n\n" +
                $"Seed : {autoDecorSeed}\n" +
                $"  • Tier 1 – Pylônes de vitesse (proches bord)\n" +
                $"  • Tier 2 – Marqueurs 4-10 m\n" +
                $"  • Tier 3 – Structures 15-40 m\n\n" +
                "Ajustez manuellement si besoin,\n" +
                "puis cliquez '💾 Sauvegarder Décor'.",
                "OK");
        }

        #endregion
        // ─────────────────────────────────────────────────────────────────────

        #region Private Methods

        /// <summary>
        /// Couleur sombre cyan utilisée par défaut pour tous les objets de décor auto-générés.
        /// Unity n'expose pas Color.darkCyan, on la définit ici : #008B8B (HTML DarkCyan).
        /// </summary>
        private static readonly Color DarkCyan = new Color(0f, 0.545f, 0.545f);

        /// <summary>
        /// Retourne le décalage en Y (halfHeight) pour que la base d'un primitive posé à (x,0,z)
        /// soit exactement au niveau du sol.
        ///
        /// Dimensions des primitives Unity avec scale = Vector3.one :
        ///   Cube     → extents.y = 0.5 → groundY = 0.5 * scale.y
        ///   Sphere   → extents.y = 0.5 → groundY = 0.5 * scale.y
        ///   Cylinder → extents.y = 1.0 → groundY = 1.0 * scale.y
        ///   Capsule  → extents.y = 1.0 → groundY = 1.0 * scale.y
        ///   Quad     → plat           → groundY = 0
        /// </summary>
        private static float GetGroundY(PrimitiveType primitiveType, Vector3 scale)
        {
            return primitiveType switch
            {
                PrimitiveType.Cylinder => scale.y,
                PrimitiveType.Capsule  => scale.y,
                _                      => scale.y * 0.5f   // Cube, Sphere, Quad
            };
        }

        /// <summary>
        /// Sélectionne la couleur à utiliser pour l'objet d'index <paramref name="index"/> :
        ///   • Si la palette est renseignée → cycle dans la palette
        ///   • Sinon → couleur individuelle stockée (data.color) ou DarkCyan par défaut
        /// </summary>
        private static Color ResolveDecorColor(int index, Color storedColor, Color[] palette)
        {
            if (palette != null && palette.Length > 0)
                return palette[index % palette.Length];
            return storedColor;
        }

        /// <summary>
        /// Crée un primitive Unity comme objet de décor, lui applique couleur et supprime son collider.
        /// Nommage : "Decor_PrimitiveType_Index" pour retrouver le type lors de la sauvegarde.
        ///
        /// La position <paramref name="position"/> est supposée déjà ajustée en Y (base au sol).
        /// Le shader utilisé est celui du primitive Unity par défaut (compatible URP et Built-in).
        /// </summary>
        private static void CreateDecorGameObject(
            GameObject container,
            PrimitiveType primitiveType,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color,
            int index,
            System.Collections.Generic.Dictionary<Color, Material> matCache = null)
        {
            var go = GameObject.CreatePrimitive(primitiveType);
            go.name = $"Decor_{primitiveType}_{index}";
            go.transform.SetParent(container.transform);
            go.transform.position   = position;
            go.transform.rotation   = rotation;
            go.transform.localScale = scale;

            // Matériau avec couleur.
            // On clône le sharedMaterial du primitive (déjà correct pour URP/Built-in)
            // plutôt que de chercher le shader "Standard" qui n'existe pas en URP.
            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                if (matCache != null)
                {
                    if (!matCache.TryGetValue(color, out Material mat))
                    {
                        mat = new Material(rend.sharedMaterial) { color = color };
                        matCache[color] = mat;
                    }
                    rend.sharedMaterial = mat;
                }
                else
                {
                    rend.sharedMaterial = new Material(rend.sharedMaterial) { color = color };
                }
            }

            // Le décor est purement visuel : on retire le collider généré automatiquement
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        /// <summary>
        /// Tente de déterminer le type de primitive depuis le nom d'un objet de décor.
        /// Convention de nommage : "Decor_PrimitiveType_Index".
        /// </summary>
        private static PrimitiveType GetPrimitiveTypeFromName(string name)
        {
            if (name.Contains("Cylinder")) return PrimitiveType.Cylinder;
            if (name.Contains("Sphere"))   return PrimitiveType.Sphere;
            if (name.Contains("Capsule"))  return PrimitiveType.Capsule;
            if (name.Contains("Quad"))     return PrimitiveType.Quad;
            return PrimitiveType.Cube; // défaut
        }

        /// <summary>
        /// Pousse une position hors de la chaussée si elle se trouve à moins de
        /// <c>trackHalfWidth + safetyMargin</c> du centre de la route le plus proche.
        ///
        /// Algorithme : itère jusqu'à 40 fois par pas de 0.3 m en s'éloignant du centre.
        /// Utile pour les pylônes de Tier 1 dans les virages serrés.
        /// </summary>
        private static Vector3 PushOffTrack(
            Vector3 pos,
            Vector3[] leftEdge,
            Vector3[] rightEdge,
            float trackHalfWidth,
            float safetyMargin = 0.5f)
        {
            const float pushStep  = 0.3f;
            const int   maxIter   = 40;

            for (int iter = 0; iter < maxIter; iter++)
            {
                // Trouver le point central le plus proche (en 2D)
                float minDist = float.MaxValue;
                int   closestIdx = 0;

                for (int k = 0; k < leftEdge.Length; k++)
                {
                    Vector3 center = (leftEdge[k] + rightEdge[k]) * 0.5f;
                    float d = Mathf.Sqrt(
                        (pos.x - center.x) * (pos.x - center.x) +
                        (pos.z - center.z) * (pos.z - center.z));
                    if (d < minDist) { minDist = d; closestIdx = k; }
                }

                if (minDist > trackHalfWidth + safetyMargin) break; // déjà hors route

                // Repousser en s'éloignant du centre
                Vector3 center3 = (leftEdge[closestIdx] + rightEdge[closestIdx]) * 0.5f;
                Vector3 outDir  = new Vector3(pos.x - center3.x, 0f, pos.z - center3.z);
                if (outDir.sqrMagnitude < 0.001f) outDir = Vector3.right;
                outDir.Normalize();
                pos.x += outDir.x * pushStep;
                pos.z += outDir.z * pushStep;
            }

            return pos;
        }

        private bool ValidateBeforeExport(out string errorMessage)
        {
            if (circuitData == null)
            {
                errorMessage = "CircuitData asset manquant ! Assignez-en un dans l'Inspector.";
                return false;
            }
            
            if (splineContainer == null)
            {
                errorMessage = "SplineContainer manquant ! Ajoutez un component SplineContainer.";
                return false;
            }
            
            if (splineContainer.Spline == null || splineContainer.Spline.Count < 2)
            {
                errorMessage = "La spline doit contenir au moins 2 points !";
                return false;
            }
            
            errorMessage = string.Empty;
            return true;
        }

        private SplinePoint[] ConvertSplineToPoints(SplineContainer container)
        {
            var spline = container.Spline;
            var points = new SplinePoint[spline.Count];

            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];

                // Position en world space
                Vector3 worldPosition = container.transform.TransformPoint(knot.Position);

                // Rotation en world space
                Quaternion worldRotation = container.transform.rotation * knot.Rotation;

                // Les tangentes sont des OFFSETS en espace local du knot
                // On doit les transformer en world space
                Vector3 worldTangentIn = container.transform.TransformPoint(knot.Position + knot.TangentIn) - worldPosition;
                Vector3 worldTangentOut = container.transform.TransformPoint(knot.Position + knot.TangentOut) - worldPosition;

                points[i] = new SplinePoint
                {
                    position = worldPosition,
                    tangentIn = worldTangentIn,
                    tangentOut = worldTangentOut,
                    rotation = worldRotation  // ← NOUVEAU
                };
            }

            return points;
        }

        [ContextMenu("Debug Spline Data")]
        public void DebugSplineData()
        {
            if (splineContainer == null || splineContainer.Spline == null) return;

            var spline = splineContainer.Spline;

            Debug.Log("=== SPLINE DEBUG ===");
            for (int i = 0; i < Mathf.Min(3, spline.Count); i++)
            {
                var knot = spline[i];

                Debug.Log($"Knot {i}:");
                Debug.Log($"  Position: {knot.Position}");
                Debug.Log($"  TangentIn: {knot.TangentIn}");
                Debug.Log($"  TangentOut: {knot.TangentOut}");
                //Debug.Log($"  TangentMode: {knot.Mode}");

                // Test d'évaluation Unity
                float t = i / (float)spline.Count;
                splineContainer.Evaluate(spline, t, out float3 pos, out float3 tangent, out float3 up);
                Debug.Log($"  Unity Evaluate at t={t}: {pos}");
            }
        }
        #endregion

        #region Gizmos

        private void DrawSplinePreview()
        {
            if (!showSplinePoints || splineContainer == null) return;
            
            var spline = splineContainer.Spline;
            if (spline == null || spline.Count == 0) return;
            
            Gizmos.color = Color.cyan;
            
            for (int i = 0; i < spline.Count; i++)
            {
                Vector3 pos = splineContainer.transform.TransformPoint(spline[i].Position);
                Gizmos.DrawWireSphere(pos, 1f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(pos + Vector3.up * 2f, $"P{i}");
                #endif
            }
        }
        
        private void DrawCheckpointsPreview()
        {
            if (!showCheckpoints || circuitData == null || splineContainer == null) return;
            
            // Créer un CircuitData temporaire pour preview
            var tempData = ScriptableObject.CreateInstance<CircuitData>();
            tempData.splinePoints = ConvertSplineToPoints(splineContainer);
            tempData.trackWidth = circuitData.trackWidth;
            tempData.closedLoop = circuitData.closedLoop;
            tempData.autoCheckpointCount = circuitData.autoCheckpointCount;
            
            var checkpoints = CircuitMeshGenerator.GenerateAutoCheckpoints(
                tempData, 
                circuitData.autoCheckpointCount
            );
            
            Gizmos.color = Color.green;
            
            foreach (var cp in checkpoints)
            {
                Vector3 size = new Vector3(cp.width, 3f, 0.5f);
                
                // Dessiner le checkpoint
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(cp.position, cp.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                
                // Version semi-transparente
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.color = Color.green;
                
                Gizmos.matrix = oldMatrix;
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(cp.position + Vector3.up * 4f, $"CP {cp.index}");
                #endif
            }
        }
        
        private void DrawSpawnPointPreview()
        {
            if (!showSpawnPoint || spawnPoint == null) return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnPoint.position, 1.5f);
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 3f, "SPAWN");
            #endif
        }
        
        #endregion
    }
#endif
}