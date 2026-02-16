# 🏗️ Guide Level Designer - Création et Édition de Circuits

## 📋 Vue d'Ensemble

Le **CircuitBuilder** est l'outil unifié pour créer et éditer des circuits. Il garantit que le preview dans l'éditeur correspond **exactement** au résultat en jeu.

---

## 🎯 Modes de Fonctionnement

Le CircuitBuilder détecte automatiquement le mode :

- **CRÉATION** 🟢 : CircuitData vide → Nouveau circuit
- **ÉDITION** 🔵 : CircuitData avec données → Modification circuit existant

---

## 🆕 Créer un Nouveau Circuit

### Étape 1 : Créer le CircuitData
1. Sélectionner l'objet avec le component `CircuitBuilder` dans la scène CircuitEditor
2. Dans l'Inspector :
   - Entrer un nom pour le nouveau circuit (ex: "MonSuperCircuit")
   - Cliquer sur **✨ CRÉER NOUVEAU CIRCUIT**
3. Le CircuitData est créé automatiquement dans `Assets/Project/Settings/Circuits/`

### Étape 2 : Éditer la Spline
1. Utiliser l'outil **Spline** de Unity (dans la barre d'outils)
2. Ajouter/modifier des points pour créer le tracé du circuit
3. Ajuster les tangentes pour des courbes fluides

### Étape 3 : Preview
1. Cliquer sur **🔍 Generate Preview**
2. Le mesh du circuit s'affiche dans la scène
3. ⚠️ **IMPORTANT** : Ce preview utilise les **mêmes paramètres** que le jeu !
4. Ajuster la spline si nécessaire et régénérer le preview

### Étape 4 : Spawn Point
1. Cliquer sur **📍 Create Spawn Point**
2. Positionner et orienter le spawn point manuellement dans la scène
3. Ce point définira où les véhicules apparaissent

### Étape 5 : Checkpoints
1. Cliquer sur **🚦 Generate Checkpoint Preview**
2. Les checkpoints apparaissent automatiquement sur le circuit
3. Ajuster manuellement leur position/rotation si nécessaire
4. Cliquer sur **💾 Save Checkpoints to CircuitData**

### Étape 6 : Export Final
1. Cliquer sur **💾 Export to CircuitData**
2. Le circuit est sauvegardé dans le fichier CircuitData
3. Ajouter le CircuitData à la `CircuitDatabase` (dans Resources)

---

## ✏️ Éditer un Circuit Existant

### Étape 1 : Charger le CircuitData
1. Dans l'Inspector, assigner le CircuitData existant
2. Le mode **ÉDITION** 🔵 s'affiche automatiquement

### Étape 2 : Charger dans l'Éditeur
1. Cliquer sur **📥 Load from CircuitData**
2. La spline se reconstruit automatiquement
3. Le spawn point est repositionné

### Étape 3 : Modifier
1. Éditer la spline avec l'outil Unity Spline
2. Ajuster le spawn point si nécessaire
3. Cliquer sur **🔍 Generate Preview** pour voir les changements

### Étape 4 : Checkpoints (Optionnel)
1. Si nécessaire, régénérer les checkpoints
2. Ou charger les checkpoints existants et les ajuster
3. Sauvegarder avec **💾 Save Checkpoints to CircuitData**

### Étape 5 : Sauvegarder
1. Cliquer sur **💾 Export to CircuitData**
2. Le CircuitData est mis à jour (écrase les anciennes données)

---

## 🎨 Interface CircuitBuilder

### Section "Actions Principales"
- **📥 Load from CircuitData** (Mode ÉDITION uniquement) : Charge un circuit existant
- **🔍 Generate Preview** : Affiche le mesh du circuit (identique au jeu !)
- **💾 Export to CircuitData** : Sauvegarde les modifications

### Section "Gestion Checkpoints"
- **🚦 Generate Checkpoint Preview** : Génère checkpoints automatiques
- **💾 Save Checkpoints to CircuitData** : Sauvegarde positions relatives

### Section "Utilitaires"
- **🧹 Clear Preview** : Nettoie le preview de la scène
- **📍 Create Spawn Point** : Crée un point de spawn

---

## ✅ Bonnes Pratiques

### Nommage
- Utiliser des noms clairs : "Circuit_Desert", "Circuit_Montagne", etc.
- Éviter les espaces et caractères spéciaux

### Spline
- Minimum 4 points pour un circuit fermé
- Utiliser "Closed Loop" pour circuits en boucle
- Ajuster les tangentes pour des courbes naturelles

### Checkpoints
- Nombre recommandé : 8-12 pour un circuit moyen
- Premier checkpoint = Start/Finish (automatique)
- Espacement régulier le long du circuit

### Preview vs Runtime
- **Garantie** : Le preview utilise exactement les mêmes paramètres que le jeu
- Si ça marche en preview, ça marchera en jeu ! ✓

---

## ⚙️ Configuration Technique

### Paramètres Partagés (Éditeur = Runtime)
Ces paramètres sont définis dans `CircuitGenerationConstants.cs` :

- **Segments par point** : 10
- **Qualité des courbes** : 10
- **UV Tiling** : 1.0 x 0.5

Ces valeurs garantissent que le mesh généré est identique entre preview et jeu.

### Différences Preview/Runtime
- **Colliders** : Pas générés en preview (performance)
- Tout le reste est **identique**

---

## 🐛 Dépannage

### "No CircuitData to load"
→ Assigner un CircuitData dans l'Inspector

### "CircuitData ne contient aucun point de spline"
→ Le circuit n'a pas encore été exporté, utiliser mode CRÉATION

### Les checkpoints ne se sauvegardent pas
→ Vérifier que le SpawnPoint existe (nécessaire pour positions relatives)

### Le preview est différent du jeu
→ **NE DEVRAIT PAS ARRIVER** avec la nouvelle architecture !
→ Vérifier CircuitGenerationConstants.cs si problème

---

## 📁 Structure des Fichiers

```
Assets/Project/
├── Settings/
│   ├── CircuitGenerationConstants.cs  ← Configuration partagée
│   └── Circuits/
│       ├── MonCircuit1.asset
│       └── MonCircuit2.asset
│
├── Scripts/Track/TrackBuilder/
│   ├── CircuitBuilder.cs              ← Outil principal
│   ├── CircuitBuilderEditor.cs        ← Interface Unity
│   └── CircuitMeshGenerator.cs        ← Génération mesh
│
└── Scene/Core/
    └── CircuitEditor.unity             ← Scène d'édition
```

---

## 🎓 Workflow Complet - Exemple

```
1. Ouvrir CircuitEditor.unity
2. Sélectionner l'objet CircuitBuilder
3. Créer "Circuit_Test" 
4. Éditer spline (8 points, boucle fermée)
5. Generate Preview → Vérifier rendu
6. Create Spawn Point → Positionner début circuit
7. Generate Checkpoint Preview → 10 checkpoints
8. Save Checkpoints
9. Export to CircuitData
10. Ouvrir CircuitDatabase (Resources)
11. Ajouter Circuit_Test à la liste
12. Tester en jeu !
```

---

## 🚀 Nouveautés de cette Version

✅ **Mode édition** : Charger et modifier circuits existants
✅ **Preview = Jeu** : Garantie de cohérence
✅ **Checkpoints visuels** : Placement et ajustement dans l'éditeur
✅ **Un seul outil** : Plus besoin de CircuitEditorTool
✅ **Interface améliorée** : Sections claires, boutons organisés

---

## 📞 Support

En cas de problème :
1. Vérifier que CircuitBuilder est sur l'objet racine de la scène
2. Vérifier que SplineContainer est présent
3. Consulter la console Unity pour messages d'erreur
4. Vérifier CircuitGenerationConstants.cs pour configuration

Bon level design ! 🎮
