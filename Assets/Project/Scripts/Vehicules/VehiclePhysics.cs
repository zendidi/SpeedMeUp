using UnityEngine;
using ArcadeRacer.Settings;
using ArcadeRacer.Physics;

namespace ArcadeRacer.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehiclePhysics : MonoBehaviour
    {
        #region References

        [Header("=== REFERENCES ===")]
        [SerializeField] private VehicleStats stats;

        private Rigidbody _rigidbody;
        private Transform _transform;
        private VehicleController _controller;
        private OffroadDetector _offroadDetector;
        private VehicleCorneringPhysics _corneringPhysics;

        #endregion

        #region Scene Configuration

        [Header("=== COLLISION LAYERS ===")]
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private LayerMask vehicleLayer;

        [Header("=== GROUND CHECK ===")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform[] groundCheckPoints;

        [Header("=== ADVANCED PHYSICS ===")]
        [SerializeField] private VehiclePhysicsCore physicsCore = new VehiclePhysicsCore();

        [Header("=== FREINAGE ADAPTATIF ===")]
        [Tooltip("Taux de montée de l'intensité de freinage adaptatif par seconde. " +
                 "Le joueur doit maintenir la touche pour atteindre le freinage maximum.")]
        [Range(0.1f, 5f)]
        public float brakeBuildRate = 1.2f;

        [Tooltip("Taux de relâchement du freinage adaptatif par seconde quand la touche est lâchée. " +
                 "Doit être plus élevé que brakeBuildRate pour un retour plus rapide.")]
        [Range(0.5f, 20f)]
        public float brakeReleaseRate = 4f;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool _showDebug = false;

        #endregion

        #region State Variables

        private float _throttleInput;
        private float _brakeInput;
        private float _steeringInput;
        private bool _driftInput;

        public Vector3 _velocity;
        private bool _isGrounded;
        private Vector3 _groundNormal;
        public float _currentSpeed;
        private float _currentSteeringAngle;

        // Freinage adaptatif [0-1] — monte lentement, retombe plus vite
        private float _adaptiveBrake;

        #endregion

        #region Properties

        public float CurrentSpeedKMH => _currentSpeed * 3.6f;
        public float SpeedPercentage => Mathf.Clamp01(_currentSpeed / stats.MaxSpeedMS);
        public bool IsGrounded => _isGrounded;
        public bool IsDrifting => _driftInput && _isGrounded && _currentSpeed > stats.MinDriftSpeedMS;
        public float CurrentSteeringAngle => _currentSteeringAngle;
        public VehicleStats Stats => stats;
        public float baseSteeringSpeed => stats.steeringSpeed;

        // Inputs exposés pour les composants voisins (ex : VehicleCorneringPhysics)
        public float ThrottleInput  => _throttleInput;
        public float BrakeInput     => _brakeInput;
        public float SteeringInput  => _steeringInput;

        /// <summary>Intensité de freinage adaptatif [0-1] — destiné à la jauge UI.</summary>
        public float AdaptiveBrakeIntensity => _adaptiveBrake;

        public float OversteerIntensity  => _corneringPhysics != null ? _corneringPhysics.OversteerIntensity  : 0f;
        public float UndersteerIntensity => _corneringPhysics != null ? _corneringPhysics.UndersteerIntensity : 0f;
        public float SpinOutIntensity    => _corneringPhysics != null ? _corneringPhysics.SpinOutIntensity    : 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _transform = transform;
            _controller = GetComponent<VehicleController>();
            _offroadDetector = GetComponent<OffroadDetector>();
            _corneringPhysics = GetComponent<VehicleCorneringPhysics>();

            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.mass = stats.mass;

            _velocity = Vector3.zero;

           
            physicsCore.Initialize(stats.mass);
        }

        private void FixedUpdate()
        {
            // Sauvegarder la vélocité avant calculs
            Vector3 oldVelocity = _velocity;

            CheckGround();
            ApplyGravity();
            ApplyAcceleration();
            UpdateAdaptiveBrake();
            ApplySteering();
            ApplyBraking();
            ApplyDrag();

           
            Vector3 deltaV = _velocity - oldVelocity;
            float longitudinalAccel = Vector3.Dot(deltaV, _transform.forward) / Time.fixedDeltaTime;
            physicsCore.UpdateWeightTransfer(longitudinalAccel, stats.mass);

            MoveVehicle();
            UpdateState();
        }

        #endregion

        #region Input

        public void SetInputs(float throttle, float brake, float steering, bool drift)
        {
            _throttleInput = throttle;
            _brakeInput = brake;
            _steeringInput = steering;
            _driftInput = drift;
        }

        #endregion

        #region Ground Detection

        private void CheckGround()
        {
            _isGrounded = false;
            _groundNormal = Vector3.up;

            float checkDistance = stats.groundCheckDistance;

            if (groundCheckPoints == null || groundCheckPoints.Length == 0)
            {
                _isGrounded = UnityEngine.Physics.Raycast(_transform.position, Vector3.down, checkDistance, groundLayer);
                return;
            }

            int groundedCount = 0;
            Vector3 averageNormal = Vector3.zero;

            foreach (Transform point in groundCheckPoints)
            {
                if (point == null) continue;

                if (UnityEngine.Physics.Raycast(point.position, Vector3.down, out RaycastHit hit, checkDistance, groundLayer))
                {
                    groundedCount++;
                    averageNormal += hit.normal;

                    if (_showDebug)
                    {
                        Debug.DrawRay(point.position, Vector3.down * checkDistance, Color.green);
                    }
                }
                else if (_showDebug)
                {
                    Debug.DrawRay(point.position, Vector3.down * checkDistance, Color.red);
                }
            }

            if (groundedCount > 0)
            {
                _isGrounded = true;
                _groundNormal = (averageNormal / groundedCount).normalized;
            }
        }

        #endregion

        #region Physics - Acceleration

        private void ApplyAcceleration()
        {
            if (_throttleInput <= 0f || !_isGrounded) return;

            float speedRatio = Mathf.Clamp01(_currentSpeed / stats.MaxSpeedMS);
            float torque = CalculateTorque(speedRatio);

            float staticResistance = 1.0f;
            if (speedRatio < stats.staticFrictionThreshold)
            {
                staticResistance = stats.staticFrictionFactor;
            }

            float force = stats.accelerationForce * _throttleInput * torque * staticResistance;

           
            float gripMultiplier = physicsCore.GetGripMultiplier();
            force *= gripMultiplier;
            
           
            if (_offroadDetector != null)
            {
                force *= _offroadDetector.AccelerationMultiplier;
            }

            float acceleration = force / stats.mass;
            _velocity += _transform.forward * acceleration * Time.fixedDeltaTime;

            float forwardSpeed = Vector3.Dot(_velocity, _transform.forward);
            if (forwardSpeed > stats.MaxSpeedMS)
            {
                Vector3 lateralVelocity = Vector3.Project(_velocity, _transform.right);
                _velocity = _transform.forward * stats.MaxSpeedMS + lateralVelocity;
            }

            if (_showDebug && Time.frameCount % 30 == 0)
            {
                float offroadMult = _offroadDetector != null ? _offroadDetector.AccelerationMultiplier : 1f;
                Debug.Log($"[Accel] Speed: {speedRatio:P0} ({CurrentSpeedKMH:F0} km/h) | Torque: {torque:F2} | Grip: {gripMultiplier:F2} | Offroad: {offroadMult:F2}");
            }
        }

        private float CalculateTorque(float speedRatio)
        {
            if (speedRatio <= stats.staticFrictionThreshold)
            {
                return stats.torqueCurve.Length > 0 ? stats.torqueCurve[0].torque : 1.0f;
            }

            if (stats.torqueCurve == null || stats.torqueCurve.Length == 0)
            {
                return 1.0f;
            }

            if (speedRatio <= stats.torqueCurve[0].speedRatio)
            {
                return stats.torqueCurve[0].torque;
            }

            if (speedRatio >= stats.torqueCurve[stats.torqueCurve.Length - 1].speedRatio)
            {
                return stats.torqueCurve[stats.torqueCurve.Length - 1].torque;
            }

            for (int i = 0; i < stats.torqueCurve.Length - 1; i++)
            {
                if (speedRatio >= stats.torqueCurve[i].speedRatio && speedRatio <= stats.torqueCurve[i + 1].speedRatio)
                {
                    float t = (speedRatio - stats.torqueCurve[i].speedRatio) /
                              (stats.torqueCurve[i + 1].speedRatio - stats.torqueCurve[i].speedRatio);

                    return Mathf.Lerp(stats.torqueCurve[i].torque, stats.torqueCurve[i + 1].torque, t);
                }
            }

            return 1.0f;
        }

        #endregion

        #region Physics - Steering

        private void ApplySteering()
        {
            if (!_isGrounded) return;

           
            float steeringModifier = CalculateSteeringModifier();
            physicsCore.UpdateAngularVelocity(_steeringInput, stats.steeringSpeed * steeringModifier, Time.fixedDeltaTime);
            physicsCore.ApplyAngularInertia(_transform, Time.fixedDeltaTime);

            // Angle pour visuel/audio
            if (Mathf.Abs(_steeringInput) > 0.01f)
            {
                _currentSteeringAngle = physicsCore.AngularVelocity * Mathf.Rad2Deg;
            }
            else
            {
                _currentSteeringAngle = Mathf.Lerp(_currentSteeringAngle, 0f, Time.fixedDeltaTime * stats.steeringReturnSpeed);
            }

            // Mettre à jour l'état du nouveau système de virage avant d'appliquer les corrections
            _corneringPhysics?.UpdateCorneringState(
                _steeringInput, _throttleInput, CurrentSpeedKMH, Time.fixedDeltaTime);

            ApplyGripOrDrift();
            ApplyTurningSpeedLoss();
        }

        private float CalculateSteeringModifier()
        {
            float speedRatio = Mathf.Clamp01(_currentSpeed / stats.MaxSpeedMS);
            return Mathf.Lerp(stats.lowSpeedSteeringMultiplier, stats.highSpeedSteeringMultiplier, speedRatio);
        }

        private void ApplyTurningSpeedLoss()
        {
            if (Mathf.Abs(_steeringInput) < 0.01f) return;

            float steeringIntensity = Mathf.Abs(_steeringInput);
            float speedRatio = Mathf.Clamp01(_currentSpeed / stats.MaxSpeedMS);
            float dragFromSpeed = speedRatio * stats.speedDragMultiplier;
            float totalDrag = stats.turningSpeedLoss * steeringIntensity * dragFromSpeed;

            Vector3 forwardVelocity = _transform.forward * Vector3.Dot(_velocity, _transform.forward);
            Vector3 sidewaysVelocity = _transform.right * Vector3.Dot(_velocity, _transform.right);

            forwardVelocity *= (1f - totalDrag * Time.fixedDeltaTime);
            _velocity = forwardVelocity + sidewaysVelocity;

            if (_showDebug && Time.frameCount % 30 == 0 && totalDrag > 0.01f)
            {
                Debug.Log($"[Turning Loss] Drag: {totalDrag:F3}");
            }
        }

        private void ApplyGripOrDrift()
        {
            Vector3 forwardVelocity = _transform.forward * Vector3.Dot(_velocity, _transform.forward);
            Vector3 sidewaysVelocity = _transform.right * Vector3.Dot(_velocity, _transform.right);

            if (_driftInput && stats.allowDrift && _currentSpeed > stats.MinDriftSpeedMS)
            {
                _velocity = forwardVelocity + sidewaysVelocity * stats.driftGripReduction;

                if (_steeringInput != 0f)
                {
                    _velocity += _transform.right * _steeringInput * stats.driftForce * Time.fixedDeltaTime;
                }
            }
            else
            {
                float gripFactor = Mathf.Clamp01(stats.gripStrength * 0.15f);
                _velocity = forwardVelocity + sidewaysVelocity * (1f - gripFactor);

                if (_isGrounded && _currentSpeed > 1f && _corneringPhysics != null)
                {
                    Vector3 velCorr = _corneringPhysics.ComputeCorneringCorrection(
                        _velocity, _transform,
                        _steeringInput, _throttleInput,
                        physicsCore.AngularVelocity,
                        Time.fixedDeltaTime,
                        out float angularDelta);

                    _velocity += velCorr;
                    physicsCore.ApplyExternalAngularDelta(
                        angularDelta,
                        _corneringPhysics.SpinOutIntensity,
                        _corneringPhysics.SpinOutMaxAngularVelocity);
                }
            }

            if (_showDebug && Time.frameCount % 30 == 0 && _corneringPhysics != null)
            {
                float over  = _corneringPhysics.OversteerIntensity;
                float under = _corneringPhysics.UndersteerIntensity;
                if (over > 0.1f || under > 0.1f)
                {
                    Debug.Log($"[Cornering] Oversteer: {over:F2} | Understeer: {under:F2} | " +
                              $"SpinOut: {_corneringPhysics.SpinOutIntensity:F2} | " +
                              $"TurnIntensity: {_corneringPhysics.TurnIntensity:F2} | " +
                              $"AdaptBrake: {_adaptiveBrake:F2} | " +
                              $"FrontLoad: {_corneringPhysics.FrontLoadPoint:F2} | " +
                              $"RearLoad: {_corneringPhysics.RearLoadPoint:F2}");
                }
            }
        }

        #endregion

        #region Physics - Braking & Drag

        private void UpdateAdaptiveBrake()
        {
            if (_brakeInput > 0.05f)
                _adaptiveBrake = Mathf.MoveTowards(_adaptiveBrake, _brakeInput, brakeBuildRate * Time.fixedDeltaTime);
            else
                _adaptiveBrake = Mathf.MoveTowards(_adaptiveBrake, 0f, brakeReleaseRate * Time.fixedDeltaTime);
        }

        private void ApplyBraking()
        {
            if (_brakeInput <= 0f || !_isGrounded) return;

            if (_adaptiveBrake < 0.001f) return;

            float brakeEfficiency = CalculateBrakeEfficiency();
            float brakeForce = stats.brakeForce * _adaptiveBrake * brakeEfficiency;
            float brakeDeceleration = brakeForce / stats.mass;

            Vector3 forwardVelocity = _transform.forward * Vector3.Dot(_velocity, _transform.forward);
            Vector3 sidewaysVelocity = _transform.right * Vector3.Dot(_velocity, _transform.right);

            float currentForwardSpeed = forwardVelocity.magnitude;
            float newForwardSpeed = Mathf.Max(0f, currentForwardSpeed - brakeDeceleration * Time.fixedDeltaTime);

            Vector3 forwardDirection = forwardVelocity.normalized;
            if (currentForwardSpeed > 0.01f)
            {
                _velocity = forwardDirection * newForwardSpeed + sidewaysVelocity;
            }
            else
            {
                _velocity = sidewaysVelocity;
            }

            if (_showDebug && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[Brake] Force: {brakeForce:F0}N | Decel: {brakeDeceleration:F1}m/s² | Effective: {_adaptiveBrake:F2} | Raw: {_brakeInput:F2}");
            }
        }

        private float CalculateBrakeEfficiency()
        {
            float currentSpeedKMH = _currentSpeed * 3.6f;

            if (currentSpeedKMH <= stats.brakeMinSpeedThreshold)
            {
                return stats.brakeLowSpeedEfficiency;
            }

            if (currentSpeedKMH >= stats.brakeMaxSpeedThreshold)
            {
                return stats.brakeHighSpeedEfficiency;
            }

            float t = (currentSpeedKMH - stats.brakeMinSpeedThreshold) /
                      (stats.brakeMaxSpeedThreshold - stats.brakeMinSpeedThreshold);

            return Mathf.Lerp(stats.brakeLowSpeedEfficiency, stats.brakeHighSpeedEfficiency, t);
        }

        private void ApplyDrag()
        {
            // Calculate base drag
            float baseDrag = stats.drag;
            
            // Add offroad drag if applicable
            if (_offroadDetector != null)
            {
                baseDrag += _offroadDetector.AdditionalDrag;
            }
            
            if (!_isGrounded)
            {
                float airDrag = baseDrag * 0.5f;
                _velocity *= (1f - airDrag * Time.fixedDeltaTime);
            }
            else
            {
               
                if (_throttleInput < 0.1f && _brakeInput < 0.1f)
                {
                    _velocity = physicsCore.ApplyCoastDown(_velocity, _transform, stats.mass, Time.fixedDeltaTime);
                }
                else
                {
                    // Drag normal pendant accélération
                    float groundDragCoefficient = stats.naturalDecelerationInverted;
                    float speedRatio = Mathf.Clamp01(_currentSpeed / stats.MaxSpeedMS);
                    float dragMultiplier = Mathf.Lerp(0.1f, 0.5f, speedRatio);
                    float dragFactor = (1f / (groundDragCoefficient * 5f)) * dragMultiplier;
                    
                    // Apply additional offroad drag
                    dragFactor += baseDrag * 0.1f;
                    
                    _velocity *= (1f - dragFactor * Time.fixedDeltaTime);
                }
            }
        }

        private void ApplyGravity()
        {
            if (!_isGrounded)
            {
                _velocity += UnityEngine.Physics.gravity * Time.fixedDeltaTime;
            }
            else
            {
                Vector3 groundPlaneVelocity = Vector3.ProjectOnPlane(_velocity, _groundNormal);
                _velocity = groundPlaneVelocity;

                if (_currentSpeed > 5f)
                {
                    _velocity += -_groundNormal * stats.downForce * 0.01f * Time.fixedDeltaTime;
                }
            }
        }

        #endregion

        #region Movement

        private void MoveVehicle()
        {
            Vector3 movement = _velocity * Time.fixedDeltaTime;
            _transform.position += movement;

            DetectCollisions(movement);
        }

        private void DetectCollisions(Vector3 movement)
        {
            float radius = 0.5f;
            float distance = movement.magnitude + 0.1f;

            if (distance < 0.01f) return;

            if (UnityEngine.Physics.SphereCast(_transform.position, radius, movement.normalized, out RaycastHit hit, distance))
            {
                if (IsInLayerMask(hit.collider.gameObject.layer, wallLayer))
                {
                    HandleWallCollision(hit);
                }
                else if (IsInLayerMask(hit.collider.gameObject.layer, vehicleLayer))
                {
                    HandleVehicleCollision(hit);
                }
            }
        }

        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return layerMask == (layerMask | (1 << layer));
        }

        #endregion

        #region Collision Handling

        private void HandleWallCollision(RaycastHit hit)
        {
            Vector3 normal = hit.normal;
            Vector3 reflection = Vector3.Reflect(_velocity, normal);

            _velocity = reflection * stats.wallBounceMultiplier;
            _transform.position = hit.point + normal * 0.1f;

            if (_showDebug)
            {
                Debug.Log($"[Wall Collision] Bounce! Speed: {_currentSpeed:F1} m/s");
                Debug.DrawRay(hit.point, normal, Color.red, 0.5f);
            }
        }

        private void HandleVehicleCollision(RaycastHit hit)
        {
            VehiclePhysics otherVehicle = hit.collider.GetComponentInParent<VehiclePhysics>();
            if (otherVehicle == null) return;

            float m1 = stats.mass;
            float m2 = otherVehicle.stats.mass;

            Vector3 v1 = _velocity;
            Vector3 v2 = otherVehicle._velocity;

            Vector3 v1New = ((m1 - m2) * v1 + 2 * m2 * v2) / (m1 + m2);
            Vector3 v2New = ((m2 - m1) * v2 + 2 * m1 * v1) / (m1 + m2);

            _velocity = v1New * stats.vehicleBounceFactor;
            otherVehicle._velocity = v2New * stats.vehicleBounceFactor;

            Vector3 separationDir = (_transform.position - otherVehicle._transform.position).normalized;
            _transform.position += separationDir * 0.2f;

            if (_showDebug)
            {
                Debug.Log($"[Vehicle Collision] Hit {otherVehicle.name}");
            }
        }

        #endregion

        #region Collision Events

        private void OnCollisionEnter(Collision collision)
        {
            float impactForce = collision.relativeVelocity.magnitude;

            if (_showDebug)
            {
                Debug.Log($"[Collision Event] Impact: {impactForce:F1}");
            }
        }

        #endregion

        #region State

        private void UpdateState()
        {
            _currentSpeed = _velocity.magnitude;
        }

        public void ResetVehicle(Vector3 position, Quaternion rotation)
        {
            _transform.position = position;
            _transform.rotation = rotation;
            _velocity = Vector3.zero;
            _currentSpeed = 0f;
            physicsCore.ResetAngularVelocity();
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // VehicleCorneringPhysics gère ses propres gizmos de virage
            if (_corneringPhysics != null) return;
            if (!_showDebug || !Application.isPlaying) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, _velocity);

            if (_isGrounded)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, _groundNormal * 2f);
            }
        }
#endif

        #endregion
    }
}