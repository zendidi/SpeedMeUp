# Documentation - Système de Gestion des Circuits

## 📚 Vue d'ensemble

Ce système fournit une solution complète pour la gestion des circuits dans SpeedMeUp:

1. **CircuitDatabase** - Base de données centralisée des circuits
2. **HighscoreManager** - Gestion des meilleurs temps par circuit
3. **CircuitSelectionUI** - Interface de sélection de circuits
4. **CircuitThumbnailGenerator** - Générateur automatique de miniatures

---

## 🎯 1. CircuitDatabase (Source Unique de Vérité)

### Qu'est-ce que c'est ?

Un ScriptableObject qui centralise tous les circuits disponibles dans le jeu. Plus besoin de dupliquer les listes de circuits partout!

### Installation

1. **Créer la base de données** :
   - Clic droit dans le projet Unity → `Create → Arcade Racer → Circuit Database`
   - Nommer le fichier `CircuitDatabase`
   - **Important**: Placer le fichier dans `Assets/Resources/` pour qu'il soit accessible globalement

2. **Configurer les circuits** :
   - Ouvrir `CircuitDatabase` dans l'inspecteur
   - Dans la section "Available Circuits", cliquer sur le `+`
   - Glisser-déposer vos `CircuitData` depuis le projet

### Utilisation en code

```csharp
using ArcadeRacer.Settings;

// Accéder à la base de données
CircuitDatabase db = CircuitDatabase.Instance;

// Récupérer tous les circuits
foreach (var circuit in db.AvailableCircuits)
{
    Debug.Log(circuit.circuitName);
}

// Récupérer un circuit par nom
CircuitData circuit = db.GetCircuitByName("Desert Track");

// Récupérer un circuit par index
CircuitData firstCircuit = db.GetCircuitByIndex(0);

// Vérifier si un circuit existe
bool exists = db.ContainsCircuitByName("Desert Track");
```

---

## 🏆 2. HighscoreManager (Gestion des Meilleurs Temps)

### Qu'est-ce que c'est ?

Un singleton qui gère les highscores locaux (PlayerPrefs) avec top 10 par circuit.

### Format de temps

**Format: MM:SS:mmm** (minutes:secondes:millièmes)
- Exemple: `01:23:456` = 1 minute, 23 secondes, 456 millièmes

### Installation

Le `HighscoreManager` se crée automatiquement au premier accès. Aucune configuration requise!

### Utilisation

#### Ajouter un score

```csharp
using ArcadeRacer.Core;

// À la fin d'une course
float finalTime = 83.456f; // secondes
string playerName = "Player1";
string circuitName = "Desert Track";

bool isTopScore = HighscoreManager.Instance.TryAddScore(
    circuitName, 
    finalTime, 
    playerName
);

if (isTopScore)
{
    Debug.Log("Nouveau record !");
}
```

#### Récupérer les highscores

```csharp
// Récupérer le top 10 d'un circuit
List<HighscoreEntry> scores = HighscoreManager.Instance.GetHighscores("Desert Track");

foreach (var entry in scores)
{
    Debug.Log($"{entry.rank}. {entry.FormattedTime} - {entry.playerName}");
    // Affiche: "1. 01:23:456 - Player1"
}

// Récupérer uniquement le meilleur temps
HighscoreEntry? best = HighscoreManager.Instance.GetBestTime("Desert Track");
if (best.HasValue)
{
    Debug.Log($"Record: {best.Value.FormattedTime}");
}
```

#### Vérifier si un temps serait un record

```csharp
float newTime = 85.2f;
bool wouldBeTop = HighscoreManager.Instance.WouldBeTopScore("Desert Track", newTime);

if (wouldBeTop)
{
    // Demander le nom du joueur
}
```

#### Effacer les highscores

```csharp
// Effacer les scores d'un circuit
HighscoreManager.Instance.ClearHighscores("Desert Track");

// Effacer TOUS les highscores
HighscoreManager.Instance.ClearAllHighscores();
```

#### Formater/Parser des temps

```csharp
// Formater un temps en secondes vers MM:SS:mmm
float timeInSeconds = 83.456f;
string formatted = HighscoreEntry.FormatTime(timeInSeconds);
// Résultat: "01:23:456"

// Parser un temps formaté vers secondes
string timeString = "01:23:456";
float seconds = HighscoreEntry.ParseTime(timeString);
// Résultat: 83.456
```

---

## 🖼️ 3. CircuitThumbnailGenerator (Génération Automatique)

### Qu'est-ce que c'est ?

Un outil d'éditeur qui génère automatiquement des sprites de miniatures pour vos circuits à partir des splinePoints.

### Caractéristiques

- Sprite 256x256 pixels
- Tracé noir sur fond blanc (alpha 0.5)
- Centrage et mise à l'échelle automatique
- Sauvegarde dans `Assets/Circuits/Thumbnails/`

### Utilisation

#### Méthode 1: Bouton dans l'inspecteur

1. Sélectionner un `CircuitData` dans le projet
2. Dans l'inspecteur, descendre jusqu'à "Thumbnail Generator"
3. Cliquer sur **"Generate Thumbnail"**
4. Le sprite est automatiquement généré et assigné!

#### Méthode 2: Menu contextuel

1. Clic droit sur un `CircuitData` dans le projet
2. Sélectionner **"Generate Circuit Thumbnail"**
3. Le sprite est automatiquement généré et assigné!

#### Méthode 3: Code (pour batch processing)

```csharp
using ArcadeRacer.Settings;

// Dans un script Editor
CircuitData circuit = /* votre circuit */;
Sprite thumbnail = CircuitThumbnailGenerator.GenerateThumbnail(circuit, autoAssign: true);
```

### Résultat

- Fichier PNG créé dans `Assets/Circuits/Thumbnails/CircuitName_Thumbnail.png`
- Sprite automatiquement assigné au champ `thumbnail` du CircuitData
- Prêt à être utilisé dans l'UI!

---

## 🎨 4. CircuitSelectionUI (Interface de Sélection)

### Qu'est-ce que c'est ?

Un composant UI qui génère automatiquement une grille de circuits sélectionnables.

### Installation

#### Étape 1: Créer le Prefab de l'Item

1. **Créer un nouveau GameObject** dans la hiérarchie
2. **Ajouter le composant** `CircuitSelectionItem`
3. **Configurer la structure UI** :
   ```
   CircuitSelectionItem
   ├── Background (Image)
   ├── Thumbnail (Image)
   ├── CircuitName (TextMeshProUGUI)
   └── Button (Button)
   ```

4. **Assigner les références** dans l'inspecteur du `CircuitSelectionItem`:
   - Thumbnail Image
   - Circuit Name Text
   - Select Button
   - Background Image

5. **Sauvegarder comme Prefab** dans `Assets/Prefabs/UI/`

#### Étape 2: Créer le Panel de Sélection

1. **Créer un Canvas** (si pas déjà existant)
2. **Créer un Panel** nommé "CircuitSelectionPanel"
3. **Ajouter un GameObject enfant** nommé "GridContainer"
4. **Ajouter un GridLayoutGroup** sur GridContainer:
   - Cell Size: (200, 250) par exemple
   - Spacing: (10, 10)
   - Constraint: Fixed Column Count (3 par exemple)

5. **Ajouter le composant** `CircuitSelectionUI` sur CircuitSelectionPanel
6. **Assigner les références**:
   - Grid Container: Le GameObject avec GridLayoutGroup
   - Item Prefab: Le prefab créé à l'étape 1
   - Use Circuit Database: ✓ (coché)

### Utilisation

Le système génère automatiquement les items au Start(). Chaque circuit dans la `CircuitDatabase` aura son propre bouton.

#### Événements

```csharp
using ArcadeRacer.UI;
using ArcadeRacer.Settings;

public class MyGameManager : MonoBehaviour
{
    [SerializeField] private CircuitSelectionUI circuitSelection;
    
    void Start()
    {
        // S'abonner à l'événement de sélection
        circuitSelection.OnCircuitSelected.AddListener(OnCircuitChosen);
    }
    
    void OnCircuitChosen(CircuitData circuit)
    {
        Debug.Log($"Joueur a sélectionné: {circuit.circuitName}");
        
        // Charger le circuit
        CircuitManager.Instance.LoadCircuit(circuit);
        
        // Cacher la sélection
        circuitSelection.Hide();
        
        // Démarrer la course
        // ...
    }
}
```

#### API Publique

```csharp
// Afficher/Cacher
circuitSelection.Show();
circuitSelection.Hide();

// Recharger les items (après modification de la database)
circuitSelection.ReloadItems();

// Sélectionner un circuit par code
circuitSelection.SelectCircuit(myCircuit);
circuitSelection.SelectCircuitByIndex(0);

// Récupérer le circuit sélectionné
CircuitData selected = circuitSelection.SelectedCircuit;
```

### Intégration avec UIManager

```csharp
using ArcadeRacer.UI;

// Dans votre code de menu
UIManager uiManager = FindObjectOfType<UIManager>();

// Afficher la sélection de circuits
uiManager.ShowCircuitSelection();

// Cacher la sélection
uiManager.HideCircuitSelection();
```

---

## 🔗 5. Workflow Complet

### Configuration Initiale (Une seule fois)

1. **Créer CircuitDatabase**
   - `Assets/Resources/CircuitDatabase.asset`
   - Ajouter tous vos circuits

2. **Créer le Prefab CircuitSelectionItem**
   - Structure UI complète
   - Sauvegarder dans Prefabs/

3. **Configurer la scène de menu**
   - Canvas avec CircuitSelectionUI
   - GridLayoutGroup configuré
   - UIManager avec référence

### Workflow de développement

#### Ajouter un nouveau circuit

1. Créer le `CircuitData` ScriptableObject
2. Configurer les splinePoints
3. Cliquer "Generate Thumbnail" dans l'inspecteur
4. Ajouter le circuit à `CircuitDatabase`
5. **C'est tout!** L'UI se met à jour automatiquement

#### Tester les highscores

```csharp
// Dans un script de test
void TestHighscores()
{
    var manager = HighscoreManager.Instance;
    
    // Ajouter des scores de test
    manager.TryAddScore("Circuit 1", 65.432f, "Player1");
    manager.TryAddScore("Circuit 1", 62.123f, "Player2");
    manager.TryAddScore("Circuit 1", 68.999f, "Player3");
    
    // Afficher les résultats
    var scores = manager.GetHighscores("Circuit 1");
    foreach (var entry in scores)
    {
        Debug.Log($"{entry.rank}. {entry.FormattedTime} - {entry.playerName}");
    }
}
```

#### Menu contextuel de debug

Sur le `HighscoreManager` dans la hiérarchie:
- Clic droit → **"Debug: Display All Highscores"**
- Affiche tous les highscores dans la console

---

## 📁 Structure des Fichiers

```
Assets/
├── Resources/
│   └── CircuitDatabase.asset          # Base de données (obligatoire)
├── Circuits/
│   ├── Thumbnails/                    # Générés automatiquement
│   │   ├── Circuit1_Thumbnail.png
│   │   └── Circuit2_Thumbnail.png
│   └── [vos CircuitData]
├── Prefabs/
│   └── UI/
│       └── CircuitSelectionItem.prefab
└── Project/
    └── Scripts/
        ├── Core/
        │   └── HighscoreManager.cs
        ├── Settings/
        │   └── CircuitDatabase.cs
        ├── Track/
        │   └── Editor/
        │       └── CircuitThumbnailGenerator.cs
        └── UI/
            ├── CircuitSelectionUI.cs
            ├── CircuitSelectionItem.cs
            └── UIManager.cs
```

---

## ⚠️ Points Importants

1. **CircuitDatabase DOIT être dans Resources/**
   - Sinon, le singleton ne pourra pas le charger
   - Chemin: `Assets/Resources/CircuitDatabase.asset`

2. **Format de temps des highscores**
   - Toujours stocké en float (secondes)
   - Formaté en MM:SS:mmm à l'affichage
   - Utiliser `HighscoreEntry.FormatTime()` et `ParseTime()`

3. **Thumbnails**
   - Générés dans `Assets/Circuits/Thumbnails/`
   - Taille: 256x256 pixels
   - Format: PNG avec transparence

4. **GridLayoutGroup**
   - Le container DOIT avoir un GridLayoutGroup
   - Configurez Cell Size, Spacing, et Constraint

---

## 🐛 Dépannage

### "CircuitDatabase non trouvée"
→ Vérifier que le fichier est dans `Assets/Resources/`

### "Pas de thumbnail généré"
→ Vérifier que le CircuitData a au moins 3 splinePoints

### "Items UI ne s'affichent pas"
→ Vérifier que le Prefab a bien le composant `CircuitSelectionItem`

### "Highscores ne se sauvegardent pas"
→ Vérifier que `PlayerPrefs.Save()` est appelé (automatique dans le code)

---

## 🚀 Exemple d'Utilisation Complète

```csharp
using UnityEngine;
using ArcadeRacer.Settings;
using ArcadeRacer.Core;
using ArcadeRacer.UI;

public class GameFlow : MonoBehaviour
{
    [SerializeField] private CircuitSelectionUI circuitSelectionUI;
    [SerializeField] private UIManager uiManager;
    
    void Start()
    {
        // Afficher la sélection de circuits au démarrage
        uiManager.ShowCircuitSelection();
        
        // S'abonner à l'événement
        circuitSelectionUI.OnCircuitSelected.AddListener(OnCircuitSelected);
    }
    
    void OnCircuitSelected(CircuitData circuit)
    {
        // Cacher la sélection
        uiManager.HideCircuitSelection();
        
        // Charger le circuit
        CircuitManager.Instance.LoadCircuit(circuit);
        
        // Démarrer la course
        StartRace(circuit);
    }
    
    void StartRace(CircuitData circuit)
    {
        // Votre logique de démarrage de course...
    }
    
    void OnRaceFinished(float finalTime, CircuitData circuit)
    {
        // Vérifier si c'est un record
        bool isTopScore = HighscoreManager.Instance.WouldBeTopScore(
            circuit.circuitName, 
            finalTime
        );
        
        if (isTopScore)
        {
            // Demander le nom du joueur
            ShowNameInputDialog((playerName) =>
            {
                // Sauvegarder le score
                HighscoreManager.Instance.TryAddScore(
                    circuit.circuitName,
                    finalTime,
                    playerName
                );
                
                // Afficher le tableau des scores
                ShowHighscoreTable(circuit.circuitName);
            });
        }
    }
    
    void ShowHighscoreTable(string circuitName)
    {
        var scores = HighscoreManager.Instance.GetHighscores(circuitName);
        
        foreach (var entry in scores)
        {
            Debug.Log($"{entry.rank}. {entry.FormattedTime} - {entry.playerName}");
        }
    }
}
```

---

Voilà! Vous avez maintenant un système complet de gestion des circuits avec highscores, sélection UI, et génération automatique de thumbnails. 🎉
