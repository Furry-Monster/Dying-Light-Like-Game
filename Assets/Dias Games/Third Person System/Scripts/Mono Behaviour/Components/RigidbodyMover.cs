using UnityEngine;

namespace DiasGames.Components
{
    public class RigidbodyMover : MonoBehaviour, IMover, ICapsule
    {
        [Header("Player")]
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Distance that should cast for ground")]
        public float GroundedCheckDistance = 0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Gravity")]
        [Tooltip("Changes the engine default value at awake")]
        [SerializeField] private float Gravity = -15.0f;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _initialCapsuleHeight = 2f;
        private float _initialCapsuleRadius = 0.28f;

        // variables for root motion
        private bool _useRootMotion = false;
        private Vector3 _rootMotionMultiplier = Vector3.one;
        private bool _useRotationRootMotion = false;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private Rigidbody _rigidbody;
        private CapsuleCollider _capsule;
        private GameObject _mainCamera;

        private bool _hasAnimator;

        private void Awake()
        {
            _mainCamera = Camera.main.gameObject;
            _rigidbody = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();

            _initialCapsuleHeight = _capsule.height;
            _initialCapsuleRadius = _capsule.radius;

            Physics.gravity = new Vector3(0, Gravity, 0);
        }

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            AssignAnimationIDs();
        }

        private void FixedUpdate()
        {
            GroundedCheck();
            GravityControl();
        }

        private void OnAnimatorMove()
        {
            if (!_useRootMotion) return;

            // TODO: multiply by multiplier
            Vector3 velocity = _animator.deltaPosition / Time.deltaTime;

            _rigidbody.velocity = velocity;
            transform.rotation *= _animator.deltaRotation;
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDMotionSpeed = Animator.StringToHash("Motion Speed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = transform.position + Vector3.up * GroundedRadius * 2;
            RaycastHit groundHit;
            Grounded = Physics.SphereCast(spherePosition, GroundedRadius, Vector3.down, out groundHit, 
                GroundedCheckDistance + GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        public Collider GetGroundCollider()
        {
            if (!Grounded) return null;

            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedCheckDistance, transform.position.z);
            Collider[] grounds = Physics.OverlapSphere(spherePosition, _capsule.radius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (grounds.Length > 0)
                return grounds[0];

            return null;
        }

        public void Move(Vector2 moveInput, float targetSpeed, bool rotateCharacter = true)
        {
            Move(moveInput, targetSpeed, _mainCamera.transform.rotation, rotateCharacter);
        }

        public void Move(Vector2 moveInput, float targetSpeed, Quaternion cameraRotation, bool rotateCharacter = true)
        {
            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (moveInput == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_rigidbody.velocity.x, 0.0f, _rigidbody.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = moveInput.magnitude; // _input.analogMovement ? _input.move.magnitude : 1f;

            if (inputMagnitude > 1)
                inputMagnitude = 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

            // normalise input direction
            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraRotation.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                // rotate to face input direction relative to camera position
                if (rotateCharacter && !_useRotationRootMotion)
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // don't apply movement if it's using root motion
            if (!_useRootMotion)
            {
                Vector3 velocity = targetDirection.normalized * _speed;
                velocity.y = _rigidbody.velocity.y;
                _rigidbody.velocity = velocity;
            }
        }

        public void Move(Vector3 velocity)
        {
            if(_rigidbody.useGravity)
                velocity.y = _rigidbody.velocity.y;

            _rigidbody.velocity = velocity;
        }

        private void GravityControl()
        {
            if (_rigidbody.useGravity)
            {
                if (Grounded)
                {
                    // stop our velocity dropping infinitely when grounded
                    if (_rigidbody.velocity.y < 0.0f)
                    {
                        Vector3 velocity = _rigidbody.velocity;
                        velocity.y = Mathf.Clamp(velocity.y, -2, 0); // Avoid character goes up

                        _rigidbody.velocity = velocity;
                    }
                }
            }
        }

        /// <summary>
		/// Get rotation to face desired direction
		/// </summary>
		/// <returns></returns>
		public Quaternion GetRotationFromDirection(Vector3 direction)
        {
            float yaw = Mathf.Atan2(direction.x, direction.z);
            return Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0);
        }

        /// <summary>
        /// Sets character new position
        /// </summary>
        /// <param name="newPosition"></param>
        public void SetPosition(Vector3 newPosition)
        {
            _rigidbody.position = newPosition + _rigidbody.velocity * Time.fixedDeltaTime;
        }

        public void DisableCollision()
        {
            _capsule.enabled = false;
        }

        public void EnableCollision()
        {
            _capsule.enabled = true;
        }

        public void SetCapsuleSize(float newHeight, float newRadius)
        {
            _capsule.height = newHeight;
            _capsule.center = new Vector3(0, newHeight * 0.5f, 0);

            if (newRadius > newHeight * 0.5f)
                newRadius = newHeight * 0.5f;

            _capsule.radius = newRadius;
        }

        public void ResetCapsuleSize()
        {
            SetCapsuleSize(_initialCapsuleHeight, _initialCapsuleRadius);
        }

        public void SetVelocity(Vector3 velocity)
        {
            _rigidbody.velocity = velocity;
        }

        public Vector3 GetVelocity()
        {
            return _rigidbody.velocity;
        }

        public float GetGravity()
        {
            return Gravity;
        }

        public void ApplyRootMotion(Vector3 multiplier, bool applyRotation = false)
        {
            _useRootMotion = true;
            _rootMotionMultiplier = multiplier;
            _useRotationRootMotion = applyRotation;
        }

        public void StopRootMotion()
        {
            _useRootMotion = false;
            _useRotationRootMotion = false;
        }

        public float GetCapsuleHeight()
        {
            return _capsule.height;
        }

        public float GetCapsuleRadius()
        {
            return _capsule.radius;
        }

        public void EnableGravity()
        {
           _rigidbody.useGravity = true;
        }

        public void DisableGravity()
        {
            _rigidbody.useGravity = false;
        }

        bool IMover.IsGrounded()
        {
            return Grounded;
        }
        public void StopMovement()
        {
            _rigidbody.velocity = Vector3.zero;
            _speed = 0;

            _animator.SetFloat(_animIDSpeed, 0);
            _animator.SetFloat(_animIDMotionSpeed, 0);
        }

        public Vector3 GetRelativeInput(Vector2 input)
        {
            Vector3 relative = _mainCamera.transform.right * input.x +
                   Vector3.Scale(_mainCamera.transform.forward, new Vector3(1, 0, 1)) * input.y;

            return relative;
        }
    }
}