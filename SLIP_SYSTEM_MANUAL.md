# Manuel d'utilisation — Système Survirage / Sous-virage

## 1. Comment ça fonctionne dans CE projet

### La boucle de physique (rappel rapide)

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

### Ce que calcule le système, pas à pas

#### Étape 1 — Vitesse latérale par essieu

La voiture a une vitesse **latérale globale** `v_lat` (m/s) — c'est la composante de la
vélocité dans la direction `transform.right`. Mais quand le véhicule tourne, les deux essieux
n'ont pas la même vitesse latérale : l'essieu avant est « tiré » d'un côté, l'arrière de
l'autre.

```
v_avant  = v_lat + ω × (L/2)
v_arrière = v_lat - ω × (L/2)
```

où `ω` (omega) est la vitesse angulaire de lacet en rad/s et `L/2` le demi-empattement.

#### Étape 2 — Angle de glissement par essieu

```
α = atan2(v_lat_essieu, |v_longitudinale| + 0.1)
```

L'angle de glissement mesure **à quel point un pneu glisse de côté par rapport à sa direction
de roulement**. Un pneu qui roule parfaitement droit a α = 0. Un pneu qui glisse à 45° a
α = 0.785 rad (~45°).

Pour l'essieu avant, l'angle de braquage du joueur (`steeringInput × maxSteeringAngleDeg`) est
**soustrait** de l'angle de glissement. Cela modélise le fait que le conducteur compense
partiellement le glissement avec le volant.

#### Étape 3 — Détection du régime

| Condition | Régime |
|---|---|
| `|α_arrière| > |α_avant| + oversteerThreshold` | **Survirage** |
| `|α_avant| > |α_arrière| + understeerThreshold` | **Sous-virage** |
| Survirage ET intensity > `spinOutThreshold` | **Tête-à-queue** |
| Sous-virage ET intensity > `understeerPushThreshold` | **Déport extérieur** |

#### Étape 4 — Corrections appliquées à la vélocité

**Survirage normal**
- Force latérale dans la direction du glissement arrière → l'arrière part
- Delta angulaire positif → la voiture tourne plus vite dans le sens du virage

**Tête-à-queue (survirage extrême)**
- Couple angulaire supplémentaire auto-entretenu (le plafond de vitesse angulaire est relevé)
- La rotation s'emballe progressivement jusqu'à `spinOutMaxAngularVelocity`

**Sous-virage normal**
- Le delta angulaire est atténué → le volant est moins efficace, la voiture tend à aller tout droit

**Déport extérieur (sous-virage extrême)**
- Force latérale vers **l'extérieur** du virage → la voiture est poussée à l'opposé du braquage
- Exemple : virage à droite + sous-virage → poussée vers la gauche (outside)

---

## 2. Toutes les variables exposées

### Dans `VehiclePhysicsCore` → section `=== SURVIRAGE / SOUS-VIRAGE ===`

Les paramètres ci-dessous sont dans le sous-objet `slipCalculator` visible dans l'Inspector.

---

### 2.1 Activation

| Variable | Type | Défaut | Description |
|---|---|---|---|
| `enabled` | bool | `true` | Désactive tout le système sans toucher aux autres paramètres |

---

### 2.2 Géométrie

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `wheelbase` | m | `2.5` | 1.5–4.0 | Empattement (distance entre essieux). **Doit correspondre à la taille réelle du modèle 3D.** Un empattement trop petit exagère les effets. |
| `maxSteeringAngleDeg` | ° | `25` | 10–45 | Angle de braquage maximum. Sert à normaliser le bénéfice du contre-braquage du joueur. Une valeur haute = le joueur corrige plus facilement. |

**Conseils :**
- Mesurez l'empattement dans la scène Unity (distance entre essieu avant et arrière).
- Si votre voiture est petite (kart), utilisez 1.5–2.0. Berline standard ≈ 2.5. SUV ≈ 2.8.

---

### 2.3 Seuils de déclenchement

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `oversteerThreshold` | rad | `0.08` | 0.01–0.5 | Différence d'angle de glissement arrière/avant à partir de laquelle le survirage s'active. |
| `understeerThreshold` | rad | `0.10` | 0.01–0.5 | Différence d'angle de glissement avant/arrière à partir de laquelle le sous-virage s'active. |

**Guide pratique :**

- `0.04–0.06` → Voiture très sensible, sur-répond en permanence (ressenti d'instabilité)
- `0.08–0.12` → **Zone recommandée** — effets présents mais pas envahissants
- `0.15–0.25` → Effets tardifs, voiture très neutre (proche d'une voiture de sport arcade ferme)
- `0.30+` → Effets quasi invisibles sauf manœuvres très extrêmes

*0.08 rad ≈ 4.6° de différence d'angle entre essieux. C'est réaliste.*

---

### 2.4 Intensités

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `oversteerStrength` | — | `1.0` | 0–3 | Amplification globale du survirage (force latérale + couple). Mettre à 0 = désactiver sans toucher aux seuils. |
| `understeerStrength` | — | `0.8` | 0–3 | Amplification de la résistance de direction en sous-virage. |

**Guide pratique :**

- `0.0` → Aucun effet (désactivé)
- `0.5` → Effet léger, bon point de départ pour un jeu arcade
- `1.0` → Valeur de référence
- `2.0` → Très prononcé, perd rapidement le contrôle
- `3.0` → Extrême, uniquement pour tests

---

### 2.5 Coefficients angulaires

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `oversteerYawFactor` | — | `3.0` | 0–10 | Multiplicateur du couple angulaire de lacet en survirage. Plus élevé = la voiture pivote plus vite quand l'arrière glisse. |
| `understeerYawDampFactor` | — | `2.0` | 0–10 | Multiplicateur d'amortissement de la rotation en sous-virage. Plus élevé = le volant perd son autorité plus vite. |

**Guide pratique :**

- `oversteerYawFactor` : commencez à `2`, montez jusqu'à `5` si les tête-à-queue ne sont pas visibles.
- `understeerYawDampFactor` : commencez à `1`, montez si la voiture ne semble pas pousser tout droit en sous-virage.

---

### 2.6 Tête-à-queue (survirage extrême)

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `spinOutThreshold` | 0–1 | `0.65` | 0–1 | Intensité de survirage au-delà de laquelle le tête-à-queue se déclenche. `0` = toujours actif, `1` = jamais actif. |
| `spinOutAngularMultiplier` | — | `4.0` | 0–10 | Couple angulaire supplémentaire pendant le tête-à-queue. Contrôle la vitesse à laquelle la rotation s'emballe. |
| `spinOutMaxAngularVelocity` | rad/s | `12.0` | 5–30 | Plafond de vitesse angulaire pendant le tête-à-queue. 5 ≈ 1 tour/1.3s ; 12 ≈ 1 tour/0.5s ; 25 ≈ 1 tour/0.25s |

**Correspondance physique :**
- Un **freinage trop brutal en virage** transfère la charge vers l'avant → l'arrière perd de l'adhérence → `_rearAxleLoad` baisse → vitesse latérale arrière augmente → `_rearSlipAngle` dépasse le seuil → tête-à-queue.
- Un **trop-plein d'accélération en sortie de virage** (traction arrière) a le même effet.

**Guide pratique (scénarios) :**

| Profil voiture | `spinOutThreshold` | `spinOutAngularMultiplier` | `spinOutMaxAngularVelocity` |
|---|---|---|---|
| Kart arcade très joueur | `0.50` | `5` | `15` |
| Berline de course normale | `0.65` | `4` | `12` |
| Bolide stable difficile à piéger | `0.80` | `3` | `8` |
| Mode simulation exigeant | `0.55` | `6` | `20` |

---

### 2.7 Déport extérieur (sous-virage extrême)

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `understeerPushThreshold` | 0–1 | `0.5` | 0–1 | Intensité de sous-virage à partir de laquelle la voiture est poussée vers l'extérieur. |
| `understeerOutwardPushStrength` | m/s² | `4.0` | 0–15 | Force de déport (m/s de delta par seconde normalisé par vitesse). Plus élevé = la voiture part plus fort vers l'extérieur. |

**Correspondance physique :**
- **Virage trop rapide** → la vitesse latérale dépasse ce que les pneus avant peuvent encaisser → l'angle de glissement avant explose → sous-virage → la voiture ne tourne pas assez → elle est projetée vers l'extérieur de l'arc de virage.

**Guide pratique (scénarios) :**

| Profil voiture | `understeerPushThreshold` | `understeerOutwardPushStrength` |
|---|---|---|
| Voiture arcade légère (sous-virage doux) | `0.60` | `2` |
| Berline de route (comportement neutre) | `0.50` | `4` |
| Camion / SUV (sous-virage fort) | `0.35` | `7` |
| Mode simulation (punissant) | `0.40` | `8` |

---

## 3. Debug visuel — Code couleur

Activer le flag `_showDebug` sur `VehiclePhysics` dans l'Inspector.

| Couleur | Endroit | Signification |
|---|---|---|
| **Magenta** | Roues arrière | Survirage (l'arrière glisse) |
| **Rouge** | Roues arrière | Tête-à-queue en cours (survirage extrême) — la magenta vire au rouge |
| **Jaune** | Roues avant | Sous-virage (l'avant résiste au virage) |
| **Orange** | Roues avant | Déport extérieur actif (sous-virage extrême) — le jaune vire à l'orange |
| Bleu | Centre véhicule | Vecteur de vélocité |
| Vert | Centre véhicule | Normale du sol |
| Rouge (sphère) | Essieu avant | Charge sur l'essieu avant |
| Cyan (sphère) | Essieu arrière | Charge sur l'essieu arrière |

Les rayons de debug sont dessinés **aux positions des roues** si vous avez renseigné
`_frontWheelDebug` et `_rearWheelDebug` dans l'Inspector. Sinon, ils sont dessinés
à des positions calculées depuis l'empattement.

### Comment brancher les refs de roues :
1. Sur le GameObject de la voiture, sélectionner `VehiclePhysics`
2. Chercher la section `WHEEL DEBUG REFS (optionnel)`
3. Glisser les Transform des roues avant dans `_frontWheelDebug` (ex: FL_Wheel, FR_Wheel)
4. Glisser les Transform des roues arrière dans `_rearWheelDebug` (ex: RL_Wheel, RR_Wheel)

---

## 4. Diagnostic rapide en jeu

Ouvrez la **Scene view** pendant le Play mode (avec Gizmos activés) :

| Ce que vous voyez | Diagnostic |
|---|---|
| Aucun rayon | Normal (pas de glissement) ou `_showDebug` désactivé |
| Rayon magenta court sur les roues arrière en virage | Survirage léger, normal et contrôlable |
| Rayon magenta long + sphère rouge qui grossit | Approche du tête-à-queue, réduire la vitesse ou contre-braquer |
| Rayon rouge vif sur les roues arrière | Tête-à-queue en cours |
| Rayon jaune sur les roues avant | Sous-virage, le joueur prend le virage trop vite |
| Rayon orange sur les roues avant | Déport extérieur, la voiture va sortir de la route |

### Réglage itératif recommandé

1. Commencez avec tous les paramètres à leurs valeurs par défaut.
2. Activez `_showDebug` et observez en jeu à quelle fréquence les rayons apparaissent.
3. Si les rayons apparaissent trop souvent → augmenter les seuils (`oversteerThreshold`, `understeerThreshold`).
4. Si le tête-à-queue ne se déclenche jamais → réduire `spinOutThreshold` ou augmenter `spinOutAngularMultiplier`.
5. Si le comportement est trop brutal → réduire `oversteerStrength`, `understeerStrength`.
6. Calibrez `wheelbase` en mesurant votre mesh de voiture dans Unity (Distance entre essieu avant et arrière).
