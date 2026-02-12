# Système de Gestion des Circuits - Quick Start

## 🎯 Fonctionnalités Ajoutées

### 1. CircuitDatabase - Source Unique de Vérité
- **Fichier**: `Scripts/Settings/CircuitDatabase.cs`
- **Usage**: ScriptableObject centralisant tous les circuits
- **Installation**: Créer dans `Assets/Resources/CircuitDatabase.asset`
- **Accès**: `CircuitDatabase.Instance.AvailableCircuits`

### 2. HighscoreManager - Système de Classement
- **Fichier**: `Scripts/Core/HighscoreManager.cs`
- **Usage**: Singleton gérant les top 10 par circuit
- **Format**: **MM:SS:mmm** (minutes:secondes:millièmes) ✅
- **Stockage**: PlayerPrefs local
- **API**:
  ```csharp
  bool TryAddScore(string circuitName, float time, string playerName)
  List<HighscoreEntry> GetHighscores(string circuitName)
  HighscoreEntry? GetBestTime(string circuitName)
  ```

### 3. CircuitThumbnailGenerator - Générateur de Miniatures
- **Fichier**: `Scripts/Track/Editor/CircuitThumbnailGenerator.cs`
- **Usage**: Outil d'éditeur pour générer des sprites 256x256
- **Rendu**: Tracé noir sur fond blanc (alpha 0.5)
- **Utilisation**: 
  - Bouton "Generate Thumbnail" dans l'inspecteur CircuitData
  - Menu contextuel: Clic droit → "Generate Circuit Thumbnail"

### 4. CircuitSelectionUI - Interface de Sélection
- **Fichiers**: 
  - `Scripts/UI/CircuitSelectionUI.cs`
  - `Scripts/UI/CircuitSelectionItem.cs`
- **Usage**: Génère automatiquement une grille de circuits clickables
- **Affichage**: Thumbnail + Nom du circuit
- **Container**: GridLayoutGroup
- **Integration**: `UIManager.ShowCircuitSelection()`

## 📦 Installation Rapide

1. **Créer CircuitDatabase**:
   ```
   Clic droit → Create → Arcade Racer → Circuit Database
   Placer dans: Assets/Resources/CircuitDatabase.asset
   Ajouter vos circuits dans la liste
   ```

2. **Générer les Thumbnails**:
   ```
   Sélectionner un CircuitData → Inspector → "Generate Thumbnail"
   Ou: Clic droit sur CircuitData → "Generate Circuit Thumbnail"
   ```

3. **Configurer l'UI de Sélection**:
   ```
   Canvas → Panel → GridContainer (avec GridLayoutGroup)
   Ajouter CircuitSelectionUI sur le Panel
   Créer un Prefab CircuitSelectionItem
   Assigner les références
   ```

4. **Tester les Highscores**:
   ```csharp
   HighscoreManager.Instance.TryAddScore("Circuit1", 65.432f, "Player1");
   var scores = HighscoreManager.Instance.GetHighscores("Circuit1");
   ```

## 🔗 Intégration

- **UIManager** étendu avec:
  - `ShowCircuitSelection()`
  - `HideCircuitSelection()`
  - Auto-find de `CircuitSelectionUI`

- **Format de temps unifié**:
  - `HighscoreEntry.FormatTime(float)` → "MM:SS:mmm"
  - `HighscoreEntry.ParseTime(string)` → float
  - Compatible avec `LapTimer.FormatTime()`

## 📚 Documentation Complète

Voir: `DOCUMENTATION_SYSTEME_CIRCUITS.md` pour tous les détails, exemples de code, et workflow complet.

## ✅ Ajustement Validé

**Format de temps pour highscores: MM:SS:mmm** ✅
- Minutes (2 chiffres)
- Secondes (2 chiffres)
- Millièmes (3 chiffres)
- Exemple: `01:23:456`
