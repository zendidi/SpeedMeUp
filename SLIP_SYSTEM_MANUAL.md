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
| `maxSteeringAngleDeg` | ° | `25` | 10–45 | Angle de braquage maximum. Sert à calculer `_frontSlipAngle` (stocké pour le debug et la correction de vélocité), mais **n'affecte PAS la comparaison de détection** (les intensités utilisent les angles bruts sans compensation). |

**Conseils :**
- Mesurez l'empattement dans la scène Unity (distance entre essieu avant et arrière).
- Si votre voiture est petite (kart), utilisez 1.5–2.0. Berline standard ≈ 2.5. SUV ≈ 2.8.

---

### 2.3 Grip des pneus *(nouveaux paramètres)*

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `frontGripCoefficient` | μ | `1.0` | 0.1–2.0 | Coefficient d'adhérence latérale des pneus avant. Valeur plus basse = l'avant sature plus tôt → sous-virage plus facile à déclencher. |
| `rearGripCoefficient` | μ | `0.9` | 0.1–2.0 | Coefficient d'adhérence latérale des pneus arrière. **Valeur par défaut légèrement inférieure au front** → tendance naturelle au survirage. |
| `referenceSpeed` | m/s | `30` | 5–100 | Vitesse à partir de laquelle les effets atteignent 100 % de leur intensité. En dessous, les intensités sont réduites linéairement. |

**Comment ça marche :**
- La détection compare l'angle de glissement *normalisé* de chaque essieu : `slipNormalisé = |angle| / (μ × charge_essieu)`
- Un essieu avec moins de grip (μ faible) OU moins de charge (essieu allégé par transfert de poids) voit son slip normalisé *monter plus vite*.
- Résultat : freiner en virage (charge déplacée vers l'avant → arrière allégé) rend le survirage naturellement plus probable.

**Guide pratique :**

| Profil | `frontGripCoefficient` | `rearGripCoefficient` |
|---|---|---|
| Voiture très stable (neutre/sous-virage) | 0.8 | 1.2 |
| Berline standard (légère tendance sous-virage) | 1.0 | 0.9 |
| Voiture de sport (neutre) | 1.0 | 1.0 |
| Voiture sportive arrière (survirage possible) | 1.0 | 0.7 |
| Drift car | 1.2 | 0.5 |

---

### 2.4 Zone morte *(nouveau paramètre)*

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `lateralVelocityDeadZone` | m/s | `0.2` | 0–2 | Vitesse de glissement latéral **au centre du véhicule** nécessaire avant que le système s'active. Empêche les faux positifs en ligne droite avec de petits coups de volant. |

**Pourquoi c'est important :**
Le volant crée une vitesse angulaire ω qui donne aux essieux une vitesse latérale relative, même sur une ligne droite parfaite. Sans zone morte, ce mouvement pur de rotation serait perçu comme un glissement. La zone morte impose que le *centre de masse* de la voiture soit lui-même en train de déraper avant que la détection se déclenche.

- `0.0` → Aucune zone morte (très sensible, risque de faux positifs sur ligne droite)
- `0.2` → **Zone recommandée** — insensible aux petits coups de volant propres
- `0.5` → Zone large — seulement pour les grosses dérives franches
- `1.0+` → Quasi-insensible, réservé à des setups arcade très light

---

### 2.5 Seuils d'activation

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `oversteerThreshold` | — | `0.08` | 0–0.5 | Différence de slip normalisé (arrière vs avant) nécessaire pour que le survirage s'applique. |
| `understeerThreshold` | — | `0.10` | 0–0.5 | Différence de slip normalisé (avant vs arrière) nécessaire pour que le sous-virage s'applique. |

**Ce que représentent ces seuils :**
Il ne s'agit plus d'angles en radians mais d'une différence de slip *normalisé par le grip*. Une valeur de `0.08` signifie : "le slip normalisé de l'essieu le plus glissant doit dépasser celui de l'autre d'au moins 8 % avant que les effets se déclenchent."

**Guide pratique :**

- `0.02–0.05` → Très sensible
- `0.05–0.15` → **Zone recommandée**
- `0.20–0.40` → Effets tardifs, voiture très stable
- `0.50` → Effets quasi invisibles

---

### 2.6 Intensités

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

### 2.7 Coefficients angulaires

| Variable | Unité | Défaut | Plage | Description |
|---|---|---|---|---|
| `oversteerYawFactor` | — | `3.0` | 0–10 | Multiplicateur du couple angulaire de lacet en survirage. Plus élevé = la voiture pivote plus vite quand l'arrière glisse. |
| `understeerYawDampFactor` | — | `2.0` | 0–10 | Multiplicateur d'amortissement de la rotation en sous-virage. Plus élevé = le volant perd son autorité plus vite. |

**Guide pratique :**

- `oversteerYawFactor` : commencez à `2`, montez jusqu'à `5` si les tête-à-queue ne sont pas visibles.
- `understeerYawDampFactor` : commencez à `1`, montez si la voiture ne semble pas pousser tout droit en sous-virage.

---

### 2.8 Tête-à-queue (survirage extrême)

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

### 2.9 Déport extérieur (sous-virage extrême)

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
| **Magenta** | Mesh des roues arrière + rayon Scene | Survirage (l'arrière glisse) |
| **Rouge** | Mesh des roues arrière + rayon Scene | Tête-à-queue en cours (survirage extrême) |
| **Jaune** | Mesh des roues avant + rayon Scene | Sous-virage (l'avant résiste au virage) |
| **Orange** | Mesh des roues avant + rayon Scene | Déport extérieur actif (sous-virage extrême) |
| Bleu | Centre véhicule (Gizmos) | Vecteur de vélocité |
| Vert | Centre véhicule (Gizmos) | Normale du sol |
| Rouge (sphère) | Essieu avant (Gizmos) | Charge sur l'essieu avant |
| Cyan (sphère) | Essieu arrière (Gizmos) | Charge sur l'essieu arrière |

**Les meshes des roues changent de couleur en temps réel dans la Game View** (via `MaterialPropertyBlock`, sans créer de nouvelles instances de matériau). La couleur est restaurée automatiquement dès que l'intensité redescend à zéro ou quand `_showDebug` est désactivé.

### Comment brancher les refs de roues :
1. Sur le GameObject de la voiture, sélectionner `VehiclePhysics`
2. Chercher la section `WHEEL DEBUG REFS (optionnel)`
3. Glisser les Transform des roues avant dans `_frontWheelDebug` (ex: FL_Wheel, FR_Wheel)
4. Glisser les Transform des roues arrière dans `_rearWheelDebug` (ex: RL_Wheel, RR_Wheel)
5. Chaque Transform doit avoir (ou avoir un enfant avec) un composant `Renderer`

> **Note :** Si aucune ref de roue n'est assignée, des rayons de fallback sont dessinés aux positions calculées depuis l'empattement (Scene view uniquement). Il n'y aura pas de changement de couleur sur les meshes dans ce cas.

---

## 4. Diagnostic rapide en jeu

Ouvrez la **Game View** (les meshes changent de couleur) ou la **Scene view** (rayons + Gizmos) en Play mode :

| Ce que vous voyez | Diagnostic |
|---|---|
| Aucune couleur / rayon | Normal (pas de glissement) ou `_showDebug` désactivé |
| Roues arrière magenta en virage | Survirage léger, normal et contrôlable |
| Roues arrière rouge vif | Tête-à-queue en cours — contre-braquer |
| Roues avant jaunes | Sous-virage, le joueur prend le virage trop vite |
| Roues avant orange | Déport extérieur, la voiture va sortir de la route |
| Effets qui se déclenchent en ligne droite | `lateralVelocityDeadZone` trop faible, augmenter |
| Oversteer impossible à activer | Réduire `rearGripCoefficient`, `oversteerThreshold`, ou `referenceSpeed` |
| Effets trop agressifs | Réduire `oversteerStrength`, `understeerStrength`, ou augmenter les seuils |

### Réglage itératif recommandé

1. Commencez avec tous les paramètres à leurs valeurs par défaut.
2. Activez `_showDebug` et observez en jeu.
3. **Ligne droite avec couleur sur les roues** → augmenter `lateralVelocityDeadZone` (0.3–0.5).
4. **Survirage impossible** → réduire `rearGripCoefficient` (essayez 0.7) et/ou `referenceSpeed` (essayez 15).
5. **Effets trop violents** → réduire `oversteerStrength`/`understeerStrength`.
6. **Tête-à-queue ne se déclenche jamais** → réduire `spinOutThreshold` (0.4) ou augmenter `spinOutAngularMultiplier` (6).
7. Calibrez `wheelbase` en mesurant votre mesh de voiture dans Unity (distance entre essieu avant et arrière).
