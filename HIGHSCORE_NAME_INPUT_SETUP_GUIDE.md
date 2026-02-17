# 🏆 Guide de Configuration - Modal de Saisie du Nom pour Highscore

## Vue d'Ensemble

Ce système affiche automatiquement un modal de saisie du nom du joueur lorsqu'il réalise un temps qui se qualifie pour le **top 10** du circuit en cours. Le modal apparaît **immédiatement après la complétion d'un tour**, indépendamment du nombre de tours total de la course.

---

## 📋 Fonctionnalités

✅ **Détection Automatique** : Vérifie si le temps au tour est un top 10  
✅ **Validation Instantanée** : Fonctionne dès la fin d'un tour, pas besoin de finir la course  
✅ **Input Field TMPro** : Saisie du nom avec validation  
✅ **Limite de Caractères** : Configurable dans l'Inspector  
✅ **Pause du Jeu** : Option pour bloquer les inputs pendant la saisie  
✅ **Validation** : Bouton confirm désactivé si le champ est vide  
✅ **Submit avec Enter** : Validation rapide au clavier  
✅ **Nom par Défaut** : "Player" si annulation  

---

## 🎮 Configuration dans Unity

### Étape 1 : Créer la Hiérarchie UI

Dans votre Canvas, créez la structure suivante :

```
Canvas
└── HighscoreNameInputModal (GameObject)
    ├── Modal (Panel - Image)
    │   ├── Background (Image - semi-transparent)
    │   ├── ContentPanel (Panel)
    │   │   ├── TitleText (TextMeshProUGUI)
    │   │   ├── MessageText (TextMeshProUGUI)
    │   │   ├── NameInputField (TMP_InputField)
    │   │   └── ButtonsPanel (Horizontal Layout Group)
    │   │       ├── ConfirmButton (Button + TextMeshProUGUI)
    │   │       └── CancelButton (Button + TextMeshProUGUI) [Optionnel]
```

### Étape 2 : Configuration des Composants

#### HighscoreNameInputModal (GameObject racine)
1. Ajouter le component `HighscoreNameInputUI`
2. Configurer dans l'Inspector :

```
=== UI COMPONENTS ===
Modal Panel         : → Modal (Panel)
Name Input Field    : → NameInputField (TMP_InputField)
Confirm Button      : → ConfirmButton (Button)
Cancel Button       : → CancelButton (Button) [Optionnel]
Title Text          : → TitleText (TextMeshProUGUI)
Message Text        : → MessageText (TextMeshProUGUI)

=== SETTINGS ===
Max Characters           : 20 (ou votre choix)
Default Player Name      : "Player"
Block Game Input While Open : ✓ (pause le jeu pendant la saisie)

=== MESSAGES ===
Title Message       : "🏆 NOUVEAU RECORD !"
Prompt Message      : "Entrez votre nom :"
```

#### Modal Panel
- **RectTransform** : Stretch to fill parent
- **Image** : Background color (ex: noir semi-transparent, Alpha 200)

#### ContentPanel
- **RectTransform** : Center, Width 500, Height 400
- **Image** : Panel background (ex: blanc ou couleur de votre thème)
- **Vertical Layout Group** :
  - Padding : 20
  - Spacing : 15
  - Child Force Expand : Width ✓, Height ✗

#### TitleText
- **TextMeshProUGUI** :
  - Font Size : 36
  - Alignment : Center
  - Color : Couleur de votre thème
  - Auto Size : Off

#### MessageText
- **TextMeshProUGUI** :
  - Font Size : 20
  - Alignment : Center
  - Color : Gris ou blanc
  - Wrapping : Enabled

#### NameInputField
- **TMP_InputField** :
  - Text Component : InputField Text (TextMeshProUGUI)
  - Placeholder : "Votre nom..." (TextMeshProUGUI)
  - Character Limit : Géré par le script
  - Content Type : Standard
  - Line Type : Single Line
- **Image** (Background) : Bordure ou fond

#### ButtonsPanel
- **Horizontal Layout Group** :
  - Spacing : 10
  - Child Force Expand : Width ✓, Height ✓
  - Child Control Size : Width ✓, Height ✓

#### ConfirmButton
- **Button** :
  - Transition : Color Tint
  - Normal Color : Vert
  - Highlighted : Vert clair
  - Pressed : Vert foncé
  - Disabled : Gris (quand le champ est vide)
- **TextMeshProUGUI** (enfant) : "CONFIRMER" ou "OK"

#### CancelButton (Optionnel)
- **Button** :
  - Transition : Color Tint
  - Normal Color : Rouge
- **TextMeshProUGUI** (enfant) : "ANNULER"

### Étape 3 : Intégration dans UIManager

1. Ouvrir votre scène avec le `UIManager`
2. Sélectionner le GameObject contenant le `UIManager`
3. Dans l'Inspector, section "UI COMPONENTS" :
   - **Highscore Name Input UI** : → Assigner votre `HighscoreNameInputModal`

Le script détectera automatiquement le composant si non assigné.

### Étape 4 : Vérification du RaceManager

Le `RaceManager` est déjà configuré pour fonctionner automatiquement. Il :
1. Détecte les temps qualifiants après chaque tour
2. Affiche le modal automatiquement
3. Sauvegarde le highscore avec le nom du joueur

Aucune configuration supplémentaire nécessaire !

---

## 🎨 Personnalisation

### Changer le Nombre Maximum de Caractères

Dans l'Inspector du `HighscoreNameInputUI` :
```
Max Characters : 15  (au lieu de 20)
```

### Modifier les Messages

Dans l'Inspector du `HighscoreNameInputUI` :
```
Title Message  : "🎉 BRAVO !"
Prompt Message : "Comment vous appelez-vous ?"
```

Ou par code :
```csharp
highscoreNameInputUI.SetTitleMessage("🎉 BRAVO !");
highscoreNameInputUI.SetPromptMessage("Comment vous appelez-vous ?");
```

### Ne Pas Bloquer le Jeu Pendant la Saisie

Dans l'Inspector du `HighscoreNameInputUI` :
```
Block Game Input While Open : ✗  (décocher)
```

### Personnaliser le Nom par Défaut

Dans l'Inspector du `HighscoreNameInputUI` :
```
Default Player Name : "Anonyme"
```

---

## 🔧 Utilisation du Système

### Flux Automatique (Recommandé)

Le système fonctionne automatiquement :

1. **Joueur termine un tour**
2. `RaceManager` vérifie si c'est un temps top 10
3. Si oui → Modal s'affiche automatiquement
4. Joueur entre son nom
5. Confirme avec le bouton ou Enter
6. Highscore sauvegardé automatiquement

### Utilisation Manuelle (Avancé)

Si vous voulez afficher le modal manuellement :

```csharp
using ArcadeRacer.UI;

public class MyScript : MonoBehaviour
{
    private HighscoreNameInputUI nameInputUI;

    void Start()
    {
        nameInputUI = FindFirstObjectByType<HighscoreNameInputUI>();
        
        // S'abonner aux événements
        nameInputUI.OnNameSubmitted += OnPlayerNameEntered;
        nameInputUI.OnCancelled += OnPlayerCancelled;
    }

    void ShowModal()
    {
        float lapTime = 65.432f;
        string circuitName = "Desert Track";
        
        nameInputUI.Show(lapTime, circuitName);
    }

    void OnPlayerNameEntered(string playerName)
    {
        Debug.Log($"Nom reçu : {playerName}");
        // Sauvegarder dans HighscoreManager
    }

    void OnPlayerCancelled()
    {
        Debug.Log("Saisie annulée");
    }

    void OnDestroy()
    {
        if (nameInputUI != null)
        {
            nameInputUI.OnNameSubmitted -= OnPlayerNameEntered;
            nameInputUI.OnCancelled -= OnPlayerCancelled;
        }
    }
}
```

---

## 🧪 Tests

### Test 1 : Temps Qualifiant
1. Démarrer une course
2. Réaliser un bon temps (top 10)
3. Finir le tour
4. ✅ **Attendu** : Modal s'affiche avec le temps et le circuit

### Test 2 : Temps Non Qualifiant
1. Démarrer une course
2. Réaliser un temps moyen (pas top 10)
3. Finir le tour
4. ✅ **Attendu** : Pas de modal (continue normalement)

### Test 3 : Validation du Nom
1. Modal affiché
2. Champ vide
3. ✅ **Attendu** : Bouton "Confirmer" désactivé (grisé)
4. Taper un nom
5. ✅ **Attendu** : Bouton "Confirmer" activé

### Test 4 : Submit avec Enter
1. Modal affiché
2. Taper un nom
3. Presser Enter
4. ✅ **Attendu** : Modal se ferme, highscore sauvegardé

### Test 5 : Limite de Caractères
1. Modal affiché
2. Taper plus de caractères que la limite
3. ✅ **Attendu** : Input s'arrête à la limite (ex: 20 chars)

### Test 6 : Annulation
1. Modal affiché
2. Cliquer "Annuler" (si bouton présent)
3. ✅ **Attendu** : Modal se ferme, highscore sauvegardé avec "Player"

### Test 7 : Plusieurs Tours
1. Faire un 1er tour qualifiant → Modal
2. Entrer nom → Confirmer
3. Faire un 2ème tour qualifiant → Modal
4. ✅ **Attendu** : Modal s'affiche à nouveau correctement

### Test 8 : Vérification Highscore
1. Compléter un tour qualifiant
2. Entrer le nom "TestPlayer123"
3. Confirmer
4. Menu → HighscoreManager → Context Menu "Debug: Display All Highscores"
5. ✅ **Attendu** : "TestPlayer123" apparaît dans la liste avec le bon temps

---

## 🐛 Dépannage

### Le Modal ne s'Affiche Pas

**Causes possibles :**
1. Le temps n'est pas un top 10
   - Vérifier dans la console : `"🏆 Temps qualifiant pour le top 10"`
   - Faire Context Menu sur HighscoreManager → "Clear Highscores" pour réinitialiser
   
2. Le HighscoreNameInputUI n'est pas dans la scène
   - Vérifier que le GameObject existe dans le Canvas
   - Vérifier que le component `HighscoreNameInputUI` est attaché

3. Le UIManager ne trouve pas le composant
   - Assigner manuellement dans l'Inspector du UIManager
   - Vérifier les logs console pour erreurs

### Le Bouton Confirmer est Toujours Grisé

**Solution :**
- Vérifier que le `TMP_InputField` est bien assigné dans l'Inspector
- Vérifier que le `Confirm Button` est bien assigné
- Essayer de taper dans le champ → le bouton devrait s'activer

### Le Jeu ne se Met pas en Pause

**Solution :**
- Dans l'Inspector du `HighscoreNameInputUI` :
  - `Block Game Input While Open` : ✓ cocher

### Le Nom n'est pas Sauvegardé

**Solution :**
1. Vérifier les logs console pour voir si `OnPlayerNameSubmitted` est appelé
2. Vérifier que `CircuitManager.CurrentCircuit` existe
3. Utiliser le context menu sur HighscoreManager pour vérifier les highscores

### La Limite de Caractères ne Fonctionne pas

**Solution :**
- Le `TMP_InputField` a sa propre limite ET le script a une limite
- Vérifier les deux :
  - Inspector du `TMP_InputField` → Character Limit
  - Inspector du `HighscoreNameInputUI` → Max Characters

---

## 📝 Notes Techniques

### Indépendance du Système de Tours

Ce système est **totalement indépendant** du nombre de tours de la course :
- Fonctionne avec 1 tour, 3 tours, 10 tours, etc.
- Peut afficher le modal plusieurs fois dans la même course
- Compatible avec le futur système sans limitation de tours

### Performance

Le modal utilise `Time.timeScale = 0` quand `blockGameInputWhileOpen = true` :
- Le jeu est en pause (physique, animations)
- L'UI continue de fonctionner normalement
- Remis à `1` quand le modal se ferme

### Validation du Top 10

La vérification utilise `HighscoreManager.WouldBeTopScore()` :
- Compare avec les 10 meilleurs temps existants
- Retourne `true` si le temps est meilleur que le 10ème
- Ou si il y a moins de 10 scores enregistrés

### Gestion des Events

Le système utilise des événements C# :
```csharp
public event Action<string> OnNameSubmitted;
public event Action OnCancelled;
```

Important : **Toujours unsubscribe** dans `OnDestroy()` pour éviter les fuites mémoire !

---

## 📦 Fichiers Créés

```
Assets/Project/Scripts/UI/
└── HighscoreNameInputUI.cs  (nouveau)

Assets/Project/Scripts/Track/
└── RaceManager.cs            (modifié)

Assets/Project/Scripts/UI/
└── UIManager.cs              (modifié)
```

---

## ✅ Checklist de Configuration

- [ ] Créer la hiérarchie UI dans le Canvas
- [ ] Ajouter le component `HighscoreNameInputUI`
- [ ] Assigner tous les champs dans l'Inspector
- [ ] Configurer le `Max Characters`
- [ ] Personnaliser les messages si souhaité
- [ ] Assigner dans le `UIManager`
- [ ] Tester avec un temps qualifiant
- [ ] Tester avec un temps non qualifiant
- [ ] Vérifier la sauvegarde dans HighscoreManager

---

## 🎯 Exemple Visuel

```
┌──────────────────────────────────────────┐
│                                          │
│         🏆 NOUVEAU RECORD !              │
│                                          │
│        Entrez votre nom :                │
│                                          │
│     Temps: 01:05.432 sur Circuit1        │
│                                          │
│  ┌──────────────────────────────────┐   │
│  │  [Votre nom...]_________________ │   │
│  └──────────────────────────────────┘   │
│                                          │
│  ┌─────────────┐    ┌──────────────┐   │
│  │  CONFIRMER  │    │   ANNULER    │   │
│  └─────────────┘    └──────────────┘   │
│                                          │
└──────────────────────────────────────────┘
```

---

## 🚀 Prochaines Étapes

Après avoir configuré le système :

1. **Tester** avec différents scénarios
2. **Personnaliser** les couleurs et messages
3. **Ajuster** la limite de caractères selon vos besoins
4. **Optionnel** : Ajouter des animations d'apparition/disparition
5. **Optionnel** : Ajouter des sons (woosh, confirmation)

---

## 💡 Conseils

- Utilisez un **placeholder clair** dans le TMP_InputField : "Entrez votre nom..."
- Gardez la **limite de caractères raisonnable** : 15-25 caractères
- Les **emojis** fonctionnent dans les messages (🏆, 🎉, etc.)
- Testez avec le **clavier ET la souris** (Enter et bouton)
- Pensez à **localiser** les messages si votre jeu est multilingue

---

**Date de création** : 17 février 2026  
**Version** : 1.0  
**Compatibilité** : Unity 2021.3+, TextMeshPro  
**Statut** : ✅ Complet et prêt à l'emploi
