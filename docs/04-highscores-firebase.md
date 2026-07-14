# 4. Highscores & synchronisation Firebase

Système le plus retravaillé du projet — ce document décrit l'**état actuel** (après le commit `a2ed0a1`, le plus récent sur ce sujet). Pour le guide de setup Firebase pas-à-pas (créer le projet, activer Realtime Database, récupérer les clés), voir [`FIREBASE_SETUP_GUIDE.md`](../FIREBASE_SETUP_GUIDE.md) à la racine, qui reste à jour. Pour l'historique des bugs traversés, voir [05-historique-decisions.md](05-historique-decisions.md).

## Composants

| Fichier | Rôle |
|---|---|
| `Assets/Project/Scripts/Core/HighscoreManager.cs` (925 lignes) | Singleton. Persistance locale `PlayerPrefs` + cache réseau + sync Firebase |
| `Assets/Project/Scripts/Core/FirebaseAuthManager.cs` | Authentification anonyme Firebase **via API REST pure** (pas de SDK) |
| `Assets/Project/Scripts/UI/HighscoreDisplayUI.cs` | Affichage classement, dropdown circuit |
| `Assets/Project/Scripts/UI/HighscoreNameInputUI.cs` | Modal de saisie du nom joueur |
| `database.rules.json` (racine) | Règles Firebase : lecture publique, écriture authentifiée, validation (`timeInSeconds > 5.0`, nom 1-20 caractères) |
| `firebase.json` (racine) | Config Firebase CLI (`firebase deploy --only database`) |

**Important** : `Assets/google-services.json` est présent mais **inutilisé** — le projet évite délibérément le SDK Firebase au profit d'appels `UnityWebRequest` bruts, pour rester léger.

## Fonctionnement local (offline)

Si `firebaseDatabaseUrl` est vide sur le `HighscoreManager`, tout fonctionne uniquement en `PlayerPrefs`, sans aucun appel réseau — c'est l'état par défaut tant que Firebase n'est pas configuré. Format de stockage : `"MM:SS:mmm|PlayerName|cp1,cp2,...|dd/MM/yyyy|HH:mm:ss"`, top 10 par circuit.

## Architecture réseau (état final)

```
RaceManager.OnLapCompleted()
   └─ CheckAndPromptForHighscore() : WouldBeTopScore ?
        └─ tours qualifiants accumulés dans _pendingHighscoreLaps
           (le nom n'est demandé qu'UNE FOIS, en fin de course)
             └─ HighscoreNameInputUI (modal) → SaveAllPendingHighscores(nom)
                  └─ HighscoreManager.TryAddScore()
                       ├─ sauvegarde locale PlayerPrefs (immédiate, synchrone)
                       └─ PushToNetwork() (coroutine, asynchrone, best-effort)
```

Firebase devient **source de vérité** dès qu'un circuit a été synchronisé au moins une fois : `GetHighscores()` retourne alors le cache réseau plutôt que `PlayerPrefs`. Si Firebase échoue (pas d'internet), le jeu continue normalement sur les données locales — juste un warning en console.

## Les 4 bugs corrigés dans le dernier commit (`a2ed0a1`) — pourquoi ils comptent

Ces fixes ciblaient spécifiquement des échecs de sync **en build**, invisibles dans l'éditeur (où l'auth Firebase est quasi instantanée). Comprendre ces 4 points évite de les réintroduire :

1. **Timeout d'auth trop court.** `FirebaseAuthManager.InitializeAuth()` peut prendre jusqu'à 20s (10s refresh token + 10s sign-in anonyme). L'ancien `SyncAllFromNetwork` n'attendait que `networkTimeoutSeconds` (10s par défaut) → requêtes GET envoyées sans auth → 401 → `_syncedFromNetwork` jamais rempli → `GetHighscores()` retombait pour toujours sur du `PlayerPrefs` périmé en build.
   → Fix : nouvelle constante `AUTH_WAIT_TIMEOUT_SECONDS = 30f`, indépendante de `networkTimeoutSeconds`.
2. **Pas de retry sur 401/403.** Une réponse 401/403 abandonnait silencieusement la sync du circuit, même si l'auth se terminait juste après.
   → Fix : `SyncFromNetwork(allowRetry=true)` attend l'auth et retente une fois (`allowRetry=false` sur le retry, pour éviter une boucle infinie).
3. **`TryAddScore` corrompait Firebase avec des données `PlayerPrefs` périmées.** Si un score était ajouté avant la fin de la sync Firebase (course terminée dans les 30 premières secondes), `GetHighscores()` renvoyait encore le vieux `PlayerPrefs` d'un build précédent. Le nouveau score fusionné avec ces données périmées était alors poussé (PUT) sur Firebase, rendant les vieux scores permanents.
   → Fix : file d'attente de scores en attente (`_pendingScores`). `TryAddScore` met en file si le circuit n'est pas encore synchronisé ; `SyncThenProcessPending()` synchronise d'abord Firebase, puis `ProcessPendingScores()` re-soumet les scores en attente contre des données Firebase propres et faisant autorité.
4. **L'écran de highscores ne déclenchait pas de sync fraîche à l'ouverture.**
   → Fix : `HighscoreDisplayUI.OnEnable()` appelle `RefreshFromNetwork()` — chaque ouverture d'écran récupère les données Firebase à jour.

## Historique des fixes réseau précédents (contexte, plus anciens)

- `3bbd687` — `GetHighscores()` utilise le cache réseau en mémoire comme source de vérité au lieu de `PlayerPrefs`, pour éviter qu'un ancien build ne fasse ressurgir des données périmées.
- `2c90b1d` — Les requêtes GET n'envoyaient pas le token d'auth (`withAuth: false`), causant des 401 alors que les règles exigent `auth != null` en lecture ; `SyncAllFromNetwork` attend désormais l'authentification avant de lire.
- `b06e27b` — `MergeIntoLocal()` fusionnait avec `PlayerPrefs`, préservant des entrées périmées d'anciens builds. Remplacé par `OverwriteLocalFromNetwork()` : Firebase écrase toujours le local, y compris sur 0 score ou 404.

## Affichage des splits en temps réel

`CheckpointTimingDisplay.cs` affiche le split du **dernier checkpoint passé uniquement** (un seul champ `TextMeshProUGUI`, pas un tableau), mis à jour de façon événementielle par `LapTimer.RecordCheckpoint()` — pas de polling. C'est la version finale après plusieurs itérations (voir [05-historique-decisions.md](05-historique-decisions.md)).

**Logique de couleur actuelle** (la plus simple, celle qui a été gardée) :

```csharp
if (checkpointTime < rank1Time)      return BLEU;   // meilleur que le record actuel
else if (checkpointTime <= rank10Time) return VERT;  // dans le top 10
else                                   return ROUGE;  // hors top 10
```

Seulement 2 points de référence (rank 1 et rank 10 du circuit) — les versions précédentes avec calcul de moyenne (ranks 2-10, puis ranks 1-10) ont été abandonnées pour cette version plus simple.

## Debug utile

- Context menu sur `HighscoreManager` → "Debug: Display All Highscores"
- Logs préfixés `[HighscoreManager]`, `[FirebaseAuthManager]`, `[RaceManager]`, `[CheckpointTimingDisplay]`
