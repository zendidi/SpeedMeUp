# 🧪 Guide de Test - Améliorations Highscore

Ce document fournit des procédures détaillées pour tester les nouvelles fonctionnalités implémentées.

---

## 📋 Checklist de Test Rapide

Utilisez cette checklist pour vérifier que toutes les fonctionnalités fonctionnent:

- [ ] Timer démarre au passage du CP0 (pas avant)
- [ ] Premier tour est bien comptabilisé
- [ ] Couleurs s'affichent correctement (vert/bleu/rouge)
- [ ] Temps de référence se chargent au changement de circuit
- [ ] Tous les checkpoints intermédiaires sont enregistrés

---

## 🎯 Tests Détaillés

### Test 1: Démarrage du Timer au CP0 ⏱️

**Objectif:** Vérifier que le chronomètre démarre uniquement au passage du CP0.

**Procédure:**
1. Lancer Unity et ouvrir une scène avec un circuit
2. Lancer la course (Play Mode)
3. Observer le HUD du chronomètre
4. **Vérifier:** Le temps affiche "00:00.000" tant que vous n'avez pas franchi le CP0
5. Avancer jusqu'au CP0 (ligne de départ/arrivée)
6. **Vérifier:** Au passage du CP0, le chronomètre démarre
7. Vérifier dans la Console:
   ```
   [CheckpointManager] PlayerCar started timer at CP0 ⏱️
   [LapTimer] PlayerCar - Timer started!
   ```

**Résultat attendu:**
- ✅ Timer reste à 0 jusqu'au CP0
- ✅ Timer démarre au passage du CP0
- ✅ Messages de log confirmant le démarrage

**Si ça ne marche pas:**
- Vérifier que LapTimer a bien la méthode `StartTimer()`
- Vérifier que CheckpointManager appelle `StartTimer()` au bon moment
- Vérifier que le CP0 est bien configuré avec `IsStartFinishLine = true`

---

### Test 2: Premier Tour Comptabilisé 🏁

**Objectif:** Confirmer que le premier tour est correctement enregistré.

**Procédure:**
1. Démarrer une nouvelle course
2. Passer le CP0 pour démarrer le timer
3. Faire un tour complet du circuit
4. Repasser le CP0 à la fin du tour
5. **Vérifier:** Console affiche:
   ```
   [CheckpointManager] PlayerCar completed lap at CP0 🏁
   [LapTimer] X.XXX seconds - completed in MM:SS.mmm
   [LapTimer] PlayerCar - Lap 1 completed in MM:SS.mmm
   [RaceManager] PlayerCar completed lap 1/3
   ```
6. **Vérifier:** Le compteur de tours passe de 0 à 1

**Résultat attendu:**
- ✅ Premier tour est comptabilisé
- ✅ Temps du tour est affiché (non zéro)
- ✅ Compteur de tours s'incrémente

**Si ça ne marche pas:**
- Vérifier que tous les checkpoints ont été passés dans l'ordre
- Vérifier que `_vehicleHasLeftStart` est mis à `true` après le premier passage au CP0
- Vérifier les logs pour voir quel checkpoint cause le problème

---

### Test 3: Système de Couleurs 🎨

**Objectif:** Vérifier que les couleurs de performance s'affichent correctement.

**Prérequis:**
- Avoir au moins 2-3 highscores enregistrés pour le circuit
- Ces highscores doivent avoir des temps de checkpoints

**Procédure:**

**3A. Vérification du Chargement:**
1. Ouvrir la Console Unity (Window > General > Console)
2. Charger un circuit avec des highscores
3. **Vérifier:** Console affiche:
   ```
   [CheckpointTimingDisplay] Loaded rank 1 checkpoint times for [CircuitName]: X checkpoints
   [CheckpointTimingDisplay] Loaded average checkpoint times for [CircuitName]: X checkpoints
   ```

**3B. Test Performance Rapide (Couleur VERTE):**
1. Faire un tour très rapide
2. **Vérifier:** Les temps de checkpoints s'affichent en VERT
3. **Signification:** Votre temps bat le record actuel!

**3C. Test Performance Moyenne (Couleur BLEUE):**
1. Faire un tour à vitesse normale
2. **Vérifier:** Les temps s'affichent en BLEU
3. **Signification:** Vous êtes dans la moyenne du top 10

**3D. Test Performance Lente (Couleur ROUGE):**
1. Faire un tour lentement
2. **Vérifier:** Les temps s'affichent en ROUGE
3. **Signification:** Vous êtes en-dessous de la moyenne

**Résultat attendu:**
- ✅ Couleur VERTE pour temps meilleur que rank 1
- ✅ Couleur BLEUE pour temps dans la moyenne
- ✅ Couleur ROUGE pour temps au-delà de la moyenne
- ✅ Couleurs changent en temps réel selon la performance

**Si ça ne marche pas:**
- Vérifier que le circuit a des highscores avec checkpoint times
- Vérifier que `CheckpointTimingDisplay.circuitName` est correctement défini
- Vérifier dans l'Inspector que les couleurs sont bien assignées
- Vérifier que les TextMeshProUGUI sont assignés dans le tableau

---

### Test 4: Changement de Circuit 🔄

**Objectif:** Vérifier que les temps de référence se rechargent au changement de circuit.

**Procédure:**
1. Charger le Circuit A
2. **Vérifier:** Console affiche le chargement des temps pour Circuit A
3. Faire quelques checkpoints pour voir les couleurs
4. Charger le Circuit B (différent)
5. **Vérifier:** Console affiche:
   ```
   [CheckpointTimingDisplay] Circuit loaded: '[CircuitB]'. Reloading reference times...
   [CheckpointTimingDisplay] Loaded rank 1 checkpoint times for CircuitB: X checkpoints
   ```
6. Faire quelques checkpoints
7. **Vérifier:** Les couleurs correspondent aux highscores du Circuit B

**Résultat attendu:**
- ✅ Temps de référence mis à jour automatiquement
- ✅ Couleurs correspondent au nouveau circuit
- ✅ Aucune erreur dans la console

---

### Test 5: Enregistrement Highscores 💾

**Objectif:** Vérifier que les nouveaux temps sont correctement enregistrés.

**Procédure:**
1. Terminer une course complète (ex: 3 tours)
2. **Vérifier:** Console affiche à la fin:
   ```
   🏆 [RaceManager] PlayerCar finished in position 1!
   🏆 [RaceManager] Nouveau highscore pour [CircuitName]: MM:SS.mmm - [PlayerName]
   ```
3. Utiliser le context menu: RightClick sur HighscoreManager > "Debug: Display All Highscores"
4. **Vérifier:** Le nouveau temps apparaît dans la liste
5. Recharger le circuit
6. **Vérifier:** Les temps de référence incluent votre nouveau temps

**Résultat attendu:**
- ✅ Temps enregistré dans le HighscoreManager
- ✅ Checkpoint times inclus dans l'enregistrement
- ✅ Temps chargé correctement au prochain démarrage

---

## 🐛 Tests de Cas Limites

### Test 6: Circuit Sans Highscores

**Procédure:**
1. Créer/charger un nouveau circuit sans highscores
2. Démarrer une course
3. **Vérifier:** Aucune erreur dans la console
4. **Vérifier:** Les textes de checkpoint affichent la couleur par défaut (blanc)

**Résultat attendu:**
- ✅ Pas d'erreur NullReference
- ✅ Couleur par défaut affichée
- ✅ Le jeu fonctionne normalement

---

### Test 7: Passage de Checkpoints dans le Mauvais Ordre

**Procédure:**
1. Démarrer une course
2. Essayer de passer les checkpoints dans le mauvais ordre
3. **Vérifier:** Console affiche des warnings:
   ```
   [CheckpointManager] PlayerCar passed checkpoint X but expected Y ❌
   ```
4. **Vérifier:** Le tour n'est pas comptabilisé

**Résultat attendu:**
- ✅ Checkpoints invalides détectés
- ✅ Tours non comptabilisés si ordre incorrect
- ✅ Messages de warning dans la console

---

### Test 8: Redémarrage de Course

**Procédure:**
1. Démarrer une course
2. Faire quelques checkpoints
3. Appeler `RaceManager.RestartRace()` (context menu ou bouton)
4. **Vérifier:** Timer réinitialisé
5. **Vérifier:** Compteur de tours à 0
6. **Vérifier:** Checkpoint times effacés
7. Refaire un tour
8. **Vérifier:** Tout fonctionne normalement

**Résultat attendu:**
- ✅ Réinitialisation complète
- ✅ Pas de données résiduelles
- ✅ Nouveau tour fonctionne correctement

---

## 📊 Tests de Performance

### Test 9: Plusieurs Véhicules

**Objectif:** Vérifier que le système fonctionne avec plusieurs véhicules.

**Procédure:**
1. Ajouter 2-3 véhicules dans la scène
2. Enregistrer tous dans `RaceManager.racingVehicles`
3. Démarrer la course
4. **Vérifier:** Chaque véhicule a son propre timer
5. **Vérifier:** Les tours sont comptabilisés indépendamment

**Résultat attendu:**
- ✅ Chaque véhicule tracked indépendamment
- ✅ Pas de confusion entre les véhicules
- ✅ Highscores enregistrés pour chaque véhicule

---

### Test 10: Tours Multiples

**Procédure:**
1. Configurer une course de 5 tours
2. Terminer tous les tours
3. **Vérifier:** Tous les tours sont comptabilisés
4. **Vérifier:** Le meilleur tour est identifié correctement
5. **Vérifier:** Les checkpoint times du meilleur tour sont sauvegardés

**Résultat attendu:**
- ✅ Tous les tours comptés (1, 2, 3, 4, 5)
- ✅ Meilleur tour identifié
- ✅ Checkpoint times corrects dans le highscore

---

## 🔍 Vérification Visuelle

### Éléments UI à Vérifier

**CheckpointTimingDisplay:**
```
╔═══════════════════════════════════╗
║  CP1: 00:15.234  [VERT]          ║
║  CP2: 00:31.567  [BLEU]          ║
║  CP3: 00:48.901  [ROUGE]         ║
║  CP4: --:--.---  [BLANC]         ║
╚═══════════════════════════════════╝
```

**Checklist Visuelle:**
- [ ] Les temps sont formatés correctement (MM:SS.mmm)
- [ ] Les couleurs sont visibles et distinctes
- [ ] Le texte "CP1:", "CP2:", etc. est affiché
- [ ] Les checkpoints non passés affichent "--:--.---"

---

## 🎮 Tests en Conditions Réelles

### Scénario de Test Complet

**Durée estimée:** 10-15 minutes

1. **Préparation:**
   - Ouvrir Unity
   - Charger un circuit
   - Vérifier que tout est bien configuré

2. **Course 1 - Découverte:**
   - Démarrer la course
   - Faire un tour tranquillement
   - Observer les couleurs (probablement rouge/bleu)
   - Terminer la course
   - Noter le temps final

3. **Course 2 - Amélioration:**
   - Redémarrer
   - Essayer d'améliorer le temps
   - Observer les couleurs changer
   - Viser le vert sur quelques checkpoints
   - Terminer et comparer avec course 1

4. **Course 3 - Record:**
   - Redémarrer
   - Pousser au maximum
   - Essayer d'obtenir du vert partout
   - Battre le record si possible
   - Vérifier l'enregistrement du nouveau highscore

5. **Vérification Finale:**
   - Vérifier le classement des highscores
   - Recharger le circuit
   - Vérifier que les nouveaux temps sont chargés
   - Faire un dernier tour pour confirmer

---

## ✅ Critères de Succès

La fonctionnalité est considérée comme validée si:

### Fonctionnalités Principales
- [x] Timer démarre au CP0 (pas avant)
- [x] Premier tour comptabilisé
- [x] Tous les tours comptabilisés
- [x] Checkpoint times enregistrés

### Système de Couleurs
- [x] VERT pour temps meilleur que rank 1
- [x] BLEU pour temps dans la moyenne
- [x] ROUGE pour temps au-delà de la moyenne
- [x] Changement en temps réel

### Intégration
- [x] Chargement automatique au démarrage
- [x] Rechargement au changement de circuit
- [x] Sauvegarde dans HighscoreManager
- [x] Aucune erreur dans la console

### Robustesse
- [x] Pas de NullReferenceException
- [x] Fonctionne sans highscores existants
- [x] Gère les checkpoints manquants
- [x] Fonctionne avec plusieurs véhicules

---

## 🚨 Problèmes Connus / Limitations

### Limitations Actuelles

1. **Pas de Ghost Replay:**
   - Le système ne montre pas de véhicule fantôme du rank 1
   - Fonctionnalité future possible

2. **Pas de Delta Affiché:**
   - Ne montre pas "+0.5s" ou "-0.3s" de différence
   - Seulement les couleurs pour l'instant

3. **Couleurs Fixes:**
   - Les couleurs sont configurées dans l'Inspector
   - Pas de personnalisation en jeu

### Workarounds Connus

**Si les couleurs ne s'affichent pas:**
- Vérifier que les highscores ont des checkpoint times
- Recharger le circuit
- Vérifier la configuration dans l'Inspector

**Si le timer ne démarre pas:**
- Vérifier que vous passez bien le CP0
- Vérifier que CP0 est marqué IsStartFinishLine = true
- Regarder les logs pour débugger

---

## 📝 Rapport de Test Template

Utilisez ce template pour documenter vos tests:

```
=== RAPPORT DE TEST ===
Date: __/__/____
Testeur: ________
Version: ________

CONFIGURATION:
- Unity Version: _______
- Circuit Testé: _______
- Nombre de véhicules: _______

RÉSULTATS:

[ ] Test 1: Timer au CP0          ✅ / ❌
    Notes: ________________

[ ] Test 2: Premier tour          ✅ / ❌
    Notes: ________________

[ ] Test 3: Couleurs              ✅ / ❌
    Notes: ________________

[ ] Test 4: Changement circuit    ✅ / ❌
    Notes: ________________

[ ] Test 5: Sauvegarde            ✅ / ❌
    Notes: ________________

BUGS TROUVÉS:
1. ________________
2. ________________

SUGGESTIONS:
1. ________________
2. ________________

CONCLUSION:
✅ Prêt pour production
⚠️ Bugs mineurs à corriger
❌ Bugs majeurs, refaire les tests
```

---

## 🎓 Formation Utilisateur

### Pour les Joueurs

**Comment interpréter les couleurs:**

- 🟢 **VERT:** "Excellent! Tu bats le record actuel sur ce checkpoint!"
- 🔵 **BLEU:** "Bon temps, tu es dans le top 10!"
- 🔴 **ROUGE:** "Tu peux faire mieux, accélère!"

**Conseils:**
- Essayez d'obtenir du vert sur tous les checkpoints
- Si vous voyez beaucoup de rouge, travaillez ces sections
- Le bleu est déjà une bonne performance

---

## 📚 Ressources Additionnelles

**Fichiers de Documentation:**
- `HIGHSCORE_ENHANCEMENTS_SUMMARY.md` - Vue d'ensemble complète
- Ce fichier - Guide de test détaillé
- Console logs - Débogage en temps réel

**Context Menus Utiles:**
- RightClick sur `HighscoreManager` > "Debug: Display All Highscores"
- RightClick sur `CheckpointManager` > "Generate Checkpoints from CircuitData"

---

**Bonne chance avec les tests! 🚀**
