using UnityEngine;

namespace ArcadeRacer.Vehicle
{
    /// <summary>
    /// Système de physique de virage basé sur des points de charge simulés sur les essieux.
    ///
    /// Ce composant remplace VehicleSlipCalculator/VehiclePhysicsCore pour la détection
    /// survirage/sous-virage quand il est présent sur le même GameObject que VehiclePhysics.
    ///
    /// Modèle :
    ///   ① La voie de chaque essieu est mesurée automatiquement depuis les positions des roues.
    ///   ② Un point de charge se déplace sur l'axe de chaque essieu en fonction du steering et
    ///      de la vitesse (centrifugation simulée). Il est centré au repos et se déplace vers la
    ///      roue extérieure quand on tourne.
    ///   ③ Une intensité de virage s'accumule tant qu'on tient le volant, puis décroît rapidement.
    ///   ④ Survirage  = accélération  × intensité_virage × |déplacement_charge_avant|
    ///      Sous-virage = freinage_adapt. × intensité_virage × |déplacement_charge_arrière|
    ///   ⑤ Le freinage est adaptatif : il monte progressivement et retombe plus vite qu'il ne monte.
    ///   ⑥ Feedback couleur des roues : arrière magenta→rouge (survirage), avant jaune→orange (sous-virage).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VehiclePhysics))]
    public class VehicleCorneringPhysics : MonoBehaviour
    {
        #region Configuration — Wheel References

        [Header("=== ROUES (mesure de voie + feedback couleur) ===")]
        [Tooltip("Roues avant (FL puis FR). La voie est mesurée depuis ces positions en Awake.")]
        [SerializeField] private Transform[] frontWheels;

        [Tooltip("Roues arrière (RL puis RR). La voie est mesurée depuis ces positions en Awake.")]
        [SerializeField] private Transform[] rearWheels;

        #endregion

        #region Configuration — Axle Geometry

        [Header("=== GÉOMÉTRIE DES ESSIEUX ===")]
        [Tooltip("Multiplicateur d'intensité des effets pour l'essieu AVANT. " +
                 "Appliqué à la voie mesurée. 1 = proportionnel à la voie réelle, > 1 = amplifié.")]
        [Range(0.1f, 5f)]
        public float frontAxleEffectMultiplier = 1f;

        [Tooltip("Multiplicateur d'intensité des effets pour l'essieu ARRIÈRE.")]
        [Range(0.1f, 5f)]
        public float rearAxleEffectMultiplier = 1f;

        #endregion

        #region Configuration — Load Points

        [Header("=== POINTS DE CHARGE ===")]
        [Tooltip("Vitesse de déplacement du point de charge sur l'essieu AVANT (unités/s). " +
                 "Plus élevé = réaction plus rapide au steering.")]
        [Range(0.1f, 15f)]
        public float frontLoadSensitivity = 2.5f;

        [Tooltip("Vitesse de déplacement du point de charge sur l'essieu ARRIÈRE (unités/s). " +
                 "Valeur indépendante du front pour affiner le comportement.")]
        [Range(0.1f, 15f)]
        public float rearLoadSensitivity = 2f;

        [Tooltip("Vitesse de retour des points de charge au centre quand le volant est relâché (unités/s).")]
        [Range(0.5f, 20f)]
        public float loadReturnSpeed = 4f;

        [Tooltip("Vitesse (km/h) à partir de laquelle le déplacement des points de charge atteint " +
                 "son amplitude maximale. En dessous : amplitude réduite proportionnellement.")]
        [Range(20f, 250f)]
        public float loadReferenceSpeedKMH = 80f;

        #endregion

        #region Configuration — Turn Intensity

        [Header("=== INTENSITÉ DE VIRAGE ===")]
        [Tooltip("Taux de montée de l'intensité de virage par seconde, multiplié par |steering|. " +
                 "Avec 1.5 et steering plein : ~0.67s pour atteindre l'intensité max.")]
        [Range(0.1f, 10f)]
        public float turnIntensityBuildRate = 1.5f;

        [Tooltip("Taux de décroissance de l'intensité de virage par seconde quand le volant est relâché. " +
                 "Doit être > buildRate pour un retour plus rapide.")]
        [Range(0.2f, 20f)]
        public float turnIntensityDecayRate = 3f;

        #endregion

        #region Configuration — Adaptive Braking

        [Header("=== FREINAGE ADAPTATIF ===")]
        [Tooltip("Taux de montée de l'intensité de freinage par seconde. " +
                 "Le joueur doit maintenir la touche pour atteindre le freinage maximum.")]
        [Range(0.1f, 5f)]
        public float brakeBuildRate = 1.2f;

        [Tooltip("Taux de relâchement du freinage adaptatif par seconde quand la touche est lâchée. " +
                 "Doit être plus élevé que brakeBuildRate pour un retour plus rapide.")]
        [Range(0.5f, 20f)]
        public float brakeReleaseRate = 4f;

        #endregion

        #region Configuration — Oversteer

        [Header("=== SURVIRAGE ===")]
        [Tooltip("Seuil de déclenchement du survirage [0-1].\n" +
                 "Formule : freinage_adapt × intensité_virage × |charge_avant| × strength.\n" +
                 "En dessous de ce seuil : aucun effet.")]
        [Range(0f, 1f)]
        public float oversteerThreshold = 0.25f;

        [Tooltip("Multiplicateur global des effets de survirage (intensité du déclenchement).")]
        [Range(0f, 5f)]
        public float oversteerStrength = 1f;

        [Tooltip("Facteur de conversion intensité survirage → delta de vitesse angulaire de lacet.")]
        [Range(0f, 10f)]
        public float oversteerYawFactor = 3f;

        [Tooltip("Intensité de la dérive latérale de l'arrière lors du survirage.")]
        [Range(0f, 5f)]
        public float oversteerSlideStrength = 1f;

        [Header("=== TÊTE-À-QUEUE (survirage extrême) ===")]
        [Tooltip("Intensité de survirage au-delà de laquelle le tête-à-queue se déclenche [0-1].")]
        [Range(0f, 1f)]
        public float spinOutThreshold = 0.7f;

        [Tooltip("Couple angulaire auto-entretenu pendant le tête-à-queue. " +
                 "Plus élevé = rotation s'emballe plus vite.")]
        [Range(0f, 15f)]
        public float spinOutAngularMultiplier = 4f;

        [Tooltip("Vitesse angulaire maximale (rad/s) autorisée pendant le tête-à-queue.")]
        [Range(3f, 30f)]
        public float spinOutMaxAngularVelocity = 12f;

        #endregion

        #region Configuration — Understeer

        [Header("=== SOUS-VIRAGE ===")]
        [Tooltip("Seuil de déclenchement du sous-virage [0-1].\n" +
                 "Formule : accélération × intensité_virage × |charge_arrière| × strength.\n" +
                 "En dessous de ce seuil : aucun effet.")]
        [Range(0f, 1f)]
        public float understeerThreshold = 0.25f;

        [Tooltip("Multiplicateur global des effets de sous-virage.")]
        [Range(0f, 5f)]
        public float understeerStrength = 1f;

        [Tooltip("Facteur d'amortissement de la rotation de lacet en sous-virage.")]
        [Range(0f, 10f)]
        public float understeerYawDampFactor = 2f;

        [Header("=== DÉPORT EXTÉRIEUR (sous-virage extrême) ===")]
        [Tooltip("Intensité de sous-virage au-delà de laquelle la voiture est déportée " +
                 "vers l'extérieur du virage [0-1].")]
        [Range(0f, 1f)]
        public float understeerPushThreshold = 0.6f;

        [Tooltip("Force de déport vers l'extérieur du virage en sous-virage extrême.")]
        [Range(0f, 15f)]
        public float understeerPushStrength = 3f;

        #endregion

        #region Configuration — Debug

        [Header("=== DEBUG ===")]
        [Tooltip("Active le feedback couleur sur les roues et les gizmos dans la Scene view.")]
        [SerializeField] private bool showDebug = false;

        #endregion

        #region Private State

        // Voie par défaut (m) utilisée comme fallback quand moins de 2 roues sont assignées
        private const float DEFAULT_AXLE_WIDTH_M = 1.6f;

        // Voie mesurée depuis la scène × multiplicateur (mètres)
        private float _frontAxleWidth;
        private float _rearAxleWidth;

        // Point de charge sur chaque essieu [-1 = gauche, 0 = centre, +1 = droite]
        private float _frontLoadPoint;
        private float _rearLoadPoint;

        // Intensité de virage accumulée [0, 1]
        private float _turnIntensity;

        // Freinage adaptatif [0, 1]
        private float _adaptiveBrake;

        // Intensités calculées (exposées en lecture seule)
        private float _oversteerIntensity;
        private float _understeerIntensity;
        private float _spinOutIntensity;

        // Wheel color feedback
        private Renderer[] _frontWheelRenderers;
        private Renderer[] _rearWheelRenderers;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _colorId     = Shader.PropertyToID("_Color");
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private bool _frontColorApplied;
        private bool _rearColorApplied;

        #endregion

        #region Properties

        /// <summary>Voie avant (m) mesurée depuis la scène × multiplicateur.</summary>
        public float FrontAxleWidth => _frontAxleWidth;

        /// <summary>Voie arrière (m) mesurée depuis la scène × multiplicateur.</summary>
        public float RearAxleWidth => _rearAxleWidth;

        /// <summary>Position du point de charge sur l'essieu avant [-1 = gauche, 0 = centre, +1 = droite].</summary>
        public float FrontLoadPoint => _frontLoadPoint;

        /// <summary>Position du point de charge sur l'essieu arrière.</summary>
        public float RearLoadPoint => _rearLoadPoint;

        /// <summary>Intensité de virage accumulée [0-1]. Monte tant qu'on tourne, descend dès qu'on lâche.</summary>
        public float TurnIntensity => _turnIntensity;

        /// <summary>Intensité de freinage adaptatif [0-1]. Monte lentement, retombe rapidement.</summary>
        public float AdaptiveBrakeIntensity => _adaptiveBrake;

        /// <summary>Intensité du survirage normalisée [0-1].</summary>
        public float OversteerIntensity => _oversteerIntensity;

        /// <summary>Intensité du sous-virage normalisée [0-1].</summary>
        public float UndersteerIntensity => _understeerIntensity;

        /// <summary>Intensité du tête-à-queue normalisée [0-1].</summary>
        public float SpinOutIntensity => _spinOutIntensity;

        /// <summary>Vitesse angulaire max configurable pendant le tête-à-queue (rad/s).</summary>
        public float SpinOutMaxAngularVelocity => spinOutMaxAngularVelocity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _physics = GetComponent<VehiclePhysics>();
            MeasureAxleWidths();
            _propBlock = new MaterialPropertyBlock();
            _frontWheelRenderers = GatherRenderers(frontWheels);
            _rearWheelRenderers  = GatherRenderers(rearWheels);
        }

        private void OnDestroy()
        {
            ClearWheelColor(_frontWheelRenderers);
            ClearWheelColor(_rearWheelRenderers);
        }

        #endregion

        #region Axle Measurement

        private void MeasureAxleWidths()
        {
            _frontAxleWidth = MeasureAxle(frontWheels, DEFAULT_AXLE_WIDTH_M) * frontAxleEffectMultiplier;
            _rearAxleWidth  = MeasureAxle(rearWheels,  DEFAULT_AXLE_WIDTH_M) * rearAxleEffectMultiplier;
        }

        private static float MeasureAxle(Transform[] wheels, float fallback)
        {
            if (wheels != null && wheels.Length >= 2 && wheels[0] != null && wheels[1] != null)
                return Vector3.Distance(wheels[0].position, wheels[1].position);
            return fallback;
        }

        #endregion

        #region State Update (called by VehiclePhysics)

        /// <summary>
        /// Met à jour l'état interne (point de charge, intensité de virage, freinage adaptatif).
        /// Doit être appelé par VehiclePhysics avant ComputeCorneringCorrection chaque FixedUpdate.
        /// </summary>
        public void UpdateCorneringState(float steering, float throttle, float rawBrake,
                                         float speedKMH, float deltaTime)
        {
            UpdateAdaptiveBrake(rawBrake, deltaTime);
            UpdateTurnIntensity(steering, deltaTime);
            UpdateLoadPoints(steering, speedKMH, deltaTime);
        }

        private void UpdateAdaptiveBrake(float rawBrake, float dt)
        {
            if (rawBrake > 0.05f)
                _adaptiveBrake = Mathf.MoveTowards(_adaptiveBrake, rawBrake, brakeBuildRate * dt);
            else
                _adaptiveBrake = Mathf.MoveTowards(_adaptiveBrake, 0f, brakeReleaseRate * dt);
        }

        private void UpdateTurnIntensity(float steering, float dt)
        {
            float steerMag = Mathf.Abs(steering);
            if (steerMag > 0.05f)
                _turnIntensity = Mathf.Clamp01(_turnIntensity + turnIntensityBuildRate * steerMag * dt);
            else
                _turnIntensity = Mathf.Max(0f, _turnIntensity - turnIntensityDecayRate * dt);
        }

        private void UpdateLoadPoints(float steering, float speedKMH, float dt)
        {
            float speedFactor = Mathf.Clamp01(speedKMH / Mathf.Max(loadReferenceSpeedKMH, 1f));

            if (Mathf.Abs(steering) > 0.05f)
            {
                // La charge se déplace vers la roue EXTÉRIEURE (opposé à la direction du steering).
                // Virage droite (steering > 0) → charge part à GAUCHE (négatif dans repère local)
                float target = -steering * speedFactor;
                _frontLoadPoint = Mathf.MoveTowards(_frontLoadPoint, target, frontLoadSensitivity * dt);
                _rearLoadPoint  = Mathf.MoveTowards(_rearLoadPoint,  target, rearLoadSensitivity  * dt);
            }
            else
            {
                _frontLoadPoint = Mathf.MoveTowards(_frontLoadPoint, 0f, loadReturnSpeed * dt);
                _rearLoadPoint  = Mathf.MoveTowards(_rearLoadPoint,  0f, loadReturnSpeed * dt);
            }
        }

        #endregion

        #region Cornering Correction

        /// <summary>
        /// Calcule la correction de vélocité et le delta de vitesse angulaire issus du
        /// survirage / sous-virage.
        /// Doit être appelé après UpdateCorneringState dans le même FixedUpdate.
        /// </summary>
        /// <param name="velocity">Vélocité actuelle (espace monde, m/s)</param>
        /// <param name="vehicleTransform">Transform du véhicule</param>
        /// <param name="steering">Input de direction [-1, 1]</param>
        /// <param name="throttle">Input d'accélération [0, 1]</param>
        /// <param name="angularVelocity">Vitesse angulaire de lacet courante (rad/s)</param>
        /// <param name="deltaTime">Time.fixedDeltaTime</param>
        /// <param name="angularVelocityDelta">Sortie : delta à appliquer à la vitesse angulaire</param>
        /// <returns>Correction de vélocité en espace monde (Vector3)</returns>
        public Vector3 ComputeCorneringCorrection(
            Vector3 velocity,
            Transform vehicleTransform,
            float steering,
            float throttle,
            float angularVelocity,
            float deltaTime,
            out float angularVelocityDelta)
        {
            angularVelocityDelta = 0f;

            float forwardSpeed = Vector3.Dot(velocity, vehicleTransform.forward);
            if (Mathf.Abs(forwardSpeed) < 1f)
            {
                ClearIntensities();
                UpdateWheelColors();
                return Vector3.zero;
            }

            float speed      = Mathf.Abs(forwardSpeed);
            float steerSign  = Mathf.Abs(steering) > 0.01f ? Mathf.Sign(steering) : 0f;

            // Échelle d'effet basée sur la voie mesurée + multiplicateur
            float frontScale = _frontAxleWidth;
            float rearScale  = _rearAxleWidth;

            // ── SURVIRAGE ──────────────────────────────────────────────────────────────
            // Formule : freinage_adaptatif × intensité_virage × |déplacement_charge_avant| × strength
            float frontDisp     = Mathf.Abs(_frontLoadPoint);
            float oversteerRaw  = _adaptiveBrake * _turnIntensity * frontDisp * oversteerStrength;
            _oversteerIntensity = Mathf.Clamp01(
                Mathf.Max(0f, oversteerRaw - oversteerThreshold)
                / Mathf.Max(1f - oversteerThreshold, 0.001f));

            // ── SOUS-VIRAGE ────────────────────────────────────────────────────────────
            // Formule : accélération × intensité_virage × |déplacement_charge_arrière| × strength
            float rearDisp       = Mathf.Abs(_rearLoadPoint);
            float understeerRaw  = throttle * _turnIntensity * rearDisp * understeerStrength;
            _understeerIntensity = Mathf.Clamp01(
                Mathf.Max(0f, understeerRaw - understeerThreshold)
                / Mathf.Max(1f - understeerThreshold, 0.001f));

            // ── TÊTE-À-QUEUE ───────────────────────────────────────────────────────────
            _spinOutIntensity = Mathf.Clamp01(
                (_oversteerIntensity - spinOutThreshold)
                / Mathf.Max(1f - spinOutThreshold, 0.001f));

            Vector3 correction = Vector3.zero;

            // ── APPLICATION SURVIRAGE ──────────────────────────────────────────────────
            if (_oversteerIntensity > 0f && steerSign != 0f)
            {
                // L'arrière glisse vers l'extérieur du virage (opposé au sens du virage)
                // Virage droite (steerSign=+1) → arrière part à GAUCHE (−transform.right)
                float slideForce = -steerSign * _oversteerIntensity
                    * oversteerSlideStrength * speed * frontScale * deltaTime;
                correction += vehicleTransform.right * slideForce;

                // La rotation de lacet augmente dans le sens du virage
                angularVelocityDelta += steerSign * _oversteerIntensity
                    * oversteerYawFactor * frontScale * deltaTime;

                // Tête-à-queue : couple auto-entretenu si intensité dépasse le seuil critique
                if (_spinOutIntensity > 0f)
                {
                    angularVelocityDelta += steerSign * _spinOutIntensity
                        * spinOutAngularMultiplier * deltaTime;
                }
            }

            // ── APPLICATION SOUS-VIRAGE ────────────────────────────────────────────────
            if (_understeerIntensity > 0f)
            {
                // Atténuer la rotation (le volant perd son efficacité)
                angularVelocityDelta -= Mathf.Sign(angularVelocity)
                    * _understeerIntensity
                    * understeerYawDampFactor
                    * Mathf.Abs(angularVelocity)
                    * rearScale * deltaTime;

                // Déport extérieur en sous-virage extrême
                if (_understeerIntensity > understeerPushThreshold && Mathf.Abs(steering) > 0.05f)
                {
                    float push = (_understeerIntensity - understeerPushThreshold)
                        / Mathf.Max(1f - understeerPushThreshold, 0.001f);
                    float outForce = -Mathf.Sign(steering) * push
                        * understeerPushStrength * speed * rearScale * deltaTime;
                    correction += vehicleTransform.right * outForce;
                }
            }

            UpdateWheelColors();
            return correction;
        }

        private void ClearIntensities()
        {
            _oversteerIntensity  = 0f;
            _understeerIntensity = 0f;
            _spinOutIntensity    = 0f;
        }

        #endregion

        #region Wheel Color Feedback

        private Renderer[] GatherRenderers(Transform[] wheels)
        {
            if (wheels == null || wheels.Length == 0)
                return System.Array.Empty<Renderer>();
            var result = new Renderer[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                    result[i] = wheels[i].GetComponentInChildren<Renderer>();
            }
            return result;
        }

        private void ApplyWheelColor(Renderer[] renderers, Color color)
        {
            _propBlock.SetColor(_colorId,     color);
            _propBlock.SetColor(_baseColorId, color);
            foreach (Renderer r in renderers)
            {
                if (r != null) r.SetPropertyBlock(_propBlock);
            }
        }

        private void ClearWheelColor(Renderer[] renderers)
        {
            foreach (Renderer r in renderers)
            {
                if (r != null) r.SetPropertyBlock(null);
            }
        }

        private void UpdateWheelColors()
        {
            if (!showDebug)
            {
                if (_rearColorApplied)
                {
                    ClearWheelColor(_rearWheelRenderers);
                    _rearColorApplied = false;
                }
                if (_frontColorApplied)
                {
                    ClearWheelColor(_frontWheelRenderers);
                    _frontColorApplied = false;
                }
                return;
            }

            // Roues arrière : survirage (magenta → rouge en tête-à-queue)
            if (_oversteerIntensity > 0.05f)
            {
                Color c = Color.Lerp(Color.magenta, Color.red, _spinOutIntensity);
                ApplyWheelColor(_rearWheelRenderers, c);
                _rearColorApplied = true;
            }
            else if (_rearColorApplied)
            {
                ClearWheelColor(_rearWheelRenderers);
                _rearColorApplied = false;
            }

            // Roues avant : sous-virage (jaune → orange en déport)
            if (_understeerIntensity > 0.05f)
            {
                float pushProgress = Mathf.Clamp01(
                    (_understeerIntensity - understeerPushThreshold)
                    / Mathf.Max(1f - understeerPushThreshold, 0.001f));
                Color c = Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), pushProgress);
                ApplyWheelColor(_frontWheelRenderers, c);
                _frontColorApplied = true;
            }
            else if (_frontColorApplied)
            {
                ClearWheelColor(_frontWheelRenderers);
                _frontColorApplied = false;
            }
        }

        #endregion

        #region Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebug || !Application.isPlaying) return;

            DrawAxleGizmo(
                frontWheels,
                _frontLoadPoint,
                _frontAxleWidth,
                _oversteerIntensity > 0.05f ? Color.Lerp(Color.magenta, Color.red, _spinOutIntensity) : Color.white,
                "F");

            DrawAxleGizmo(
                rearWheels,
                _rearLoadPoint,
                _rearAxleWidth,
                _understeerIntensity > 0.05f
                    ? Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f),
                        Mathf.Clamp01((_understeerIntensity - understeerPushThreshold)
                            / Mathf.Max(1f - understeerPushThreshold, 0.001f)))
                    : Color.white,
                "R");

            // Afficher les valeurs d'état au-dessus du véhicule (via label)
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.5f,
                $"Oversteer: {_oversteerIntensity:F2}  Understeer: {_understeerIntensity:F2}\n" +
                $"TurnIntensity: {_turnIntensity:F2}  AdaptBrake: {_adaptiveBrake:F2}\n" +
                $"FrontLoad: {_frontLoadPoint:F2}  RearLoad: {_rearLoadPoint:F2}");
        }

        private void DrawAxleGizmo(Transform[] wheels, float loadPoint, float axleWidth, Color effectColor, string label)
        {
            Vector3 center, rightDir;

            if (wheels != null && wheels.Length >= 2 && wheels[0] != null && wheels[1] != null)
            {
                center   = (wheels[0].position + wheels[1].position) * 0.5f;
                rightDir = (wheels[1].position - wheels[0].position).normalized;
            }
            else
            {
                // Fallback : pas de roues assignées
                return;
            }

            float halfWidth = axleWidth * 0.5f;
            Vector3 leftEnd  = center - rightDir * halfWidth;
            Vector3 rightEnd = center + rightDir * halfWidth;

            // Ligne de l'essieu
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(leftEnd, rightEnd);

            // Point de charge (sphère se déplaçant sur la ligne)
            // loadPoint est dans [-1, 1] où 1 = pleinement déplacé vers la droite
            float rawAxleWidth = MeasureAxle(wheels, DEFAULT_AXLE_WIDTH_M);
            float halfRaw      = rawAxleWidth * 0.5f;
            Vector3 loadPos    = center + rightDir * (loadPoint * halfRaw);

            Gizmos.color = effectColor;
            Gizmos.DrawSphere(loadPos, 0.08f + 0.06f * Mathf.Abs(loadPoint));

            // Petite sphère blanche au centre de l'essieu (référence)
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(center, 0.05f);
        }
#endif

        #endregion
    }
}
