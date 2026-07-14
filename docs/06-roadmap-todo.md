# 6. Roadmap / pistes d'amélioration

Compilé à partir des sections "améliorations futures" des anciens fichiers de session et des points de vigilance relevés lors de l'exploration du code (juillet 2026). Rien ici n'est engagé — ce sont des pistes, pas des engagements.

## Gameplay / UX highscores

- **Ghost replay** : véhicule fantôme suivant le temps du rank 1 sur le circuit.
- **Delta affiché** : montrer "+0.5s" / "-0.3s" par rapport au rank 1 à côté du split actuel, plutôt que juste une couleur.
- **Indicateur de tendance** : flèche ↑/↓ entre deux checkpoints consécutifs.
- **Prédiction du temps final** : estimation basée sur les splits déjà passés.
- **Feedback audio** : son différent selon la couleur du split (encouragement/avertissement).
- **Filtre anti-injures sur les noms** + vérification d'unicité (actuellement aucune validation de contenu côté nom de joueur, seulement la longueur 1-20 caractères imposée par `database.rules.json`).

## Nettoyage technique (dette identifiée pendant l'exploration)

- Confirmer si `Assets/Project/Scripts/Track/CircuitLoader.cs` est bien mort (superseded par `CircuitManager.cs`) et le supprimer si oui.
- Vérifier l'utilité des scripts `_Version2`/`_Version3` (`AudioManager_Version2`, `MusicManager_Version2`, `VehicleAudio_Version2`, `CinemachineDynamicFOV_Version3`) — confirmer qu'aucune version antérieure résiduelle ne traîne ailleurs, et envisager de retirer le suffixe une fois stabilisé.
- Supprimer `Assets/InputSystem_Actions.inputactions` (asset par défaut du template Unity, non utilisé — le vrai est `Car_Actions.inputactions`).
- `RacePositionTracker.cs` calcule une position de course précise mais n'est branché à aucun affichage de classement en jeu — soit le brancher à l'UI, soit le retirer s'il n'est plus nécessaire.
- `Assets/google-services.json` est inutilisé (le projet évite le SDK Firebase) — à supprimer ou documenter pourquoi il reste.

## Multijoueur

`com.unity.netcode.gameobjects` est installé et `DefaultNetworkPrefabs.asset` existe, mais aucun code réseau n'a été écrit (`Scripts/Network/` vide). C'est un vrai jalon à part entière si le multijoueur est souhaité un jour, pas une fonctionnalité cassée à réparer.

## Documentation

- Ce dossier `/docs` remplace le besoin de nouveaux fichiers `.md` épars à la racine — pour toute nouvelle session de travail, ajouter/mettre à jour le fichier système concerné ici plutôt que de créer un nouveau résumé de session.
- `FIREBASE_SETUP_GUIDE.md` et `SLIP_SYSTEM_MANUAL.md` restent la référence détaillée pour leurs sujets respectifs et sont volontairement laissés à la racine (liés depuis `docs/`).
