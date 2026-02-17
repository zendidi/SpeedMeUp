# 🏆 Système d'Affichage des Highscores - Guide de Setup

## 📋 Vue d'Ensemble

Ce système permet d'afficher les meilleurs temps des joueurs par circuit, avec un design simple et élégant incluant un code couleur selon le rang.

---

## 🎨 Architecture

### Scripts Créés

1. **HighscoreItemUI.cs** - Représente une ligne dans la liste
2. **HighscoreDisplayUI.cs** - Contrôleur principal de l'UI
3. **HighscoreManager.cs** - Mis à jour pour inclure les dates

### Intégrations

- ✅ **HighscoreManager** - Gestion des scores
- ✅ **CircuitDatabase** - Liste des circuits disponibles
- ✅ **CircuitManager** - Circuit actuellement chargé

---

## 🔧 Setup dans Unity

### 1. Créer le Prefab HighscoreItem

#### Hiérarchie du Prefab
```
HighscoreItem (GameObject)
├── BackgroundImage (RawImage)
└── InfoText (TextMeshProUGUI)
```

#### Configuration HighscoreItem (Root)
- **Composant:** HighscoreItemUI (script)
- **Layout Element** (recommandé):
  - Min Height: 40
  - Preferred Height: 40

#### Configuration BackgroundImage
- **Type:** RawImage
- **Color:** White (sera overridé par le script)
- **Alpha:** 30/255 (sera géré par le script)

#### Configuration InfoText
- **Type:** TextMeshProUGUI
- **Font:** Votre police préférée
- **Font Size:** 16-20
- **Alignment:** Middle Left
- **Color:** White (ou noir selon votre design)
- **Margins:** 10px sur les côtés

#### Assignation dans HighscoreItemUI
- **Background Image:** Référence vers RawImage
- **Info Text:** Référence vers TextMeshProUGUI
- **Colors:** (valeurs par défaut déjà configurées)
  - Record Color: Purple (0.7, 0.3, 1, 0.117)
  - Top Color: Green (0.3, 1, 0.3, 0.117)
  - Mid Color: Blue (0.3, 0.5, 1, 0.117)
  - Low Color: Red (1, 0.3, 0.3, 0.117)

---

### 2. Créer le GameObject HighscoreList

#### Hiérarchie
```
Canvas
└── HighscorePanel
    ├── CircuitDropdown (TMP_Dropdown)
    └── HighscoreList (GameObject)
        └── (Items générés dynamiquement)
```

#### Configuration HighscorePanel
- Panel avec background
- Peut contenir un titre "HIGHSCORES"

#### Configuration CircuitDropdown
- **Type:** TMP_Dropdown
- **Template:** Standard Dropdown Template
- **Caption Text:** "Sélectionner Circuit"

#### Configuration HighscoreList
- **Composants:**
  - RectTransform
  - **VerticalLayoutGroup:**
    - Spacing: 5
    - Child Force Expand: Width (coché), Height (décoché)
    - Child Control Size: Width et Height (cochés)
  - **Content Size Fitter** (optionnel):
    - Vertical Fit: Preferred Size

#### Configuration HighscoreDisplayUI (sur HighscorePanel ou root)
- **Highscore List Container:** Référence vers HighscoreList Transform
- **Circuit Dropdown:** Référence vers TMP_Dropdown
- **Highscore Item Prefab:** Référence vers le prefab créé en étape 1
- **Settings:**
  - Refresh On Enable: ✓ (coché)
  - Use Current Circuit As Default: ✓ (coché)

---

## 🎯 Utilisation

### Affichage Automatique

Le système s'initialise automatiquement quand le GameObject avec HighscoreDisplayUI est activé:

1. Charge tous les circuits depuis CircuitDatabase
2. Remplit le dropdown
3. Sélectionne le circuit actuel (si disponible)
4. Affiche les highscores

### Changer de Circuit

#### Via Dropdown (UI)
L'utilisateur sélectionne un circuit dans le dropdown → La liste se rafraîchit automatiquement.

#### Via Code
```csharp
// Définir un circuit spécifique
HighscoreDisplayUI display = GetComponent<HighscoreDisplayUI>();
display.SetCircuit("NomDuCircuit");

// Ou rafraîchir l'affichage actuel
display.Refresh();
```

### Ajouter un Highscore

Utilisez le HighscoreManager existant:

```csharp
// Ajouter un nouveau score
float lapTime = 65.5f; // 1:05:500
string playerName = "Player1";
float[] checkpointTimes = new float[] { 20f, 40f, 60f };

bool isTopScore = HighscoreManager.Instance.TryAddScore(
    circuitName,
    lapTime,
    playerName,
    checkpointTimes
);

if (isTopScore)
{
    Debug.Log("Nouveau highscore enregistré!");
    
    // Rafraîchir l'affichage si visible
    if (highscoreDisplayUI != null)
    {
        highscoreDisplayUI.Refresh();
    }
}
```

---

## 🎨 Code Couleur

Le système applique automatiquement les couleurs selon le rang:

| Rang | Couleur | Signification | Alpha |
|------|---------|---------------|-------|
| **1** | 🟣 Mauve/Purple | RECORD! | 30/255 |
| **2-3** | 🟢 Vert | Top tier | 30/255 |
| **4-10** | 🔵 Bleu | Top 10 | 30/255 |
| **11+** | 🔴 Rouge | Hors top 10 | 30/255 |

Ces couleurs sont configurables dans l'Inspector du prefab HighscoreItem.

---

## 📊 Format d'Affichage

Chaque ligne affiche:

```
#Rang  |  Nom du Joueur  |  MM:SS:mmm  |  jj/mm/aaaa
```

**Exemple:**
```
#1  |  SpeedMaster  |  01:05:234  |  17/02/2026
#2  |  RacerPro     |  01:06:891  |  16/02/2026
#3  |  FastDriver   |  01:08:456  |  15/02/2026
```

---

## 🔍 Debug & Test

### Context Menu dans l'Editor

**HighscoreDisplayUI:**
- `Force Refresh Display` - Rafraîchir manuellement
- `Test with Dummy Data` - Créer 10 scores de test

**HighscoreItemUI:**
- `Test Rank 1 Color` - Tester couleur rang 1
- `Test Rank 2-3 Color` - Tester couleur rangs 2-3
- `Test Rank 4-10 Color` - Tester couleur rangs 4-10
- `Test Out of Top 10 Color` - Tester couleur hors top 10

### Logs Console

Le système génère des logs détaillés:

```
[HighscoreDisplayUI] Dropdown initialisé avec 3 circuits.
[HighscoreDisplayUI] Dropdown défini sur le circuit actuel: Mountain Circuit
[HighscoreDisplayUI] Circuit sélectionné: Desert Circuit
[HighscoreDisplayUI] Rafraîchissement de l'affichage pour: Desert Circuit
[HighscoreDisplayUI] 5 highscores affichés pour Desert Circuit
```

### Vérifier les Données

Dans l'Inspector, sélectionnez le HighscoreManager:
- Clic droit → `Debug: Display All Highscores`
- Affiche tous les scores dans la console

---

## 🚀 Workflow Complet

### 1. Setup Initial (Une fois)
```
1. Créer le prefab HighscoreItem
2. Créer la hiérarchie UI (Panel + Dropdown + HighscoreList)
3. Configurer HighscoreDisplayUI
4. Assigner les références
```

### 2. Pendant le Jeu
```
1. Le joueur termine un tour
2. RaceManager/LapTimer appelle HighscoreManager.TryAddScore()
3. Si c'est un top score:
   a. Le score est sauvegardé
   b. L'UI se rafraîchit automatiquement (si visible)
4. Le joueur peut changer de circuit via dropdown
5. L'UI se rafraîchit avec les nouveaux scores
```

### 3. Sauvegarde Automatique
- Les scores sont sauvegardés dans PlayerPrefs
- Format: `Highscore_CircuitName_Index`
- Persistance entre les sessions de jeu

---

## ⚙️ Configuration Avancée

### Désactiver le Rafraîchissement Auto

```csharp
// Dans l'Inspector
Refresh On Enable: décoché

// Rafraîchir manuellement
display.Refresh();
```

### Utiliser un Circuit Fixe

```csharp
// Dans l'Inspector
Use Current Circuit As Default: décoché

// Le dropdown commencera sur le premier circuit de la liste
```

### Personnaliser les Couleurs

Dans le prefab HighscoreItem → HighscoreItemUI component:
- Modifier les couleurs dans l'Inspector
- Tester avec les Context Menu

### Changer le Format d'Affichage

Modifier `HighscoreItemUI.Setup()`:

```csharp
// Format actuel
string displayText = $"#{entry.rank}  |  {entry.playerName}  |  {entry.FormattedTime}  |  {dateString}";

// Format personnalisé (exemple)
string displayText = $"{entry.rank}. {entry.playerName} - {entry.FormattedTime} ({dateString})";
```

---

## 🐛 Troubleshooting

### Les scores ne s'affichent pas

**Vérifier:**
1. HighscoreManager est dans la scène
2. CircuitDatabase est dans Resources/
3. CircuitDatabase contient des circuits
4. Les références dans HighscoreDisplayUI sont assignées
5. Le circuit sélectionné a des scores (tester avec dummy data)

### Le dropdown est vide

**Vérifier:**
1. CircuitDatabase.Instance != null
2. CircuitDatabase contient des CircuitData
3. Console pour les erreurs de chargement

### Les couleurs ne s'affichent pas

**Vérifier:**
1. RawImage est assignée dans HighscoreItemUI
2. Alpha est bien à 30/255 (0.117)
3. Canvas RenderMode permet les couleurs

### La date est incorrecte

**Vérifier:**
1. Le format sauvegardé: "dd/MM/yyyy"
2. Anciennes sauvegardes (avant date) utilisent date actuelle
3. PlayerPrefs sont à jour

---

## 📝 Notes Techniques

### Format de Sauvegarde

```
Clé: Highscore_CircuitName_Index
Valeur: "MM:SS:mmm|PlayerName|CP1,CP2,CP3|dd/MM/yyyy"

Exemple:
Highscore_Mountain_0: "01:05:234|SpeedMaster|20.5,40.2,60.1|17/02/2026"
```

### Compatibilité Rétrograde

Le système est compatible avec les anciennes sauvegardes sans date:
- Si la date est absente, elle est remplacée par la date actuelle
- Les scores existants continuent de fonctionner

### Performance

- Pas de Update() loop
- Instanciation à la demande
- Destruction propre des items lors du rafraîchissement
- Optimisé pour 10 entrées max par circuit

---

## ✅ Checklist de Validation

Avant de déclarer le système terminé:

- [ ] Le prefab HighscoreItem est créé avec tous les composants
- [ ] Les couleurs s'affichent correctement (test avec Context Menu)
- [ ] Le dropdown se remplit avec les circuits
- [ ] La sélection d'un circuit change la liste
- [ ] Les scores s'affichent triés (meilleur en haut)
- [ ] Le format est correct: `#Rang | Nom | Temps | Date`
- [ ] Les dates s'affichent au format jj/mm/aaaa
- [ ] L'alpha des couleurs est à 30/255
- [ ] Le circuit actuel est sélectionné par défaut
- [ ] L'ajout d'un score rafraîchit l'UI

---

## 🎉 Résultat Final

Un système d'affichage de highscores:
- ✅ **Simple** - Configuration rapide
- ✅ **Élégant** - Code couleur visuel
- ✅ **Fonctionnel** - Rafraîchissement automatique
- ✅ **Complet** - Toutes les infos nécessaires
- ✅ **Intégré** - Fonctionne avec le système existant

**Prêt à utiliser ! 🏁**
