# 1. Aperçu du projet

## Concept

**SpeedMeUp** (nom de code interne "Vroum Vroum") est un jeu de course arcade solo/local : circuits générés depuis des splines, physique de véhicule arcade (survirage/sous-virage volontairement exagérable), chronométrage par checkpoints, et classement des meilleurs temps par circuit partagé en ligne via Firebase.

## Stack technique

| Élément | Valeur |
|---|---|
| Moteur | Unity **6000.3.8f1** (Unity 6) |
| Render pipeline | URP 17.3.0 (variantes PC + Mobile présentes dans `Assets/Settings`) |
| Input | Nouveau Input System (1.18.0), action map `Car_Actions` |
| Caméra | Cinemachine 3.1.5 (FOV dynamique selon vitesse) |
| Circuits | `UnityEngine.Splines` — génération procédurale de mesh + checkpoints |
| Classement en ligne | Firebase Realtime Database, **via API REST brute** (`UnityWebRequest`), pas de SDK Firebase |
| Réseau multijoueur | `com.unity.netcode.gameobjects` 2.8.0 installé mais **non implémenté** (voir [02-architecture.md](02-architecture.md#réseau--multijoueur)) |

Pas de fichier `README.md` à la racine avant cette documentation — `Assets/Readme.asset` est le readme générique du template Unity URP, sans contenu spécifique au projet.

## Où trouver quoi

```
Assets/
├── Project/                   ← DOSSIER PRINCIPAL (tout le code et contenu du jeu)
│   ├── Scripts/                 code C# (voir 02-architecture.md)
│   ├── Scene/Core/               SampleScene.unity (jeu), CircuitEditor.unity (éditeur de circuits)
│   ├── Settings/Circuits/        CircuitData ScriptableObjects (8 circuits)
│   ├── Settings/ScriptableObjects/ VehicleStats (profils de véhicule : BigCar, DefaultCarStats, FasterCar)
│   ├── Audio/, Visual/           assets audio/visuels
│   └── Prefabs/
├── _Project/                  ← quasi vide (juste Textures/), ne pas utiliser malgré le nom
├── Circuits/Thumbnails/        miniatures PNG des circuits
├── Resources/CircuitDatabase.asset   base de données de tous les circuits (singleton chargé via Resources.Load)
├── _Recovery/                  backups de scène auto-générés par Unity après un crash (pas du contenu réel)
└── google-services.json        présent mais inutilisé (le projet évite volontairement le SDK Firebase)
```

Racine du dépôt : `database.rules.json` + `firebase.json` (config Firebase CLI) et les anciens `.md` de session (voir [05-historique-decisions.md](05-historique-decisions.md)).

## Points d'attention identifiés (à clarifier/nettoyer un jour)

- `Assets/Project/Scripts/Track/CircuitLoader.cs` semble être un chemin de chargement de circuit antérieur/parallèle à `CircuitManager.cs` (son `Start()` a son appel à `LoadCircuit()` commenté) — probablement du code mort à confirmer et supprimer.
- Plusieurs scripts portent un suffixe `_Version2`/`_Version3` (`AudioManager_Version2.cs`, `VehicleAudio_Version2.cs`, `CinemachineDynamicFOV_Version3.cs`...), signe d'itérations précédentes — vérifier qu'aucune `_Version1` résiduelle ne traîne.
- `Assets/InputSystem_Actions.inputactions` (racine d'Assets) est l'asset par défaut du template Unity, distinct du vrai `Car_Actions.inputactions` — probablement à supprimer.
- Le dossier réseau (`Scripts/Network/`, `Prefabs/Network/`) est vide malgré le package Netcode installé : scaffolding posé mais jamais commencé.
