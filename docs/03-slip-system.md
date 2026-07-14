# 3. Système de survirage / sous-virage

Doc de référence complète : [`SLIP_SYSTEM_MANUAL.md`](../SLIP_SYSTEM_MANUAL.md) (racine du dépôt, conservé tel quel, toujours à jour). Ce fichier en résume la structure pour l'onboarding.

## Boucle de physique

```
VehicleInput → VehicleController.Update()
                   ↓ SetInputs(throttle, brake, steering, drift)
             VehiclePhysics.FixedUpdate()
                   ↓ ApplySteering() → ApplyGripOrDrift()
             VehiclePhysicsCore.ComputeSlipEffect()
                   ↓ délègue
             VehicleSlipCalculator.ComputeSlipCorrection()
                   ↓ utilise
             PhysicsFormulas.SlipAngle() + AxleLateralVelocity()
```

## Principe

Le système calcule un angle de glissement par essieu (avant/arrière) à partir de la vitesse latérale globale et de la vitesse angulaire de lacet. Il compare le glissement normalisé par le grip (`μ`) de chaque essieu pour détecter 4 régimes :

| Régime | Condition |
|---|---|
| Survirage | `\|slip_arrière\| > \|slip_avant\| + oversteerThreshold` |
| Sous-virage | `\|slip_avant\| > \|slip_arrière\| + understeerThreshold` |
| Tête-à-queue | Survirage + intensité > `spinOutThreshold` |
| Déport extérieur | Sous-virage + intensité > `understeerPushThreshold` |

Freiner ou accélérer trop fort en virage déplace la charge entre essieux (`frontGripCoefficient`/`rearGripCoefficient` × charge), ce qui rend le survirage/sous-virage naturellement plus probable — pas besoin de scripter ces cas séparément.

## Paramètres principaux (`VehiclePhysicsCore` → `slipCalculator` dans l'Inspector)

| Catégorie | Variables clés | Valeurs par défaut |
|---|---|---|
| Géométrie | `wheelbase` (empattement, m), `maxSteeringAngleDeg` | 2.5 / 25° |
| Grip | `frontGripCoefficient`, `rearGripCoefficient`, `referenceSpeed` | 1.0 / 0.9 (arrière plus faible → tendance survirage) / 30 m/s |
| Zone morte | `lateralVelocityDeadZone` — évite les faux positifs en ligne droite | 0.2 m/s |
| Seuils | `oversteerThreshold`, `understeerThreshold` | 0.08 / 0.10 |
| Intensité | `oversteerStrength`, `understeerStrength` | 1.0 / 0.8 |
| Couples angulaires | `oversteerYawFactor`, `understeerYawDampFactor` | 3.0 / 2.0 |
| Tête-à-queue | `spinOutThreshold`, `spinOutAngularMultiplier`, `spinOutMaxAngularVelocity` | 0.65 / 4.0 / 12 rad/s |
| Déport extérieur | `understeerPushThreshold`, `understeerOutwardPushStrength` | 0.5 / 4.0 m/s² |

Guides pratiques par profil de véhicule (kart arcade, berline, sportive, drift car...) : voir le tableau détaillé dans `SLIP_SYSTEM_MANUAL.md` §2.

## Debug visuel

Activer `_showDebug` sur `VehiclePhysics`. Code couleur sur les meshes de roues (via `MaterialPropertyBlock`, sans instancier de matériau) :

| Couleur | Roues | Signification |
|---|---|---|
| Magenta | Arrière | Survirage |
| Rouge | Arrière | Tête-à-queue |
| Jaune | Avant | Sous-virage |
| Orange | Avant | Déport extérieur |

Brancher les refs de roues dans `VehiclePhysics` → section `WHEEL DEBUG REFS` (sinon fallback en rayons Scene view uniquement, pas de couleur sur mesh).

## Réglage itératif recommandé

1. Partir des valeurs par défaut, activer `_showDebug`.
2. Couleur en ligne droite → augmenter `lateralVelocityDeadZone` (0.3–0.5).
3. Survirage impossible à obtenir → réduire `rearGripCoefficient` (~0.7) et/ou `referenceSpeed` (~15).
4. Effets trop violents → réduire `oversteerStrength`/`understeerStrength`.
5. Tête-à-queue ne se déclenche jamais → réduire `spinOutThreshold` (0.4) ou augmenter `spinOutAngularMultiplier` (6).
6. Calibrer `wheelbase` sur la distance réelle essieu avant/arrière du mesh 3D utilisé.
