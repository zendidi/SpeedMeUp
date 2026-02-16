# 🔧 Documentation Technique - Architecture Unifiée Circuit

## 📐 Architecture Globale

### Principe Fondamental
**Un seul flux de génération** utilisé par l'éditeur ET le runtime, garantissant la cohérence.

```
CircuitGenerationConstants (Configuration partagée)
              ↓
    ┌─────────────────────┐
    │ CircuitBuilder      │ ← ÉDITEUR
    │ (Editor-time)       │
    │ - Mode Création     │
    │ - Mode Édition      │
    │ - Preview           │
    └─────────────────────┘
              ↓
    CircuitMeshGenerator.Generate()
              ↓
    ┌─────────────────────┐
    │   CircuitData       │ ← ScriptableObject
    │   (Asset)           │
    └─────────────────────┘
              ↓
    ┌─────────────────────┐
    │ CircuitManager      │ ← RUNTIME
    │ (Runtime)           │
    │ - Load Circuit      │
    └─────────────────────┘
              ↓
    CircuitMeshGenerator.Generate()
              ↓
      MÊME RÉSULTAT ✓
```

---

## 🎯 Classes Principales

### CircuitGenerationConstants
**Emplacement:** `Assets/Project/Scripts/Settings/CircuitGenerationConstants.cs`

**Rôle:** Configuration partagée entre éditeur et runtime

**Constantes:**
```csharp
public const int SEGMENTS_PER_SPLINE_POINT = 10;
public const float CURVE_QUALITY_MULTIPLIER = 10f;
public const float UV_TILING_X = 1f;
public const float UV_TILING_Y = 0.5f;
public const bool GENERATE_COLLIDER_EDITOR = false;
public const bool GENERATE_COLLIDER_RUNTIME = true;
```

**Properties:**
```csharp
public static CircuitMeshGenerator.GenerationConfig EditorConfig { get; }
public static CircuitMeshGenerator.GenerationConfig RuntimeConfig { get; }
```

**Garantie:** EditorConfig et RuntimeConfig utilisent les **mêmes valeurs** (sauf colliders)

---

### CircuitBuilder
**Emplacement:** `Assets/Project/Scripts/Track/TrackBuilder/CircuitBuilder.cs`

**Rôle:** Outil éditeur unifié pour création/édition de circuits

**Modes:**
```csharp
public enum CircuitBuilderMode
{
    None,       // Pas de CircuitData assigné
    Creation,   // Nouveau circuit (splinePoints vides)
    Edition     // Circuit existant (splinePoints présents)
}
```

**Méthodes Principales:**

#### Détection Mode
```csharp
public CircuitBuilderMode GetCurrentMode()
```
- Détecte automatiquement le mode basé sur CircuitData.splinePoints

#### Création
```csharp
public void CreateNewCircuitData()
```
- Crée un nouveau CircuitData asset
- Initialise avec valeurs par défaut
- Assigne automatiquement

#### Édition
```csharp
public void LoadCircuitDataIntoSpline()
```
- Reconstruit la spline depuis CircuitData.splinePoints
- Convertit world → local coordinates
- Repositionne le spawn point

#### Preview
```csharp
public void GeneratePreview()
```
- Utilise `CircuitGenerationConstants.EditorConfig`
- Génère mesh avec CircuitMeshGenerator
- Affiche dans la scène (hideFlags = DontSave)

#### Export
```csharp
public void ExportToCircuitData()
```
- Convertit SplineContainer → SplinePoint[]
- Sauvegarde dans CircuitData asset
- Marque comme dirty pour sauvegarde

#### Checkpoints
```csharp
public void GenerateCheckpointPreview()
```
- Utilise CircuitMeshGenerator.GenerateAutoCheckpoints()
- Crée GameObjects visuels éditeur
- Permet ajustement manuel

```csharp
public void SaveCheckpointsToCircuitData()
```
- Trouve checkpoints dans la scène
- Convertit en positions relatives (spawn point)
- Sauvegarde dans CircuitData.checkpointData[]

---

### CircuitManager
**Emplacement:** `Assets/Project/Scripts/Track/CircuitManager.cs`

**Rôle:** Charge et affiche les circuits au runtime

**Méthode Clé:**
```csharp
public void LoadCircuit(CircuitData circuitData)
{
    // Utilise configuration unifiée
    var config = CircuitGenerationConstants.RuntimeConfig;
    
    var result = CircuitMeshGenerator.Generate(circuitData, config);
    // ... créer GameObjects, colliders, etc.
}
```

**Garantie:** Utilise la **même configuration** que CircuitBuilder

---

### CircuitMeshGenerator
**Emplacement:** `Assets/Project/Scripts/Track/TrackBuilder/CircuitMeshGenerator.cs`

**Rôle:** Génération procédurale de mesh (static utility)

**Méthode Principale:**
```csharp
public static GenerationResult Generate(
    CircuitData circuitData, 
    GenerationConfig config
)
```

**Invariant:** Même circuitData + même config = **même mesh**

---

## 🔄 Flux de Données

### Création d'un Circuit

```
1. Level Designer
   ↓
2. CircuitBuilder.CreateNewCircuitData()
   ↓ Crée
3. CircuitData (vide)
   ↓ Mode = CRÉATION
4. Éditer Spline (Unity Spline Tool)
   ↓
5. CircuitBuilder.GeneratePreview()
   ├─ ConvertSplineToPoints()
   ├─ CircuitMeshGenerator.Generate(EditorConfig)
   └─ Afficher mesh
   ↓
6. Ajuster spline/spawn/checkpoints
   ↓
7. CircuitBuilder.ExportToCircuitData()
   ├─ ConvertSplineToPoints()
   ├─ CalculateTotalLength()
   └─ Save to asset
   ↓
8. CircuitData (complet)
   ↓
9. Ajouter à CircuitDatabase
```

### Édition d'un Circuit

```
1. Level Designer
   ↓
2. Assigner CircuitData existant
   ↓ Mode = ÉDITION
3. CircuitBuilder.LoadCircuitDataIntoSpline()
   ├─ Read splinePoints[]
   ├─ Reconstruct SplineContainer
   └─ Repositionner spawn
   ↓
4. Modifier Spline
   ↓
5. CircuitBuilder.GeneratePreview()
   └─ Voir changements
   ↓
6. CircuitBuilder.ExportToCircuitData()
   └─ Mise à jour asset
```

### Utilisation Runtime

```
1. Game Start
   ↓
2. CircuitSelectionUI (joueur choisit circuit)
   ↓
3. CircuitManager.LoadCircuit(circuitData)
   ├─ CircuitMeshGenerator.Generate(RuntimeConfig)
   ├─ Create GameObjects
   ├─ Create Colliders
   ├─ Create SplineContainer (for CheckpointManager)
   └─ InitializeCheckpointManager()
   ↓
4. Circuit visible et jouable
```

---

## 🎨 Conversion Coordinates

### SplinePoint Stockage
Les `SplinePoint` sont stockés en **world space** dans CircuitData :

```csharp
public struct SplinePoint
{
    public Vector3 position;      // World space
    public Vector3 tangentIn;     // World space
    public Vector3 tangentOut;    // World space
    public Quaternion rotation;   // World space
}
```

### Conversion Spline → CircuitData
```csharp
private SplinePoint[] ConvertSplineToPoints(SplineContainer container)
{
    for each knot in spline:
        worldPosition = container.transform.TransformPoint(knot.Position)
        worldRotation = container.transform.rotation * knot.Rotation
        worldTangentIn = TransformVector(knot.TangentIn)
        worldTangentOut = TransformVector(knot.TangentOut)
}
```

### Conversion CircuitData → Spline
```csharp
public void LoadCircuitDataIntoSpline()
{
    for each point in circuitData.splinePoints:
        localPos = container.transform.InverseTransformPoint(point.position)
        tangentInLocal = InverseTransformDirection(point.tangentIn)
        tangentOutLocal = InverseTransformDirection(point.tangentOut)
        
        knot = new BezierKnot(localPos, tangentInLocal, tangentOutLocal, rotation)
}
```

---

## 📊 CheckpointData Relatif

### Stockage Relatif au Spawn Point
Les checkpoints sont stockés **relativement au spawn point** :

```csharp
public struct CheckpointData
{
    public Vector3 relativePosition;      // Relatif au spawn
    public Quaternion relativeRotation;   // Relatif au spawn
    public int index;
    public bool isStartFinishLine;
}
```

### Conversion World → Relative
```csharp
public static CheckpointData CreateRelativeToSpawn(
    Vector3 worldPosition,
    Quaternion worldRotation,
    Vector3 spawnPosition,
    Quaternion spawnRotation,
    int index,
    bool isStartFinish
)
{
    Vector3 relativePos = Quaternion.Inverse(spawnRotation) * 
                          (worldPosition - spawnPosition);
    
    Quaternion relativeRot = Quaternion.Inverse(spawnRotation) * 
                             worldRotation;
    
    return new CheckpointData
    {
        relativePosition = relativePos,
        relativeRotation = relativeRot,
        index = index,
        isStartFinishLine = isStartFinish
    };
}
```

### Conversion Relative → World
```csharp
public void GetWorldTransform(
    Vector3 spawnPosition,
    Quaternion spawnRotation,
    out Vector3 worldPosition,
    out Quaternion worldRotation
)
{
    worldPosition = spawnPosition + spawnRotation * relativePosition;
    worldRotation = spawnRotation * relativeRotation;
}
```

**Avantage:** Si le spawn point bouge, les checkpoints bougent avec !

---

## ⚙️ Configuration Mesh Generation

### Structure GenerationConfig
```csharp
public struct GenerationConfig
{
    public int segmentsPerSplinePoint;       // Interpolation spline
    public float uvTilingX;                  // Texture tiling largeur
    public float uvTilingY;                  // Texture tiling longueur
    public bool generateCollider;            // Générer colliders?
    public bool optimizeMesh;                // Optimiser mesh?
    public float curveQualityMultiplier;     // Qualité courbes
}
```

### Valeurs Garanties
```csharp
// CircuitGenerationConstants.cs
EditorConfig:
    segments = 10        ✓ IDENTIQUE
    quality = 10         ✓ IDENTIQUE
    uvX = 1.0            ✓ IDENTIQUE
    uvY = 0.5            ✓ IDENTIQUE
    collider = false     ✗ DIFFÉRENT (performance)
    optimize = true      ✓ IDENTIQUE

RuntimeConfig:
    segments = 10        ✓ IDENTIQUE
    quality = 10         ✓ IDENTIQUE
    uvX = 1.0            ✓ IDENTIQUE
    uvY = 0.5            ✓ IDENTIQUE
    collider = true      ✗ DIFFÉRENT (nécessaire)
    optimize = true      ✓ IDENTIQUE
```

**Résultat:** Mesh visuellement **identique** (sauf colliders invisibles)

---

## 🧪 Tests de Cohérence

### Test 1: Preview = Runtime
```csharp
[Test]
public void PreviewConfigEqualsRuntimeConfig()
{
    var editor = CircuitGenerationConstants.EditorConfig;
    var runtime = CircuitGenerationConstants.RuntimeConfig;
    
    Assert.AreEqual(editor.segmentsPerSplinePoint, runtime.segmentsPerSplinePoint);
    Assert.AreEqual(editor.curveQualityMultiplier, runtime.curveQualityMultiplier);
    Assert.AreEqual(editor.uvTilingX, runtime.uvTilingX);
    Assert.AreEqual(editor.uvTilingY, runtime.uvTilingY);
    // collider peut différer
}
```

### Test 2: Mesh Identique
```csharp
[Test]
public void SameConfigProducesSameMesh()
{
    var data = CreateTestCircuitData();
    
    var result1 = CircuitMeshGenerator.Generate(data, config);
    var result2 = CircuitMeshGenerator.Generate(data, config);
    
    Assert.AreEqual(result1.roadMesh.vertexCount, result2.roadMesh.vertexCount);
    Assert.AreEqual(result1.roadMesh.triangles.Length, result2.roadMesh.triangles.Length);
}
```

---

## 🔒 Invariants du Système

1. **Configuration Partagée:**
   - EditorConfig et RuntimeConfig utilisent CircuitGenerationConstants
   - Modifications doivent être faites dans CircuitGenerationConstants uniquement

2. **CircuitData Immuable:**
   - Seul CircuitBuilder modifie CircuitData
   - CircuitManager lit CircuitData (read-only)

3. **Mesh Déterministe:**
   - Même CircuitData + même config = même mesh
   - Ordre des vertices garanti identique

4. **Checkpoints Relatifs:**
   - Toujours stockés relativement au spawn point
   - Permet repositionnement global

---

## 📝 Bonnes Pratiques Développeur

### Modification de Configuration
❌ **MAUVAIS:**
```csharp
var config = new GenerationConfig { segments = 15 }; // Hard-coded
```

✅ **BON:**
```csharp
var config = CircuitGenerationConstants.EditorConfig;
```

### Ajout de Paramètre
1. Ajouter constante dans `CircuitGenerationConstants`
2. Utiliser dans EditorConfig ET RuntimeConfig
3. Mettre à jour CircuitMeshGenerator si nécessaire

### Test de Cohérence
Toujours tester qu'un circuit fonctionne :
1. En preview éditeur
2. En runtime
3. Vérifier visuellement l'identité

---

## 🐛 Debug

### "Preview différent du runtime"
- Vérifier CircuitGenerationConstants.cs
- S'assurer aucun hard-coded config ailleurs
- Comparer EditorConfig vs RuntimeConfig

### "Checkpoints mal positionnés"
- Vérifier spawn point existe et bien positionné
- Checkpoints stockés en relatif
- Reload CircuitData après modification spawn

### "Mesh bizarre après édition"
- Vérifier spline closed/open cohérent
- Minimum 3 points pour circuit fermé
- Tangentes non nulles

---

## 📚 Références

- **Unity Splines:** https://docs.unity3d.com/Packages/com.unity.splines@latest
- **Bézier Curves:** https://en.wikipedia.org/wiki/B%C3%A9zier_curve
- **ScriptableObjects:** https://docs.unity3d.com/Manual/class-ScriptableObject.html

---

## 🔄 Évolutions Futures

### Possibilités
- [ ] Undo/Redo pour édition spline
- [ ] Multi-circuit editing
- [ ] Checkpoint visual handles dans scene view
- [ ] Auto-save checkpoint positions
- [ ] Circuit validation automatique

### Extensibilité
L'architecture actuelle permet facilement :
- Ajout de nouveaux types de checkpoints
- Modification de l'algo de génération mesh
- Ajout de variantes de configuration

---

**Version:** 1.0.0
**Date:** Février 2026
**Auteur:** Architecture Unifiée Circuit
