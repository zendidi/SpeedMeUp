# 5. Historique des décisions et bugs corrigés

Résumé chronologique condensé des ~15 fichiers `.md` de session laissés à la racine du dépôt (février–mars 2026). Objectif : garder la mémoire du *pourquoi* sans garder 15 fichiers séparés à parcourir. Les fichiers sources restent sur le disque (voir [README.md](README.md#anciens-fichiers-de-session-racine-du-dépôt)) si le détail pas-à-pas est nécessaire.

## Système de circuits (12 février 2026)

Mise en place initiale : `CircuitDatabase` (source unique de vérité des circuits), `HighscoreManager` (classement local, format `MM:SS:mmm`), `CircuitThumbnailGenerator` (miniatures auto), `CircuitSelectionUI` (grille de sélection). Voir `IMPLEMENTATION_COMPLETE.md`.

## Système de highscore — implémentation initiale (17 février 2026)

- Modal `HighscoreNameInputUI` pour saisir un nom de joueur en top 10, déclenché **par tour**, indépendant du nombre de tours de la course.
- `LapTimer.cs` : séparation `StartRace()` (préparation) / `StartTimer()` (démarrage réel au passage du CP0) — décision prise pour que le chrono ne démarre pas avant que le joueur ait effectivement quitté la ligne de départ.
- **Bug** : le tour n'était jamais comptabilisé car `CheckpointManager.cs` réinitialisait le timer (`Reset()` + `StartRace()`) juste avant que `CompleteLap()` lise le temps → tour toujours enregistré à `00:00.000`. Corrigé en supprimant cette réinitialisation erronée (le timer ne doit être démarré qu'une fois, par `RaceManager`, jamais par `CheckpointManager`).

## Bug du premier tour non sauvegardé (18-19 février 2026)

Deux causes distinctes ont été trouvées et corrigées à des moments différents — vaut la peine de les distinguer si le problème resurgit :

1. **Race condition sur les checkpoint times** : le tour suivant démarrait pendant que le joueur saisissait son nom, effaçant les temps de checkpoint avant que `SaveLapTimeToHighscores()` ne les lise. Fix : les checkpoint times sont capturés immédiatement dans `CheckAndPromptForHighscore()` (champ `_pendingCheckpointTimes`), pas au moment de la confirmation du nom.
2. **`Awake()` vs `Start()`** : `HighscoreNameInputUI.Start()` appelait `Hide()`. Or `Start()` peut être rappelé si le GameObject est réactivé pendant la course (ce qui arrivait), fermant le modal du 1er tour avant que le joueur ait pu taper son nom. Fix : déplacer `Hide()` dans `Awake()` (appelé une seule fois à la création).

**Leçon générale** : dans Unity, ne jamais mettre de logique d'état "one-shot" dans `Start()` si le GameObject peut être réactivé en cours de partie — `Awake()` est le bon endroit.

## Comparaison de performance des checkpoints — 3 itérations successives

Le calcul "quelle couleur afficher pour ce split de checkpoint" a été revu 3 fois avant de se stabiliser :

1. **v1** : Vert = meilleur que rank 1, Bleu = ≤ moyenne des ranks 2-10, Rouge = > moyenne.
2. **v2** : correction du calcul de moyenne pour inclure les ranks 1-10 (pas seulement 2-10).
3. **v3 (finale, conservée)** : simplifiée à seulement 2 points de référence — Bleu = meilleur que rank 1, Vert = entre rank 1 et rank 10, Rouge = pire que rank 10. Plus de calcul de moyenne du tout (`GetAverageCheckpointTimes()` supprimée, remplacée par `GetWorstTime()`).

Voir l'état final documenté dans [04-highscores-firebase.md](04-highscores-firebase.md#affichage-des-splits-en-temps-réel).

## Refactor de l'affichage des checkpoints (19 février 2026)

`CheckpointTimingDisplay` est passé d'un tableau de `TextMeshProUGUI[]` mis à jour par polling (`Update()` toutes les 0.1s) à un seul champ `TextMeshProUGUI` mis à jour par événement (`OnCheckpointRecorded`, déclenché par `LapTimer.RecordCheckpoint()`). Gain : ~99% de réduction des appels UI, affichage plus cohérent avec la logique de timing.

## Synchronisation Firebase — 4 itérations vers l'état stable

Voir le détail technique dans [04-highscores-firebase.md](04-highscores-firebase.md), résumé ici pour la chronologie :

1. `3bbd687` — cache réseau comme source de vérité au lieu de `PlayerPrefs`.
2. `2c90b1d` — token d'auth manquant sur les lectures + attente de l'authentification avant sync.
3. `b06e27b` — Firebase écrase le local au lieu de fusionner (évite la résurgence de données périmées).
4. `a2ed0a1` (le plus récent) — 4 bugs spécifiques aux **builds** (timeout d'auth trop court, pas de retry sur 401/403, corruption de Firebase par des scores en attente, pas de refresh à l'ouverture de l'écran).

**Pattern récurrent à retenir** : la plupart de ces bugs n'apparaissaient qu'en build, jamais dans l'éditeur, parce que l'authentification Firebase y est quasi instantanée. Toujours tester la sync Firebase sur un vrai build, pas seulement en Play Mode.
