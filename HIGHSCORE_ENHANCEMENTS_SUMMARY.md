# 🏆 Améliorations du Système de Highscore

## Résumé des Changements

Ce document décrit les améliorations apportées au système de highscore selon les spécifications demandées.

---

## 📋 Fonctionnalités Implémentées

### 1. ✅ Chargement des Temps de Checkpoint au Lancement du Circuit

**Problème:** Les temps de checkpoint des highscores n'étaient pas chargés au démarrage.

**Solution:** 
- `CheckpointTimingDisplay` charge automatiquement les temps de référence depuis les highscores lors du `Start()`
- S'abonne à l'événement `CircuitManager.OnCircuitLoaded` pour recharger les temps quand le circuit change
- Stocke en cache les temps du rank 1 et la moyenne des ranks 2-10 pour une comparaison efficace

**Fichiers modifiés:**
- `CheckpointTimingDisplay.cs`

### 2. ✅ Indicateur Visuel de Performance en Temps Réel

**Problème:** Le joueur ne savait pas comment son temps se comparait aux highscores pendant le tour.

**Solution:**
Nouveau système de couleurs basé sur la comparaison avec les highscores:

- **🟢 VERT:** Temps meilleur que le rank 1 (nouveau record en cours!)
- **🔵 BLEU:** Temps dans la moyenne des ranks 2-10 (bonne performance)
- **🔴 ROUGE:** Temps au-delà de la moyenne (peut s'améliorer)

**Calcul:**
1. Compare le temps actuel avec le rank 1
2. Si meilleur → VERT
3. Sinon, compare avec la moyenne des ranks 2-10
4. Si ≤ moyenne → BLEU
5. Si > moyenne → ROUGE

**Fichiers modifiés:**
- `CheckpointTimingDisplay.cs`
- `HighscoreManager.cs` (nouvelle méthode `GetAverageCheckpointTimes()`)

### 3. ✅ Correction du Bug: Premier Tour Non Comptabilisé

**Problème:** Le premier tour complété n'était jamais enregistré dans le highscore.

**Cause:** Le système vérifiait que `_vehicleHasLeftStart` soit `true` avant de compter un tour, mais ce flag n'était mis à `true` qu'après avoir passé un checkpoint > 0, ce qui empêchait le premier passage au CP0 de compter.

**Solution:**
Logique simplifiée dans `CheckpointManager.OnCheckpointPassed()`:

```csharp
// Premier passage au CP0: démarrer le timer
if (CP0 && !hasLeftStart) {
    StartTimer();
    hasLeftStart = true;
}
// Passages suivants au CP0: compter le tour
else if (CP0 && hasLeftStart) {
    OnLapCompleted();
}
// Autres checkpoints: enregistrer temps intermédiaire
else {
    RecordCheckpoint();
}
```

**Résultat:** Tous les tours sont maintenant correctement comptabilisés, y compris le premier.

**Fichiers modifiés:**
- `CheckpointManager.cs`

### 4. ✅ Démarrage du Chronomètre au CP0

**Problème:** Le chronomètre démarrait au début de la course (via `RaceManager.StartRace()`), ce qui n'était pas cohérent car le joueur devait d'abord atteindre le CP0.

**Solution:**
Séparation de la préparation et du démarrage réel du timer:

1. **`LapTimer.StartRace()`:** Prépare la course (nettoie les données, met `_isRacing = true`)
2. **`LapTimer.StartTimer()`:** Démarre réellement le chronomètre (appelé par CheckpointManager au passage du CP0)

**Flux mis à jour:**
```
RaceManager.StartRace()
  └─> LapTimer.StartRace()      // Préparation (timer pas encore démarré)

Joueur passe CP0
  └─> CheckpointManager.OnCheckpointPassed()
      └─> LapTimer.StartTimer()  // ⏱️ Timer démarre ICI!
```

**Résultat:** Le timer démarre maintenant au moment où le joueur franchit le CP0 pour la première fois.

**Fichiers modifiés:**
- `LapTimer.cs` (ajout de `_timerStarted`, séparation `StartRace()`/`StartTimer()`)
- `CheckpointManager.cs` (appel de `StartTimer()` au premier passage CP0)

---

## 📁 Fichiers Modifiés

### 1. `LapTimer.cs`
**Changements:**
- Ajout du flag `_timerStarted` pour tracker si le timer a été démarré
- Séparation de `StartRace()` (préparation) et `StartTimer()` (démarrage réel)
- Ajout de vérifications `_timerStarted` dans `RecordCheckpoint()` et `CompleteLap()`
- Mise à jour de `Reset()` pour réinitialiser `_timerStarted`

**Impact:** Le timer ne démarre plus automatiquement, mais attend le passage du CP0.

### 2. `CheckpointManager.cs`
**Changements:**
- Logique simplifiée dans `OnCheckpointPassed()`:
  - Premier CP0: `StartTimer()` + marquer `hasLeftStart = true`
  - CP0 suivants: `OnLapCompleted()`
  - Autres checkpoints: `RecordCheckpoint()`
- Suppression de la logique complexe qui empêchait le premier tour d'être compté

**Impact:** Tous les tours sont correctement comptabilisés, timer démarre au bon moment.

### 3. `HighscoreManager.cs`
**Changements:**
- Ajout de `GetAverageCheckpointTimes()`:
  - Exclut le rank 1
  - Calcule la moyenne des ranks 2-10 pour chaque checkpoint
  - Retourne un tableau de temps moyens

**Impact:** Permet la comparaison avec la moyenne des autres highscores.

### 4. `CheckpointTimingDisplay.cs`
**Changements:**
- Refonte complète du système de couleurs
- Ajout de `LoadReferenceTimesFromHighscores()`:
  - Charge les temps du rank 1
  - Charge les temps moyens des ranks 2-10
- Ajout de `GetComparisonColor()`:
  - Compare avec rank 1 (si meilleur → VERT)
  - Compare avec moyenne (si ≤ moyenne → BLEU, sinon → ROUGE)
- Abonnement à `CircuitManager.OnCircuitLoaded` pour recharger les temps

**Impact:** Affichage en temps réel de la performance du joueur vs les highscores.

---

## 🎮 Flux de Jeu Mis à Jour

### Démarrage de la Course

```
1. RaceManager.StartCountdown()
   └─> Compte à rebours (3, 2, 1...)

2. RaceManager.StartRace()
   └─> LapTimer.StartRace()         // ⚠️ Timer PAS encore démarré!
       └─> _isRacing = true
       └─> _timerStarted = false

3. Joueur atteint CP0
   └─> CheckpointManager.OnCheckpointPassed()
       └─> LapTimer.StartTimer()    // ✅ Timer démarre ICI!
           └─> _raceStartTime = Time.time
           └─> _timerStarted = true
```

### Passage de Checkpoints

```
Joueur passe CP1, CP2, ..., CPN
  └─> CheckpointManager.OnCheckpointPassed()
      └─> LapTimer.RecordCheckpoint()
          └─> checkpointTime = Time.time - _currentLapStartTime
          └─> _currentLapCheckpointTimes.Add(checkpointTime)
```

### Affichage en Temps Réel

```
CheckpointTimingDisplay.Update() (chaque 0.1s)
  └─> UpdateDisplay()
      └─> Pour chaque checkpoint:
          └─> GetComparisonColor()
              ├─> Si temps < rank1Time → VERT
              ├─> Si temps ≤ averageTime → BLEU
              └─> Si temps > averageTime → ROUGE
```

### Complétion d'un Tour

```
Joueur repasse CP0
  └─> CheckpointManager.OnCheckpointPassed()
      └─> OnLapCompleted()
          └─> RaceManager.OnLapCompleted()
              └─> LapTimer.CompleteLap()
                  ├─> lapTime = Time.time - _currentLapStartTime
                  ├─> _lapTimes.Add(lapTime)
                  ├─> _allLapsCheckpointTimes.Add(...)
                  └─> _currentLapStartTime = Time.time  // Nouveau tour commence
```

---

## ✅ Tests Recommandés

### Test 1: Démarrage du Timer au CP0
1. Lancer une course
2. **Vérifier:** Console affiche "[CheckpointManager] ... started timer at CP0 ⏱️"
3. **Vérifier:** Le temps affiché est 00:00.000 AVANT de passer CP0
4. **Vérifier:** Le temps commence à augmenter APRÈS avoir passé CP0

### Test 2: Premier Tour Comptabilisé
1. Faire un tour complet du circuit
2. Passer le CP0 à la fin du tour
3. **Vérifier:** Console affiche "[CheckpointManager] ... completed lap at CP0 🏁"
4. **Vérifier:** Console affiche le temps du tour (ex: "01:23.456")
5. **Vérifier:** Le compteur de tours passe de 0 à 1

### Test 3: Couleurs de Performance
**Prérequis:** Avoir au moins un highscore enregistré pour le circuit

1. Faire un tour en essayant d'aller vite
2. **Vérifier:** Les checkpoints affichent des couleurs:
   - Si très rapide: texte VERT
   - Si moyen: texte BLEU
   - Si lent: texte ROUGE
3. Refaire un tour en allant plus lentement
4. **Vérifier:** Les couleurs changent selon la performance

### Test 4: Chargement des Temps de Référence
1. Avoir des highscores enregistrés pour un circuit
2. Charger ce circuit
3. **Vérifier:** Console affiche:
   - "[CheckpointTimingDisplay] Loaded rank 1 checkpoint times for [NomCircuit]: X checkpoints"
   - "[CheckpointTimingDisplay] Loaded average checkpoint times for [NomCircuit]: X checkpoints"
4. **Vérifier:** Les couleurs s'affichent correctement dès le premier tour

### Test 5: Changement de Circuit
1. Charger un circuit A avec des highscores
2. **Vérifier:** Les temps de référence du circuit A sont chargés
3. Charger un circuit B (avec des highscores différents)
4. **Vérifier:** Console affiche "[CheckpointTimingDisplay] Circuit loaded: '[CircuitB]'. Reloading reference times..."
5. **Vérifier:** Les couleurs correspondent aux highscores du circuit B

---

## 🎨 Exemple Visuel

### Affichage Pendant le Tour

```
╔═══════════════════════════════════╗
║   CHECKPOINT TIMES                ║
╠═══════════════════════════════════╣
║  CP1: 00:15.234  [VERT]          ║ ← Meilleur que rank 1!
║  CP2: 00:31.567  [BLEU]          ║ ← Dans la moyenne
║  CP3: 00:48.901  [ROUGE]         ║ ← Au-delà de la moyenne
║  CP4: --:--.---  [BLANC]         ║ ← Pas encore passé
╚═══════════════════════════════════╝
```

### Interprétation des Couleurs

- **🟢 VERT:** "Excellent! Vous battez le record actuel!"
- **🔵 BLEU:** "Bon temps, vous êtes dans le top 10"
- **🔴 ROUGE:** "Vous pouvez faire mieux, accélérez!"

---

## 🔧 Configuration dans Unity

### CheckpointTimingDisplay

**Paramètres Inspector:**

```
=== REFERENCES ===
✓ Checkpoint Time Texts     : TextMeshProUGUI[]  (tableau de textes UI)
✓ Lap Timer                 : LapTimer           (auto-trouvé si non assigné)

=== COLORS ===
✓ Default Color             : Blanc
✓ Better Than Rank1 Color   : Vert    (RGB: 0, 255, 0)
✓ Average Color             : Bleu    (RGB: 0, 128, 255)
✓ Worse Color               : Rouge   (RGB: 255, 0, 0)

=== SETTINGS ===
✓ Circuit Name              : (auto-détecté depuis CircuitManager)
✓ Auto Update               : ✓
✓ Update Interval           : 0.1
```

**Note:** Les couleurs peuvent être ajustées dans l'Inspector selon les préférences visuelles.

---

## 📊 Calcul de la Moyenne

### Méthode: `HighscoreManager.GetAverageCheckpointTimes()`

**Algorithme:**
1. Récupérer tous les highscores du circuit
2. Exclure le rank 1 (on compare avec les "autres")
3. Prendre les ranks 2 à 10 (jusqu'à 9 scores)
4. Pour chaque checkpoint:
   - Sommer les temps de tous les scores disponibles
   - Diviser par le nombre de scores
5. Retourner le tableau des moyennes

**Exemple:**

```
Rank 1: CP1=15.0s, CP2=30.0s, CP3=45.0s
Rank 2: CP1=16.0s, CP2=32.0s, CP3=48.0s
Rank 3: CP1=17.0s, CP2=33.0s, CP3=49.0s
...
Rank 10: CP1=24.0s, CP2=40.0s, CP3=56.0s

Moyenne (ranks 2-10):
CP1 = (16.0 + 17.0 + ... + 24.0) / 9 = 20.0s
CP2 = (32.0 + 33.0 + ... + 40.0) / 9 = 36.0s
CP3 = (48.0 + 49.0 + ... + 56.0) / 9 = 52.0s
```

**Joueur passe CP1 en 18.0s:**
- Meilleur que rank 1 (15.0s)? Non
- ≤ Moyenne (20.0s)? Oui → **BLEU**

---

## 🐛 Bugs Corrigés

### Bug #1: Premier Tour Non Comptabilisé ✅

**Symptôme:** Le compteur de tours restait à 0 après le premier tour complet.

**Cause:** Logique incorrecte dans `CheckpointManager` qui ne permettait pas de compter le tour si `_vehicleHasLeftStart` était `false`.

**Solution:** Simplification de la logique:
- Premier CP0: marquer `hasLeftStart = true`
- CP0 suivants: compter le tour si `hasLeftStart == true`

### Bug #2: Timer Démarre Trop Tôt ✅

**Symptôme:** Le timer démarrait au début de la course, avant que le joueur n'atteigne le CP0.

**Cause:** `RaceManager.StartRace()` appelait `LapTimer.StartRace()` qui démarrait immédiatement le timer.

**Solution:** Séparation de la préparation (`StartRace()`) et du démarrage réel (`StartTimer()`).

---

## 🚀 Améliorations Futures Possibles

### 1. Affichage du Delta vs Rank 1
Afficher "+0.5s" ou "-0.3s" à côté du temps pour montrer la différence avec le rank 1.

### 2. Indicateur de Tendance
Afficher une flèche ↑ (s'améliore) ou ↓ (se dégrade) entre les checkpoints.

### 3. Ghost Race
Afficher un véhicule fantôme qui suit le temps du rank 1.

### 4. Prédiction du Temps Final
Estimer le temps final du tour basé sur les checkpoints actuels et la moyenne.

### 5. Audio Feedback
Jouer un son différent selon la couleur (encouragement, avertissement).

---

## 📚 Références

### Scripts Principaux

- `LapTimer.cs` - Gestion du chronomètre
- `CheckpointManager.cs` - Validation des passages de checkpoints
- `HighscoreManager.cs` - Stockage et récupération des highscores
- `CheckpointTimingDisplay.cs` - Affichage UI avec couleurs

### Événements

- `CircuitManager.OnCircuitLoaded` - Déclenché quand un circuit est chargé
- `RaceManager.OnLapCompleted` - Déclenché quand un tour est complété

### Dépendances

- TextMeshPro (TMPro) pour l'affichage UI
- CircuitManager pour les données de circuit
- PlayerPrefs pour la sauvegarde des highscores

---

## ✅ Résumé

### Ce qui a été implémenté:

1. ✅ Chargement automatique des temps de checkpoint au démarrage
2. ✅ Comparaison en temps réel avec les highscores
3. ✅ Système de couleurs (Vert/Bleu/Rouge) selon la performance
4. ✅ Correction du bug du premier tour non comptabilisé
5. ✅ Démarrage du timer au passage du CP0 (plus cohérent)

### Résultats:

- Le joueur sait immédiatement s'il est sur un bon temps
- Le système de chronomètre est plus cohérent et logique
- Tous les tours sont correctement comptabilisés
- Les temps de référence sont chargés automatiquement

### Prêt pour:

- Tests en jeu
- Ajustements visuels des couleurs
- Feedback des joueurs
- Améliorations futures

---

**Date d'implémentation:** 18 février 2026  
**Statut:** ✅ Complet et prêt pour tests  
**Fichiers modifiés:** 4 (LapTimer.cs, CheckpointManager.cs, HighscoreManager.cs, CheckpointTimingDisplay.cs)  
**Impact:** Amélioration majeure de l'expérience joueur
