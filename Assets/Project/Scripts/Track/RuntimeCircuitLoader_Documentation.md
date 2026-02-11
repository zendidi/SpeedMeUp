# RuntimeCircuitLoader - Documentation

## Vue d'ensemble

Le composant `RuntimeCircuitLoader` permet de charger et changer de circuit au runtime depuis la scène de jeu, avec placement automatique du véhicule au startPoint du circuit sélectionné.

## Installation

1. Ajoutez le composant `RuntimeCircuitLoader` à un GameObject de votre scène (par exemple, un GameObject vide nommé "CircuitLoader")
2. Configurez les paramètres dans l'inspecteur Unity

## Configuration dans l'inspecteur

### Circuit Library (Obligatoire)
- **Available Circuits**: Liste des CircuitData disponibles pour le chargement
  - Cliquez sur le "+" pour ajouter des circuits
  - Assignez vos ScriptableObjects CircuitData depuis le projet

### Vehicle (Auto-détection)
- **Vehicle Controller**: Référence au VehicleController à téléporter
  - Si laissé vide, sera automatiquement détecté au démarrage
  - Recommandé de laisser vide pour l'auto-détection

### UI Integration (Optionnel)
- **Circuit Dropdown**: Dropdown Unity UI pour sélectionner un circuit
  - Si assigné, sera automatiquement rempli avec la liste des circuits
  - Permet la sélection via l'interface utilisateur
- **Load Button**: Bouton Unity UI pour charger le circuit sélectionné
  - Si assigné, sera automatiquement connecté à la fonction LoadCircuit()

### Debug
- **Show Debug UI**: Affiche l'interface de debug OnGUI
  - Activé par défaut
  - Affiche les boutons Précédent/Charger/Suivant en haut à gauche

## Utilisation

### Navigation via Debug UI
Quand "Show Debug UI" est activé, une interface apparaît en haut à gauche de l'écran :
```
=== RUNTIME CIRCUIT LOADER ===
Circuit actuel: [Nom du circuit]
Longueur: [XXX.X]m

Circuit sélectionné: [Nom]
[◀ Précédent] [Charger] [Suivant ▶]
```

- **◀ Précédent**: Sélectionne le circuit précédent (boucle cyclique)
- **Charger**: Charge le circuit actuellement sélectionné
- **Suivant ▶**: Sélectionne le circuit suivant (boucle cyclique)

### Raccourcis clavier
- **N**: Sélectionne le circuit suivant (Next)
- **P**: Sélectionne le circuit précédent (Previous)

### Via l'inspecteur Unity
- Faites un clic droit sur le composant dans l'inspecteur
- Sélectionnez "Load Next Circuit" ou "Load Previous Circuit"

### Via code C#
```csharp
using ArcadeRacer.Managers;

public class MyScript : MonoBehaviour
{
    private RuntimeCircuitLoader circuitLoader;
    
    void Start()
    {
        circuitLoader = FindObjectOfType<RuntimeCircuitLoader>();
    }
    
    // Charger un circuit par index
    void LoadCircuitByIndex(int index)
    {
        circuitLoader.LoadCircuit(index);
    }
    
    // Charger un circuit par référence
    void LoadCircuitByReference(CircuitData circuit)
    {
        circuitLoader.LoadCircuit(circuit);
    }
    
    // Navigation
    void NavigateCircuits()
    {
        circuitLoader.LoadNextCircuit();     // Circuit suivant
        circuitLoader.LoadPreviousCircuit(); // Circuit précédent
    }
}
```

## Comportement

### Au démarrage (Start)
1. Auto-détecte le VehicleController si non assigné
2. Initialise le Dropdown UI si présent (remplit avec la liste des circuits)
3. Connecte le Button UI si présent

### Lors du chargement d'un circuit (LoadCircuit)
1. Valide les données du circuit
2. Appelle `CircuitManager.Instance.LoadCircuit(circuit)`
3. Téléporte le véhicule au startPoint via `CircuitManager.Instance.SpawnVehicle()`
4. Réinitialise la vélocité du Rigidbody (velocity et angularVelocity à zéro)
5. Affiche les informations de debug dans la console

### Navigation (Next/Previous)
- La navigation est cyclique : après le dernier circuit, retourne au premier
- Met à jour automatiquement le Dropdown UI si présent
- Affiche le nom du circuit sélectionné dans la console

## Gestion d'erreurs

Le composant gère robustement les cas suivants :
- Liste de circuits vide ou null
- Index de circuit invalide (hors limites)
- CircuitData null
- VehicleController non trouvé
- Rigidbody manquant sur le véhicule

Tous les cas d'erreur affichent un message explicite dans la console Unity.

## Intégration avec l'architecture existante

Le composant utilise :
- `CircuitManager.Instance.LoadCircuit(CircuitData)` - Charge le circuit
- `CircuitManager.Instance.SpawnVehicle(Transform)` - Place le véhicule
- `CircuitManager.Instance.IsCircuitLoaded` - Vérifie si un circuit est chargé
- `CircuitManager.Instance.CurrentCircuit` - Obtient les infos du circuit actuel
- `CircuitManager.Instance.SpawnPoint` - Position de spawn pour les logs

## Exemple de configuration complète

1. Créez plusieurs CircuitData ScriptableObjects dans votre projet
2. Créez un GameObject "CircuitLoader" dans votre scène
3. Ajoutez le composant `RuntimeCircuitLoader`
4. Assignez vos CircuitData à "Available Circuits"
5. Laissez "Vehicle Controller" vide pour auto-détection
6. (Optionnel) Créez une UI avec un Dropdown et un Button
7. (Optionnel) Assignez ces éléments UI au composant
8. Lancez le jeu et testez la navigation avec N/P ou l'interface OnGUI

## Notes importantes

- Le composant nécessite un `CircuitManager` présent dans la scène
- Le véhicule doit avoir un component `Rigidbody` pour la réinitialisation de vélocité
- La liste des circuits doit contenir au moins un circuit valide pour fonctionner
- L'interface OnGUI est destinée au debug, désactivez-la en production si nécessaire
