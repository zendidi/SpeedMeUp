# 🔥 Guide Complet : Firebase Realtime Database + Unity (SpeedMeUp)

## Vue d'Ensemble

Ce guide explique comment connecter le système de highscores de **SpeedMeUp** à **Firebase Realtime Database** pour stocker et partager les meilleurs temps entre tous les joueurs, gratuitement.

### Pourquoi Firebase ?

| Critère | Solution choisie |
|---|---|
| 💰 Gratuit | Plan Spark : 1 Go de données, 10 Go/mois de transfert, illimité en lecture/écriture |
| 🔧 Pas de SDK requis | On utilise l'API REST Firebase directement avec `UnityWebRequest` (déjà dans Unity) |
| 🚀 Pas de serveur à maintenir | Firebase gère tout l'infrastructure |
| ⚡ Temps réel | Les nouveaux scores apparaissent sans relancer le jeu |
| 🌍 Mondial | Un seul classement partagé entre tous les joueurs |

### Architecture du Système

```
┌─────────────────────────────────────────────────────────┐
│                    JOUEUR (Unity)                         │
│                                                           │
│  ┌─────────────────┐     ┌────────────────────────────┐  │
│  │  RaceManager    │────▶│    HighscoreManager        │  │
│  │  (détecte top10)│     │                            │  │
│  └─────────────────┘     │  1. Sauvegarde locale      │  │
│                           │     (PlayerPrefs)          │  │
│  ┌─────────────────┐     │  2. Push Firebase (PUT)    │  │
│  │ HighscoreDisplay│◀────│  3. Pull Firebase (GET)    │  │
│  │ UI (affichage)  │     │  4. Fusion des scores      │  │
│  └─────────────────┘     └────────────┬───────────────┘  │
└───────────────────────────────────────│─────────────────┘
                                        │ HTTPS / REST
                        ┌───────────────▼───────────────┐
                        │  Firebase Realtime Database    │
                        │                               │
                        │  highscores/                  │
                        │  ├── Circuit_1/               │
                        │  │   └── entries: [...]       │
                        │  └── Circuit_2/               │
                        │      └── entries: [...]       │
                        └───────────────────────────────┘
```

### Flux de données

```
Joueur finit un tour
        │
        ▼
RaceManager.CheckAndPromptForHighscore()
        │ temps qualifie le top 10 ?
        ▼
Modal de saisie du nom (HighscoreNameInputUI)
        │ joueur confirme
        ▼
HighscoreManager.TryAddScore()
        │
        ├──▶ Sauvegarde locale (PlayerPrefs) — immédiate
        │
        └──▶ PushToNetwork() coroutine — asynchrone
                    │
                    ▼
             PUT https://{url}/highscores/{circuit}.json
```

---

## 📋 Prérequis

- Compte Google (gratuit)
- Unity 2021.3 ou plus récent
- Le projet SpeedMeUp avec le `HighscoreManager` mis à jour

---

## 🔥 PARTIE 1 : Configuration de Firebase

### Étape 1 : Créer un projet Firebase

1. Aller sur **[https://console.firebase.google.com](https://console.firebase.google.com)**
2. Cliquer sur **"Ajouter un projet"** (ou "Create a project")
3. Nommer le projet : ex. `speedmeup-highscores`
4. **Désactiver Google Analytics** (pas nécessaire pour ce projet)
5. Cliquer **"Créer le projet"**
6. Attendre la création (~30 secondes) puis cliquer **"Continuer"**

### Étape 2 : Activer Realtime Database

1. Dans le menu gauche, cliquer sur **"Build" → "Realtime Database"**
2. Cliquer sur **"Créer une base de données"**
3. Choisir la région : **"Europe-West1 (Belgique)"** (plus proche de la France, latence réduite)
4. Sur l'écran des règles de sécurité, choisir **"Commencer en mode verrouillé"**

   > ℹ️ Le mode verrouillé refuse tout par défaut. On appliquera immédiatement les règles permanentes à l'étape suivante.

5. Cliquer **"Activer"**

### Étape 3 : Récupérer l'URL de la Database

Après activation, vous verrez l'écran principal de la Realtime Database. L'URL est visible en haut de la page :

```
https://speedmeup-highscores-default-rtdb.europe-west1.firebasedatabase.app
```

> 📌 **Copiez cette URL** — vous en aurez besoin dans Unity.

L'URL ressemble toujours à ce format :
```
https://NOM_PROJET-default-rtdb.REGION.firebasedatabase.app
```

Ou pour les projets US :
```
https://NOM_PROJET-default-rtdb.firebaseio.com
```

### Étape 4 : Appliquer les Règles de Sécurité Permanentes

Les règles de sécurité du projet sont stockées dans le fichier `database.rules.json` à la racine du dépôt. C'est la **source de vérité unique** — modifier ce fichier, puis déployer.

#### Option A — Via Firebase CLI (recommandé, reproductible)

```bash
# 1. Installer Firebase CLI (une seule fois)
npm install -g firebase-tools

# 2. Se connecter avec votre compte Google
firebase login

# 3. Associer le projet Firebase (une seule fois, à la racine du dépôt)
firebase use --add
# Choisir votre projet dans la liste, lui donner un alias : "default"

# 4. Déployer les règles
firebase deploy --only database
```

Sortie attendue :
```
✔  Deploy complete!

Project Console: https://console.firebase.google.com/project/speedmeup-highscores/overview
```

#### Option B — Via la Console Firebase (copier-coller)

Dans la console Firebase → **Realtime Database → Règles**, remplacer tout le contenu par le contenu du fichier `database.rules.json` puis cliquer **"Publier"**.

**Explication des règles :**
- `".read": true` → Tout le monde peut lire les highscores (classement public)
- `".write": true` → Tout le monde peut écrire (sans authentification)
- `".validate"` → La donnée doit avoir un champ `entries`
- `timeInSeconds > 5.0` → Refuse tout temps inférieur à 5 secondes (anti-triche basique)
- `playerName.length 1–20` → Nom de joueur obligatoire, 20 caractères max
- Seule la branche `highscores/` est accessible — le reste de la DB est protégé par défaut

> ✅ Ces règles sont **permanentes** — elles n'expirent jamais, contrairement au mode test.

---

## 🎮 PARTIE 2 : Configuration dans Unity

### Étape 1 : Trouver le HighscoreManager dans la Scène

1. Ouvrir la scène principale : `Assets/Project/Scene/Core/SampleScene.unity`
2. Dans la **Hiérarchie**, chercher l'objet **"HighscoreManager"**
   - S'il n'est pas visible, lancer le jeu une fois — il se crée automatiquement
   - Ou créer manuellement : `Create Empty GameObject` → nommer `HighscoreManager` → ajouter le component `HighscoreManager`

### Étape 2 : Configurer l'Inspector

Sélectionner le GameObject **HighscoreManager** dans la hiérarchie. Dans l'Inspector, vous verrez la section :

```
=== RÉSEAU (Firebase Realtime Database) ===

Firebase Database Url : [ _______________________________ ]
Auto Sync On Start    : [✓]
Network Timeout Secs  : [ 10 ]
```

**Remplir le champ "Firebase Database Url"** avec l'URL copiée à l'étape 3 :

```
https://speedmeup-highscores-default-rtdb.europe-west1.firebasedatabase.app
```

> ⚠️ **Ne pas** ajouter de `/` à la fin de l'URL.

**Options disponibles :**

| Champ | Valeur par défaut | Description |
|-------|-------------------|-------------|
| Firebase Database Url | *(vide)* | URL de votre DB Firebase. Vide = mode local uniquement |
| Auto Sync On Start | `true` | Télécharge les scores Firebase au démarrage du jeu |
| Network Timeout Secs | `10` | Secondes avant d'abandonner une requête réseau |

### Étape 3 : Sauvegarder la Scène

- `Ctrl+S` (Windows) ou `Cmd+S` (Mac) pour sauvegarder la scène
- Le champ `firebaseDatabaseUrl` est sérialisé dans la scène — il sera inclus dans le build

---

## 📡 PARTIE 3 : Comment ça Fonctionne dans le Code

### Structure des Données dans Firebase

Les scores sont stockés au format JSON sous ce chemin :

```
{databaseUrl}/highscores/{nomDuCircuit}.json
```

Exemple pour le circuit **"Circuit_Rapide"** :

```
GET https://speedmeup-highscores-default-rtdb.europe-west1.firebasedatabase.app/highscores/Circuit_Rapide.json
```

Réponse JSON :

```json
{
  "entries": [
    {
      "timeInSeconds": 62.123,
      "playerName": "Alice",
      "rank": 1,
      "dateString": "04/03/2026",
      "timeOfDayString": "14:32:10"
    },
    {
      "timeInSeconds": 65.432,
      "playerName": "Bob",
      "rank": 2,
      "dateString": "03/03/2026",
      "timeOfDayString": "09:15:44"
    }
  ]
}
```

> 📌 **Note** : Les temps de checkpoints (`checkpointTimes`) ne sont pas synchronisés en réseau. Seuls les champs `timeInSeconds`, `playerName`, `rank`, `dateString` et `timeOfDayString` sont partagés.

### Transformation du Nom de Circuit

Les caractères spéciaux du nom de circuit sont nettoyés pour former une clé Firebase valide :

| Caractère | Remplacé par |
|-----------|-------------|
| Espace ` ` | `_` |
| Tiret `-` | `_` |
| Slash `/` | `_` |
| Point `.` | `_` |

Exemple :
- `"Circuit Rapide"` → `/highscores/Circuit_Rapide.json`
- `"Track-01"` → `/highscores/Track_01.json`

### Cycle de Vie Réseau au Démarrage

```
Awake()
   │
   ├─ autoSyncOnStart = true et URL configurée ?
   │         │
   │         ▼ OUI
   │   SyncAllFromNetwork() [coroutine]
   │         │
   │         ▼ Pour chaque circuit dans CircuitDatabase
   │   SyncFromNetwork("Circuit_X")
   │         │
   │         ▼ GET Firebase
   │   Réponse : succès ?
   │         │
   │    OUI ─┤─ NON : 404 → log "Aucun score" (normal)
   │         │         └─ autre erreur → log Warning
   │         ▼
   │   MergeIntoLocal() — fusionne avec PlayerPrefs
   │         │
   │         ▼
   │   OnNetworkHighscoresLoaded?.Invoke(circuitName)
   │         │
   │         ▼
   │   HighscoreDisplayUI.OnNetworkDataLoaded()
   │         │
   │         ▼
   │   RefreshDisplay() — met à jour l'affichage
```

### Cycle de Vie Réseau lors d'un Nouveau Score

```
TryAddScore(circuit, temps, joueur)
   │
   ├─ Trier et garder le top 10
   │
   ├─ Sauvegarder dans PlayerPrefs [immédiat, synchrone]
   │
   └─ IsNetworkEnabled ?
             │
             ▼ OUI
       PushToNetwork(circuit, scores) [coroutine, asynchrone]
             │
             ▼ PUT Firebase
       Succès → log "Scores poussés"
       Échec  → log Warning (les données locales sont conservées)
```

### Fusion des Scores (MergeIntoLocal)

Quand des scores arrivent de Firebase, la fusion :
1. Charge les scores locaux depuis PlayerPrefs
2. Pour chaque score réseau : l'ajoute seulement s'il n'existe pas déjà (doublon = même `playerName` + même `timeInSeconds` à ±0.001s)
3. Trie le tout par `timeInSeconds` croissant
4. Garde les 10 premiers
5. Ré-attribue les rangs (#1 à #10)
6. Sauvegarde dans PlayerPrefs **sans** re-pousser sur Firebase (évite la boucle infinie)

---

## 🧪 PARTIE 4 : Tester l'Intégration

### Test 1 : Vérifier la Connexion

1. Lancer le jeu dans l'éditeur Unity
2. Ouvrir la **Console Unity** (`Window → General → Console`)
3. Au démarrage, vous devriez voir :

```
[HighscoreManager] Synchronisation réseau OK pour 'Circuit_X': N scores.
```

Ou si c'est la première fois (aucun score encore sur Firebase) :

```
[HighscoreManager] Aucun score réseau pour 'Circuit_X' (404).
```

> ✅ Les deux messages indiquent que la connexion fonctionne.

### Test 2 : Vérifier l'Écriture sur Firebase

1. Faire un tour dans le jeu et entrer un nom (modal de saisie)
2. Dans la console Unity, vérifier :

```
🏆 [RaceManager] Nouveau highscore pour Circuit_X: 01:05.432 - Alice (Rang: 1)
[HighscoreManager] Scores poussés vers Firebase pour 'Circuit_X'.
```

3. Sur la console Firebase → **Realtime Database → Données**, vous devriez voir :

```
highscores
└── Circuit_X
    └── entries
        └── 0
            ├── timeInSeconds: 65.432
            ├── playerName: "Alice"
            ├── rank: 1
            ├── dateString: "04/03/2026"
            └── timeOfDayString: "14:32:10"
```

### Test 3 : Vérifier la Synchronisation Multi-Joueurs

1. **Joueur A** (machine 1) : Faire un bon temps, entrer le nom "Alice"
2. **Joueur B** (machine 2) : Lancer le jeu
3. Dans la console de Joueur B :

```
[HighscoreManager] Synchronisation réseau OK pour 'Circuit_X': 1 scores.
```

4. Dans l'écran des highscores de Joueur B : "Alice" doit apparaître dans le classement

### Test 4 : Test Hors Ligne

1. Couper la connexion internet
2. Lancer le jeu
3. Dans la console Unity :

```
[HighscoreManager] Échec synchronisation réseau pour 'Circuit_X': Cannot connect to destination host
```

4. ✅ Le jeu continue de fonctionner normalement avec les scores locaux

### Test 5 : Context Menu Debug

Dans l'éditeur Unity, sélectionner le GameObject **HighscoreManager** :
- Clic droit sur le composant → **"Debug: Display All Highscores"**
- Affiche tous les scores locaux (incluant ceux synchronisés depuis Firebase)

---

## 🔧 PARTIE 5 : Dépannage

### ❌ "Firebase Database Url" non trouvée dans l'Inspector

**Cause** : Vous regardez peut-être la mauvaise version du composant.

**Solution** :
- Vérifier que le fichier `HighscoreManager.cs` dans `Assets/Project/Scripts/Core/` est bien la version mise à jour (elle doit contenir `[Header("=== RÉSEAU (Firebase Realtime Database) ===")]`)
- Si l'Inspector ne montre pas les nouveaux champs : `Edit → Preferences → General → Script Changes While Playing` → **Recompiler**

### ❌ "Cannot connect to destination host"

**Causes possibles** :
1. Pas de connexion internet
2. URL Firebase incorrecte

**Solution** :
- Vérifier l'URL dans l'Inspector : pas de faute de frappe, pas de `/` à la fin
- Tester l'URL directement dans un navigateur (elle doit retourner `null` ou du JSON)

### ❌ "Error 401 Unauthorized"

**Cause** : Les règles Firebase sont trop restrictives.

**Solution** :
- Aller sur Firebase Console → **Realtime Database → Règles**
- Vérifier que les règles permettent la lecture et l'écriture (voir section Règles ci-dessus)
- Cliquer **"Publier"** après modification

### ❌ Les scores Firebase ne s'affichent pas après démarrage

**Cause** : `autoSyncOnStart` est peut-être désactivé, ou `CircuitDatabase` est null au démarrage.

**Solution** :
- Vérifier que `Auto Sync On Start` est coché (✓) dans l'Inspector
- Dans la Console Unity, chercher `[HighscoreManager]` pour voir les messages de sync
- Si `CircuitDatabase` est null : s'assurer que le `CircuitDatabase` ScriptableObject est correctement configuré dans `Assets/Project/Scripts/Settings/`

### ❌ "Les scores ne sont pas poussés vers Firebase"

**Cause** : Le `TryAddScore` a peut-être retourné `false` (score pas dans le top 10).

**Solution** :
- Effacer les scores locaux : sur le `HighscoreManager`, faire un Context Menu → **"Clear All Highscores"**
- Refaire un tour : maintenant n'importe quel temps sera dans le top 10 (tableau vide)

### ❌ Données corrompues dans Firebase

**Solution** :
- Sur Firebase Console → **Realtime Database → Données**
- Clic droit sur le nœud du circuit → **"Supprimer"**
- Le jeu recréera les données proprement au prochain push

---

## 🔒 Sécurité Avancée (Optionnel)

### Modifier la Validation des Temps

Le seuil minimum par défaut est `5.0` secondes. Si vos circuits sont plus longs, ajuster dans `database.rules.json` :

```json
"timeInSeconds": {
  ".validate": "newData.isNumber() && newData.val() > 30.0"
}
```

Puis redéployer :
```bash
firebase deploy --only database
```

### Utiliser Firebase Authentication (Avancé)

Pour une vraie protection anti-triche, vous pouvez activer l'authentification anonyme Firebase :
- Firebase Console → **Authentication → Sign-in method → Anonymous → Activer**
- Modifier les règles pour exiger une authentification : `.write: "auth != null"`
- Intégrer `FirebaseAuth` dans Unity (nécessite le SDK Firebase Unity)

> Ce niveau de protection est optionnel pour un jeu arcade.

---

## 📊 Limites du Plan Gratuit Firebase

| Ressource | Limite Spark (Gratuit) | Estimation SpeedMeUp |
|-----------|------------------------|----------------------|
| Stockage | 1 Go | ~100 circuits × 10 scores × ~200 octets = **200 Ko** ✅ |
| Transfert/mois | 10 Go | ~10 000 parties/mois × 1 Ko = **10 Mo** ✅ |
| Connexions simultanées | 100 | Jeu solo/local ✅ |
| Requêtes | Illimitées | ✅ |

Le plan gratuit est **largement suffisant** pour SpeedMeUp.

---

## ✅ Checklist de Configuration

### Firebase Console
- [ ] Créer un projet Firebase
- [ ] Activer Realtime Database en **mode verrouillé**
- [ ] Choisir la région Europe-West1
- [ ] Copier l'URL de la database
- [ ] Déployer les règles permanentes (`firebase deploy --only database` ou copier-coller `database.rules.json`)

### Unity Inspector
- [ ] Ouvrir la scène `SampleScene.unity`
- [ ] Sélectionner le GameObject **HighscoreManager**
- [ ] Coller l'URL Firebase dans le champ **"Firebase Database Url"**
- [ ] Vérifier que **"Auto Sync On Start"** est coché
- [ ] Sauvegarder la scène (`Ctrl+S`)

### Tests
- [ ] Lancer le jeu → vérifier les logs de synchronisation
- [ ] Faire un tour qualifiant → vérifier le push Firebase
- [ ] Vérifier les données dans la console Firebase
- [ ] Tester sur une deuxième machine → vérifier la synchronisation

---

## 📦 Fichiers Concernés

```
(racine du dépôt)
├── database.rules.json           ← Règles de sécurité Firebase (permanentes, versionnées)
└── firebase.json                 ← Config Firebase CLI (pointe vers database.rules.json)

Assets/Project/Scripts/Core/
└── HighscoreManager.cs           ← Synchronisation Firebase REST

Assets/Project/Scripts/UI/
└── HighscoreDisplayUI.cs         ← Rafraîchissement auto sur événement réseau
```

---

## 🎯 Résumé Visuel : Ce que vous voyez dans Firebase

Après quelques parties, la console Firebase affiche :

```
speedmeup-highscores-default-rtdb
└── highscores
    ├── Circuit_Rapide
    │   └── entries: [{...}, {...}, ...]   ← Top 10 du circuit
    ├── Circuit_Ville
    │   └── entries: [{...}]
    └── Desert_Track
        └── entries: [{...}, {...}]
```

Chaque entrée contient :
```json
{
  "timeInSeconds": 65.432,
  "playerName": "Alice",
  "rank": 1,
  "dateString": "04/03/2026",
  "timeOfDayString": "14:32:10"
}
```

---

**Date de création** : 04 mars 2026  
**Dernière mise à jour** : 05 mars 2026 — Passage aux règles permanentes  
**Version** : 1.1  
**Compatibilité** : Unity 2021.3+, Firebase Realtime Database (Plan Spark)  
**Statut** : ✅ Règles permanentes actives
