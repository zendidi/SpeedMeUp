# 2. Architecture du code

Tout le code gameplay vit dans `Assets/Project/Scripts/`, sous le namespace `ArcadeRacer.{Core, RaceSystem, Managers, Settings, Vehicle, Physics, UI, Utilities, Editor}`.

Patterns récurrents dans tout le projet :
- **Singletons paresseux** (`static Instance`, `FindFirstObjectByType` de secours, auto-création du GameObject si absent, `DontDestroyOnLoad`) : `HighscoreManager`, `FirebaseAuthManager`, `CircuitManager`, `AudioManager`, `MusicManager`, `CircuitDatabase` (variante basée sur `Resources`).
- **ScriptableObjects pour la donnée** : `CircuitData`, `VehicleStats`, `CircuitDatabase` — séparation nette entre données réglables par un game designer et comportement.
- **Événements C# / UnityEvents** pour découpler les systèmes : `CircuitManager.OnCircuitLoaded/OnLoadError`, `RaceManager.OnCountdownStarted/OnRaceStarted/OnRaceFinished`, `HighscoreManager.OnNetworkHighscoresLoaded` (statique), `Checkpoint.OnVehiclePassed` (UnityEvent).
- **Composition plutôt qu'héritage** pour le véhicule (Controller/Input/Physics/CorneringPhysics/OffroadDetector en composants séparés, reliés via `RequireComponent`).
- `FindFirstObjectByType<T>()` est utilisé abondamment à la place d'une injection de dépendances — acceptable à cette échelle, mais un point de vigilance si le projet grandit.
- Commentaires et logs `Debug.Log` en français dans une bonne partie du code.

## Véhicule / physique

Physique arcade maison basée sur `Rigidbody` (pas de `WheelCollider` Unity), répartie en composants coopérants sur le GameObject du véhicule :

| Script | Rôle |
|---|---|
| `Vehicules/VehicleController.cs` | Orchestrateur : relie `VehicleInput` → `VehiclePhysics`, gère spawn/reset (auto-reset si chute), activation/désactivation du contrôle (utilisé par `RaceManager` pendant le décompte) |
| `Vehicules/VehicleInput.cs` | Lit le nouvel Input System (`Car_Actions`) : accélération, frein, direction, drift, reset, pause |
| `Vehicules/VehiclePhysics.cs` (645 lignes) | Boucle `FixedUpdate` principale : détection du sol par raycasts, accélération via courbe de couple (`VehicleStats.torqueCurve`), direction, freinage adaptatif, drag/gravité, collisions murs/véhicules. Délègue à `VehiclePhysicsCore` et `VehicleCorneringPhysics` |
| `Vehicules/VehiclePhysicsCore.cs` | Classe C# simple (pas un MonoBehaviour) : inertie linéaire (roulement, drag), inertie angulaire (couple de direction, centrage, amortissement), transfert de charge |
| `Vehicules/VehicleCorneringPhysics.cs` (597 lignes) | Simulation dédiée survirage/sous-virage/tête-à-queue, retour visuel (teinte des roues). Voir [03-slip-system.md](03-slip-system.md) |
| `Vehicules/PhysicsFormula.cs` | Formules statiques (moment d'inertie, décélération, transfert de charge, angle de glissement) |
| `Vehicules/OffroadDetector.cs` | Détecte le nombre de roues hors piste, retourne des multiplicateurs de pénalité |
| `Settings/ScriptableObjects/VehicleStats.cs` | ScriptableObject de profil véhicule (vitesse max, courbe de couple, direction, drift, freinage, masse...). Instances : `BigCar`, `DefaultCarStats`, `FasterCar` |

## Checkpoints / chronométrage / course

Namespace `ArcadeRacer.RaceSystem`.

| Script | Rôle |
|---|---|
| `Track/Checkpoint.cs` | Trigger `BoxCollider`, génère son propre mesh visible, notifie `CheckpointManager`. Le checkpoint 0 sert aussi de ligne de départ/arrivée |
| `Track/CheckpointManager.cs` (532 lignes) | Liste ordonnée des checkpoints du circuit courant + progression par véhicule. Génération avec 4 priorités en cascade : (1) positions sauvegardées dans `CircuitData.checkpointData`, (2) auto-génération depuis le mesh via `CircuitMeshGenerator`, (3) depuis un `SplineContainer` brut, (4) liste manuelle. Détecte les passages dans le désordre (log un warning, ne bloque pas) |
| `Track/LapTimer.cs` | Chronomètre par véhicule (ajouté dynamiquement par `RaceManager`). Le timer démarre **au premier passage du CP0**, pas à la fin du décompte — découple "contrôlable" de "chronométré". Garde l'historique complet des temps de checkpoint par tour (`AllLapsCheckpointTimes`), utilisé pour attacher les splits à un highscore. Supporte un tour d'échauffement non compté (`ResetLapForWarmup`) |
| `Track/RaceManager.cs` (796 lignes) | Orchestrateur global : machine à états décompte → course → fin (`RaceState`), gère les véhicules en course, écoute `CheckpointManager` → `OnLapCompleted`, décide de la fin de course, coordonne le flux de saisie de nom pour highscore (accumule les tours qualifiants dans `_pendingHighscoreLaps`, ne demande le nom **qu'une fois en fin de course**), expose `EnableWarmupLap`/`TotalLaps` pilotables depuis l'UI |
| `Track/RacePositionTracker.cs` | Suivi de position plus précis (`lap × nbCheckpoints + prochainCheckpoint`), rafraîchi à fréquence fixe — pas encore branché à un affichage de classement en jeu |

## Circuits / génération procédurale / décor

Namespace `ArcadeRacer.Settings` / `ArcadeRacer.Managers` / `ArcadeRacer.Editor`.

| Script | Rôle |
|---|---|
| `Track/CircuitData.cs` | ScriptableObject central : points de spline (position + tangentes + rotation), largeur de piste, boucle fermée, spawn, checkpoints sauvegardés, matériaux route/murs, temps médailles or/argent/bronze, et le **décor** (`DecorObjectData[]` + palette de matériaux cyclique). Flag `isRaceable` avec validation dans `OnValidate()` |
| `Settings/CircuitDatabase.cs` | Singleton ScriptableObject (`Resources.Load` depuis `Assets/Resources/CircuitDatabase.asset`), liste maîtresse de tous les circuits, lookup par nom/index, filtre `GetRaceableCircuits()` |
| `Settings/CircuitGenerationConstants.cs` | Constantes de génération de mesh partagées entre l'éditeur et le runtime (segments par point de spline = 50, qualité de courbe = 20) — décision explicite pour garantir que l'aperçu éditeur == résultat en jeu |
| `Track/TrackBuilder/CircuitMeshGenerator.cs` (575 lignes, utilitaire statique) | Construit le mesh de route + murs depuis les splines, génère aussi les checkpoints auto-espacés à partir de la même interpolation |
| `Track/TrackBuilder/CircuitBuilder.cs` (1385 lignes, **outil éditeur seulement**, `[ExecuteInEditMode]`, utilisé dans `CircuitEditor.unity`) | Outil principal de level design : convertit un `SplineContainer` de scène en asset `CircuitData`, aperçu live, spawn point, checkpoints, décor manuel + procédural (`GenerateAutoDecor`, RNG à seed, snapping au sol, anti-chevauchement avec la route), workflow `ValidateAndMarkAsRaceable` |
| `Track/Editor/CircuitThumbnailGenerator.cs` | Génère les miniatures de `Assets/Circuits/Thumbnails/` |
| `Track/CircuitManager.cs` (469 lignes) | Singleton **runtime** (`DontDestroyOnLoad`) : point d'entrée unique pour charger/décharger un circuit en jeu — génère les meshes, construit un `SplineContainer` runtime (compatibilité `CheckpointManager`), crée le spawn, instancie le décor (racine séparée à l'origine du monde), émet `OnCircuitLoaded`/`OnCircuitUnloaded`/`OnLoadError` |
| `Track/CircuitLoader.cs` / `Track/RuntimeCircuitLoader.cs` | Deux chemins de chargement alternatifs — `CircuitLoader` semble antérieur/mort (voir [01-overview.md](01-overview.md)) ; `RuntimeCircuitLoader` est un outil de debug (overlay OnGUI, raccourcis clavier N/P pour changer de circuit) |

Circuits présents : `EyeOfGod, FirstTry, ForthTry, SecondTry, TestLapTracking, TheEgg, ThirdTry, mambo` (`Assets/Project/Settings/Circuits/*.asset`).

## Highscores & Firebase

Voir [04-highscores-firebase.md](04-highscores-firebase.md) — système le plus sophistiqué du projet (925 lignes dans `HighscoreManager.cs`).

## Réseau / multijoueur

**Non implémenté.** `com.unity.netcode.gameobjects` 2.8.0 est installé et `Assets/DefaultNetworkPrefabs.asset` existe, mais :
- `Scripts/Network/` et `Prefabs/Network/` sont des dossiers vides
- `DefaultNetworkPrefabs.asset` a une liste vide
- Aucune occurrence de `NetworkBehaviour`, `NetworkManager` ou `NetworkObject` dans le code

C'est un scaffolding posé mais jamais commencé — un bon prochain jalon si le multijoueur est souhaité.

## UI

Namespace `ArcadeRacer.UI`.

| Script | Rôle |
|---|---|
| `UI/UIManager.cs` (412 lignes) | Coordinateur UI global : références vers `RaceHUD`, `CountdownUI`, `FinishScreenUI`, `CircuitSelectionUI`, `HighscoreNameInputUI` ; branche les toggles/sliders (tour d'échauffement, nombre de tours) sur `RaceManager` ; s'abonne aux événements de `RaceManager` |
| `UI/RaceHUD.cs` | Vitesse, temps du tour actuel/meilleur, nombre de tours, position |
| `UI/CountdownUI.cs`, `UI/FinishScreenUI.cs` | Écran de décompte et écran de résultats |
| `UI/CircuitSelectionUI.cs` (378 lignes) | Grille de sélection de circuit depuis `CircuitDatabase` (filtrable `isRaceable`), affiche le meilleur temps du circuit courant |
| `UI/CheckpointTimingDisplay.cs` | Affiche le split du dernier checkpoint passé avec code couleur vs highscores — voir l'état final documenté dans [04-highscores-firebase.md](04-highscores-firebase.md#affichage-des-splits-en-temps-réel) |

## Audio & VFX (systèmes de support)

- **Audio** (`Scripts/Audio/`) : `AudioManager_Version2`, `MusicManager_Version2`, `VehicleAudio_Version2` (son moteur lié à la vitesse), `AudioPlaceholder_Version2`.
- **VFX** (`Scripts/VFX/`) : `VehicleVFX.cs` fait office de façade pour `DriftParticles`, `SkidMarks`, `SpeedLines`, `WheelAnimator`, `CollisionVFX`.
- **Caméra** : `Camera/CinemachineDynamicFOV_Version3.cs` — FOV dynamique selon la vitesse.

## Input

- `Assets/Project/Settings/Car_Actions.inputactions` (+ `Car_Actions.cs` généré) est l'action map réellement utilisée : map `Driving` avec `Accelerate`, `Brake`, `Steering`, `Drift`, `MenuTrigger`.
- `VehicleInput.cs` et `UIManager.cs` instancient chacun leur propre objet `Car_Actions` indépendamment.
