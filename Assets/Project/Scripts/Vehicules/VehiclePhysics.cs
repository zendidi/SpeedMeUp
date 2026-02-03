using UnityEngine;
using ArcadeRacer.Settings;

namespace ArcadeRacer.Vehicle
{
    /// <summary>
    /// Gère toute la physique arcade du véhicule :  accélération, freinage, direction, drift. 
    /// Style Mario Kart avec un feeling fun et responsive. 
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehiclePhysics : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField, Tooltip("Statistiques du véhicule")]
        private VehicleStats stats;

        [SerializeField, Tooltip("Transform représentant l'avant du véhicule")]
        private Transform frontTransform;

        [Header("=== GROUND CHECK ===")]
        [SerializeField, Tooltip("Layers considérés comme du sol")]
        private LayerMask groundLayer = 1; // Default layer

        private VehicleController VehicleController => GetComponent<VehicleController>();
        private float baseAccelerationForce;

        [SerializeField, Tooltip("Multiplicateur de l'impact de la masse sur l'accélération")]
        private float massImpactMultiplier = 0.5f;

        [System.Serializable]
        public struct TorquePoint
        {
            public float speedRatio; // 0-1
            public float torque;
        }

        [Header("=== TORQUE CURVE ===")]
        [SerializeField]
        private TorquePoint[] torqueCurve = new TorquePoint[]
        {
            //new TorquePoint { speedRatio = 0.0f, torque = 0.6f },
            //new TorquePoint { speedRatio = 0.2f, torque = 0.9f },
            //new TorquePoint { speedRatio = 0.4f, torque = 1.2f },
            //new TorquePoint { speedRatio = 0.6f, torque = 0.8f },
            //new TorquePoint { speedRatio = 0.8f, torque = 0.5f },
            //new TorquePoint { speedRatio = 1.0f, torque = 0.3f },
        };

        [Header("=== DECELERATION MOMENTUM ===")]
        [SerializeField, Tooltip("Force de momentum résiduel après relâchement de l'accélérateur")]
        private float residualMomentumForce = 15f;

        [SerializeField, Tooltip("Vitesse de décroissance du momentum (plus élevé = décélération plus rapide)")]
        [Range(0.1f, 10f)]
        private float momentumDecayRate = 1.5f;

        [SerializeField, Tooltip("Résistance au roulement (friction constante à basse vitesse)")]
        private float rollingResistance = 0.8f;

        [SerializeField, Tooltip("Coefficient de résistance aérodynamique (proportionnel à v²)")]
        private float airDragCoefficient = 0.012f;

        [SerializeField, Tooltip("Vitesse (m/s) à partir de laquelle la résistance aérodynamique domine")]
        private float airDragThresholdMS = 15f;

        [SerializeField, Tooltip("Vitesse minimale (m/s) en dessous de laquelle on arrête complètement")]
        private float minStopSpeedMS = 0.5f;

        [Header("=== SPEED-BASED DECELERATION SCALING ===")]
        [SerializeField, Tooltip("Pourcentage de vitesse max où le scaling commence (0-1)")]
        [Range(0f, 1f)]
        private float decelerationScalingStartRatio = 0.5f;

        [SerializeField, Tooltip("Pourcentage de vitesse max où le scaling atteint son minimum (0-1)")]
        [Range(0f, 1f)]
        private float decelerationScalingMaxRatio = 0.9f;

        [SerializeField, Tooltip("Efficacité minimale de la décélération à haute vitesse (0-1)")]
        [Range(0f, 1f)]
        private float minDecelerationEfficiency = 0.1f;

        [SerializeField, Tooltip("Exposant de la courbe de scaling (1 = linéaire, 2 = quadratique, 3 = cubique)")]
        [Range(1f, 4f)]
        private float decelerationScalingPower = 2f;

        [Header("=== STEERING ===")]
        public float baseSteeringSpeed => stats.steeringSpeed;

        [SerializeField, Tooltip("Vitesse min (km/h) où le modificateur commence")]
        private float minSpeedThreshold = 60f;

        [SerializeField, Tooltip("Ratio de vitesse max où le modificateur est au max (0-1)")]
        [Range(0f, 1f)]
        private float maxSpeedThresholdRatio = 0.9f; // 90% de la vitesse max

        [SerializeField, Tooltip("Modificateur de steering à basse vitesse")]
        private float minSteeringModifier = 1f;

        [SerializeField, Tooltip("Modificateur de steering à haute vitesse")]
        private float maxSteeringModifier = 0.25f;

        [SerializeField, Tooltip("Type de courbe d'interpolation")]
        private AnimationCurve steeringCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool showDecelerationDebug = false;

        // Components
        private Rigidbody _rb;

        // État du véhicule
        private bool _isGrounded;
        private float _currentSpeed;
        private float _currentSteeringAngle;
        private bool _isDrifting;

        // Momentum system
        private Vector3 _lastAccelerationForce = Vector3.zero;
        private float _momentumTimer = 0f;
        private bool _wasAccelerating = false;

        // Input (fourni de l'extérieur)
        private float _throttleInput;
        private float _brakeInput;
        private float _steeringInput;
        private bool _driftInput;

        #region Properties

        /// <summary>
        /// Vitesse actuelle en km/h
        /// </summary>
        public float CurrentSpeedKMH => _currentSpeed * 3.6f;

        /// <summary>
        /// Vitesse actuelle en m/s
        /// </summary>
        public float CurrentSpeedMS => _currentSpeed;

        /// <summary>
        /// Le véhicule est au sol
        /// </summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>
        /// Le véhicule est en train de drifter
        /// </summary>
        public bool IsDrifting => _isDrifting;

        /// <summary>
        /// Angle de direction actuel
        /// </summary>
        public float CurrentSteeringAngle => _currentSteeringAngle;

        /// <summary>
        /// Pourcentage de vitesse (0-1)
        /// </summary>
        public float SpeedPercentage => Mathf.Clamp01(_currentSpeed / stats.MaxSpeedMS);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            ConfigureRigidbody();
        }

        private void FixedUpdate()
        {
            // FixedUpdate = physique
            CheckGroundStatus();

            if (_isGrounded)
            {
                ApplyAcceleration();
                ApplyBraking();
                ApplySteering();
                ApplyGrip();
                ApplyDownForce();
            }
            else
            {
                ApplyAirControl();
            }

            UpdateCurrentSpeed();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            _rb = GetComponent<Rigidbody>();
            baseAccelerationForce = stats.accelerationForce;
            if (stats == null)
            {
                Debug.LogError($"[VehiclePhysics] VehicleStats manquant sur {gameObject.name}!");
            }

            // Si pas de frontTransform, créer un point par défaut
            if (frontTransform == null)
            {
                GameObject frontPoint = new GameObject("FrontPoint");
                frontPoint.transform.parent = transform;
                frontPoint.transform.localPosition = Vector3.forward * 2f;
                frontTransform = frontPoint.transform;
                Debug.LogWarning($"[VehiclePhysics] Pas de FrontTransform assigné, création automatique.");
            }
        }

        private void ConfigureRigidbody()
        {
            _rb.mass = stats.mass;
            // On met le drag à 0 pour gérer nous-mêmes la décélération
            _rb.linearDamping = 0f;
            _rb.angularDamping = stats.angularDrag;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Centre de masse bas pour plus de stabilité
            _rb.centerOfMass = new Vector3(0, stats.centerOfMassY, 0);
        }

        #endregion

        #region Input (appelé depuis VehicleController)

        /// <summary>
        /// Définir les inputs du véhicule (appelé par VehicleController)
        /// </summary>
        public void SetInputs(float throttle, float brake, float steering, bool drift)
        {
            _throttleInput = Mathf.Clamp01(throttle);
            _brakeInput = Mathf.Clamp01(brake);
            _steeringInput = Mathf.Clamp(steering, -1f, 1f);
            _driftInput = drift && stats.allowDrift;

            // Logique de drift
            _isDrifting = _driftInput && _currentSpeed >= stats.MinDriftSpeedMS && _isGrounded;
        }

        #endregion

        #region Ground Check

        private void CheckGroundStatus()
        {
            // Raycast vers le bas depuis le centre du véhicule
            Ray ray = new Ray(transform.position, -transform.up);
            _isGrounded = Physics.Raycast(ray, stats.groundCheckDistance, groundLayer);

            // Debug
            if (showDebugGizmos)
            {
                Debug.DrawRay(transform.position, -transform.up * stats.groundCheckDistance,
                    _isGrounded ? Color.green : Color.red);
            }
        }

        #endregion

        #region Acceleration & Braking

        private void ApplyAcceleration()
        {
            if (_throttleInput > 0.1f)
            {
                // === MODE ACCÉLÉRATION ===
                _wasAccelerating = true;
                _momentumTimer = 0f;

                // Calculer le pourcentage de vitesse actuelle
                float speedPercentage = VehicleController.Physics.CurrentSpeedKMH / stats.maxSpeed;
                float torque = CalculateTorque(speedPercentage);

                // === RÉSISTANCE STATIQUE (inertie au démarrage) ===
                float staticFriction = speedPercentage < 0.1f ? 0.5f : 1f;

                // Impact de la masse
                float massRatio = 1500f / _rb.mass;
                float massFactor = Mathf.Lerp(1f, massRatio, massImpactMultiplier);

                // Force finale
                float finalForce = baseAccelerationForce * _throttleInput * torque * staticFriction;
                Vector3 accelerationForce = transform.forward * finalForce;

                // Stocker la dernière force d'accélération
                _lastAccelerationForce = accelerationForce;

                // Appliquer
                if (_currentSpeed < stats.MaxSpeedMS)
                {
                    _rb.AddForce(accelerationForce * Time.fixedDeltaTime, ForceMode.Acceleration);
                }
            }
            else
            {
                // === MODE DÉCÉLÉRATION NATURELLE ===
                ApplyNaturalDeceleration();
            }
        }

        private float CalculateTorque(float speedRatio)
        {
            // Cas limites
            if (speedRatio <= 0.15) return 1.0f;
            if (torqueCurve.Length == 0) return 1.0f;
            if (speedRatio <= torqueCurve[0].speedRatio) return torqueCurve[0].torque;
            if (speedRatio >= torqueCurve[torqueCurve.Length - 1].speedRatio)
                return torqueCurve[torqueCurve.Length - 1].torque;

            // Trouver l'intervalle
            for (int i = 0; i < torqueCurve.Length - 1; i++)
            {
                if (speedRatio >= torqueCurve[i].speedRatio && speedRatio <= torqueCurve[i + 1].speedRatio)
                {
                    float t = (speedRatio - torqueCurve[i].speedRatio) /
                              (torqueCurve[i + 1].speedRatio - torqueCurve[i].speedRatio);

                    return Mathf.Lerp(torqueCurve[i].torque, torqueCurve[i + 1].torque, t);
                }
            }

            return 1.0f; // Fallback
        }

        private void ApplyBraking()
        {
            if (_brakeInput > 0.1f)
            {
                // Freinage = force opposée à la vélocité
                Vector3 brakeForce = -_rb.linearVelocity.normalized * stats.brakeForce * _brakeInput;
                _rb.AddForce(brakeForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
        }

        private void ApplyNaturalDeceleration()
        {
            // === CALCULER LE SCALING BASÉ SUR LA VITESSE ===
            float speedPercentage = SpeedPercentage;
            float decelerationScaling = CalculateDecelerationScaling(speedPercentage);

            // === PHASE 1 : MOMENTUM RÉSIDUEL ===
            if (_wasAccelerating && _lastAccelerationForce.magnitude > 0.01f)
            {
                _momentumTimer += Time.fixedDeltaTime;

                // Calculer le facteur de décroissance
                float decayFactor = _momentumTimer;

                // Force de momentum qui diminue progressivement
                Vector3 momentumForce = _lastAccelerationForce * decayFactor * (residualMomentumForce / baseAccelerationForce);

                _rb.AddForce(momentumForce * Time.fixedDeltaTime * Time.fixedDeltaTime, ForceMode.Acceleration);

                if (showDecelerationDebug)
                {
                    Debug.Log($"[Momentum] Decay: {decayFactor:F3} | Force: {momentumForce.magnitude:F2}");
                }

         
            }

            //// === PHASE 2 : RÉSISTANCES COMBINÉES (SCALÉES) ===
            //// Friction au roulement
            //float rollingForce = rollingResistance;

            //// Résistance aérodynamique
            //float airResistance = 0f;
            //if (_currentSpeed > airDragThresholdMS)
            //{
            //    float speedOverThreshold = _currentSpeed - airDragThresholdMS;
            //    airResistance = airDragCoefficient * speedOverThreshold * speedOverThreshold;
            //}
            //else
            //{
            //    float speedRatio = _currentSpeed / airDragThresholdMS;
            //    airResistance = airDragCoefficient * speedRatio * _currentSpeed;
            //}

            //// Force totale de décélération AVANT scaling
            //float totalDecelerationForce = rollingForce + airResistance;

            //// === APPLIQUER LE SCALING BASÉ SUR LA VITESSE ===
            //totalDecelerationForce *= decelerationScaling;

            //// Appliquer la force opposée à la vélocité
            //if (_rb.linearVelocity.magnitude > 0.01f)
            //{
            //    Vector3 decelerationForce = -_rb.linearVelocity.normalized * totalDecelerationForce;
            //    _rb.AddForce(decelerationForce * Time.fixedDeltaTime, ForceMode.Acceleration);

            //    if (showDecelerationDebug)
            //    {
            //        Debug.Log($"[Deceleration] Speed: {CurrentSpeedKMH:F1} km/h ({speedPercentage:P0}) | Scaling: {decelerationScaling:P0} | Rolling: {rollingForce:F2} | Air: {airResistance:F2} | Total: {totalDecelerationForce:F2}");
            //    }
            //}

            // === ARRÊT COMPLET ===
            if (_currentSpeed < minStopSpeedMS)
            {
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
                _lastAccelerationForce = Vector3.zero;
                _momentumTimer = 0f;
            }
        }

        /// <summary>
        /// Calcule le facteur de scaling de la décélération basé sur la vitesse.
        /// Retourne 1.0 à basse vitesse, et minDecelerationEfficiency à haute vitesse.
        /// </summary>
        private float CalculateDecelerationScaling(float speedPercentage)
        {
            // En dessous du seuil de départ : décélération normale (100%)
            if (speedPercentage <= decelerationScalingStartRatio)
            {
                return 1.0f;
            }

            // Au-dessus du seuil max : décélération minimale
            if (speedPercentage >= decelerationScalingMaxRatio)
            {
                return minDecelerationEfficiency;
            }

            // === INTERPOLATION ENTRE LES DEUX SEUILS ===
            // Normaliser entre 0 et 1
            float t = (speedPercentage - decelerationScalingStartRatio) /
                      (decelerationScalingMaxRatio - decelerationScalingStartRatio);

            // Appliquer une courbe (linéaire, quadratique, cubique, etc.)
            float curvedT = Mathf.Pow(t, decelerationScalingPower);

            // Interpoler entre 100% (basse vitesse) et minEfficiency (haute vitesse)
            return Mathf.Lerp(1.0f, minDecelerationEfficiency, curvedT);
        }

        #endregion

        #region Steering

        private void ApplySteering()
        {
            if (Mathf.Abs(_steeringInput) > 0.01f)
            {
                // === CALCULER LE MODIFICATEUR DE STEERING ===
                float steeringModifier = CalculateSteeringModifier();

                // === APPLIQUER LA ROTATION ===
                float finalSteeringSpeed = baseSteeringSpeed * steeringModifier * Time.fixedDeltaTime;
                float rotationAmount = _steeringInput * finalSteeringSpeed;

                Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
                _rb.MoveRotation(_rb.rotation * deltaRotation);
            }
            else
            {
                // Retour au centre du volant
                _currentSteeringAngle = Mathf.Lerp(_currentSteeringAngle, 0f,
                    Time.fixedDeltaTime * stats.steeringReturnSpeed);
            }
        }

        private float CalculateSteeringModifier()
        {
            // Vitesse actuelle en km/h
            float currentSpeedKMH = _currentSpeed * 3.6f;

            // Vitesse max du véhicule (depuis stats)
            float maxSpeedKMH = stats.maxSpeed;

            // Calculer le seuil max basé sur le ratio
            float maxSpeedThreshold = maxSpeedKMH * maxSpeedThresholdRatio;

            // === CAS LIMITES ===
            if (currentSpeedKMH <= minSpeedThreshold)
            {
                return minSteeringModifier;
            }

            if (currentSpeedKMH >= maxSpeedThreshold)
            {
                return maxSteeringModifier;
            }

            // === INTERPOLATION LINÉAIRE ===
            float t = (currentSpeedKMH - minSpeedThreshold) / (maxSpeedThreshold - minSpeedThreshold);

            // Évaluer la courbe
            float curveValue = steeringCurve.Evaluate(t);

            return Mathf.Lerp(minSteeringModifier, maxSteeringModifier, curveValue);
        }

        private float CalculateSteeringSpeed()
        {
            float baseSteeringSpeed = stats.steeringSpeed;

            // Réduction de steering à haute vitesse
            if (_currentSpeed > stats.SteeringReductionStartSpeedMS)
            {
                float speedFactor = (_currentSpeed - stats.SteeringReductionStartSpeedMS) /
                                   (stats.MaxSpeedMS - stats.SteeringReductionStartSpeedMS);
                speedFactor = Mathf.Clamp01(speedFactor);

                baseSteeringSpeed *= (1f - speedFactor * stats.highSpeedSteeringReduction);
            }

            // Boost de steering pendant le drift
            if (_isDrifting)
            {
                baseSteeringSpeed *= stats.driftSteeringMultiplier;
            }

            return baseSteeringSpeed;
        }

        #endregion

        #region Grip & Drift

        private void ApplyGrip()
        {
            // Calculer la vélocité latérale (perpendiculaire à la direction)
            Vector3 forwardVelocity = transform.forward * Vector3.Dot(_rb.linearVelocity, transform.forward);
            Vector3 rightVelocity = transform.right * Vector3.Dot(_rb.linearVelocity, transform.right);

            // Force de grip (réduit le glissement latéral)
            float gripStrength = _isDrifting
                ? stats.gripStrength * stats.driftGripReduction
                : stats.gripStrength;

            // Appliquer une force opposée au glissement latéral
            Vector3 gripForce = -rightVelocity * gripStrength;
            _rb.AddForce(gripForce, ForceMode.Acceleration);

            // Pendant le drift, ajouter une force latérale
            if (_isDrifting && Mathf.Abs(_steeringInput) > 0.1f)
            {
                Vector3 driftForce = transform.right * _steeringInput * stats.driftForce;
                _rb.AddForce(driftForce, ForceMode.Acceleration);
            }
        }

        #endregion

        #region Down Force

        private void ApplyDownForce()
        {
            // Force vers le bas pour maintenir le véhicule au sol
            if (_currentSpeed > 5f)
            {
                Vector3 downForce = -transform.up * stats.downForce * SpeedPercentage;
                _rb.AddForce(downForce, ForceMode.Acceleration);
            }
        }

        #endregion

        #region Air Control

        private void ApplyAirControl()
        {
            // Auto-flip :  rotation automatique pour remettre la voiture à plat
            if (stats.autoFlip)
            {
                Vector3 targetUp = Vector3.up;
                Vector3 currentUp = transform.up;

                Quaternion targetRotation = Quaternion.FromToRotation(currentUp, targetUp) * _rb.rotation;

                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation,
                    Time.fixedDeltaTime * stats.autoFlipSpeed));
            }
        }

        #endregion

        #region Utility

        private void UpdateCurrentSpeed()
        {
            // Calculer la vitesse sur le plan horizontal (ignorer Y)
            Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _currentSpeed = horizontalVelocity.magnitude;
        }

        /// <summary>
        /// Réinitialiser la position et rotation du véhicule
        /// </summary>
        public void ResetVehicle(Vector3 position, Quaternion rotation)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = position;
            transform.rotation = rotation;
            _currentSteeringAngle = 0f;
            _lastAccelerationForce = Vector3.zero;
            _momentumTimer = 0f;
            _wasAccelerating = false;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || !Application.isPlaying) return;

            // Afficher le centre de masse
            if (_rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.TransformPoint(_rb.centerOfMass), 0.2f);
            }

            // Afficher la direction de vélocité
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, _rb.linearVelocity);

            // Afficher la direction forward
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);

            // Afficher le vecteur de momentum
            if (_wasAccelerating)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, _lastAccelerationForce.normalized * 5f);
            }
        }

        #endregion
    }
}