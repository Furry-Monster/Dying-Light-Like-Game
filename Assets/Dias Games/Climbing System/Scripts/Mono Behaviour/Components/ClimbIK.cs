using UnityEngine;

namespace DiasGames.Climbing
{
    public class ClimbIK : MonoBehaviour
    {
        public enum TargetHandIK { OnLedge, OnJump };

        [SerializeField] private Vector3 handIKOffset = new Vector3(0.2f, 0, 0);
        [SerializeField] private Quaternion handIKRotation = Quaternion.identity;
        [Space]
        [SerializeField] private float footOffset = 0.4f;
        public bool applyIK = true;

        // components
        private Animator _animator;

        // parameters from climbing
        private LayerMask _climbMask;
        private LayerMask _footMask;
        private RaycastHit _currentHorizontalHit;

        // hand ik transform effectors
        private Transform _leftHandOnLedgeEffector;
        private Transform _rightHandOnLedgeEffector;
        private Transform _rightHandJumpEffector;
        private Transform _leftHandJumpEffector;
        private Transform _leftHandFinalIK;
        private Transform _rightHandFinalIK;
        private Transform _targetRHJump;
        private Transform _targetLHJump;
        private float _weightIK;
        private float _weightStep;
        private float _weightTarget;
        private float _rightHandIKWeight;
        private float _leftHandIKWeight;
        private float _rhStep;
        private float _lhStep;
        private float _rhTargetWeight;
        private float _lhTargetWeight;

        // foot ik transform effector
        private Transform _rightFootEffector;
        private Transform _leftFootEffector;
        private float _rightFootDelta;
        private float _leftFootDelta;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            CreateGameObjectEffectors();
        }

        private void CreateGameObjectEffectors()
        {
            Transform group = new GameObject("Climb IK Effectos").transform;
            group.parent = transform;
            group.localPosition = Vector3.zero;
            group.localRotation = Quaternion.identity;

            _leftHandOnLedgeEffector = new GameObject("Left Hand IK - Effector").transform;
            _rightHandOnLedgeEffector = new GameObject("Right Hand IK - Effector").transform;

            _leftHandOnLedgeEffector.parent = group;
            _rightHandOnLedgeEffector.parent = group;

            _leftHandJumpEffector = new GameObject("Left Hand IK For Climb Jump - Effector").transform;
            _rightHandJumpEffector = new GameObject("Right Hand IK For Climb Jump - Effector").transform;

            _leftHandJumpEffector.parent = group;
            _rightHandJumpEffector.parent = group;

            _targetLHJump = new GameObject("LH Jump IK Target").transform;
            _targetRHJump = new GameObject("RH Jump IK Target").transform;

            _leftHandFinalIK = new GameObject("Current Left Hand IK").transform;
            _rightHandFinalIK = new GameObject("Current Right Hand IK").transform;

            _leftHandFinalIK.parent = group;
            _rightHandFinalIK.parent = group;

            // Foot effector
            _rightFootEffector = new GameObject("Right Foot IK Effector").transform;
            _rightFootEffector.parent = group;

            _leftFootEffector = new GameObject("Left Foot IK Effector").transform;
            _leftFootEffector.parent = group;
        }

        public void UpdateIKReferences(LayerMask climbMask, LayerMask footMask,RaycastHit horizontalHit)
        {
            _climbMask = climbMask;
            _footMask = footMask;
            _currentHorizontalHit = horizontalHit;
        }

        public void RunIK()
        {
            SetRightHandIKTarget(TargetHandIK.OnLedge);
            SetLeftHandIKTarget(TargetHandIK.OnLedge);

            _weightTarget = 1;
            _weightStep = (1 -_weightIK) / 0.12f; 
        }
        public void StopIK()
        {
            _weightTarget = 0;
            _weightStep = _weightIK / 0.12f;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (layerIndex != 0 || !applyIK) return;

            if (!Mathf.Approximately(_weightIK, _weightTarget))
                _weightIK = Mathf.MoveTowards(_weightIK, _weightTarget, _weightStep * Time.deltaTime);

            ProccessHandIK();
            ProccessFootIK();
        }

        #region Hand IK methods

        private void ProccessHandIK()
        {
            ProccessHandIKEffectorOnLedge();
            ProccesJumpHandIK();

            // proccess final target
            // proccess smooth transition

            if (!Mathf.Approximately(_leftHandIKWeight, _lhTargetWeight))
                _leftHandIKWeight = Mathf.MoveTowards(_leftHandIKWeight, _lhTargetWeight, _lhStep * Time.deltaTime);

            if (!Mathf.Approximately(_rightHandIKWeight, _rhTargetWeight))
                _rightHandIKWeight = Mathf.MoveTowards(_rightHandIKWeight, _rhTargetWeight, _rhStep * Time.deltaTime);

            _leftHandFinalIK.position = Vector3.Lerp(_leftHandOnLedgeEffector.position, _leftHandJumpEffector.position, _leftHandIKWeight);
            _leftHandFinalIK.rotation = Quaternion.Lerp(_leftHandOnLedgeEffector.rotation, _leftHandJumpEffector.rotation, _leftHandIKWeight);

            _rightHandFinalIK.position = Vector3.Lerp(_rightHandOnLedgeEffector.position, _rightHandJumpEffector.position, _rightHandIKWeight);
            _rightHandFinalIK.rotation = Quaternion.Lerp(_rightHandOnLedgeEffector.rotation, _rightHandJumpEffector.rotation, _rightHandIKWeight);

            ApplyHandIK();
        }
        private void ApplyHandIK()
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _weightIK);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _weightIK);
            _animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandFinalIK.position);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandFinalIK.position);
        }
        private void ProccessHandIKEffectorOnLedge()
        {
            // first pass: adjust horizontal offset
            // get current desired position
            _leftHandOnLedgeEffector.position = _animator.GetIKPosition(AvatarIKGoal.LeftHand);
            _rightHandOnLedgeEffector.position = _animator.GetIKPosition(AvatarIKGoal.RightHand);

            _leftHandOnLedgeEffector.localPosition += new Vector3(handIKOffset.x * (1 - _animator.GetFloat("HandHorOffset")), 0, 0);
            _rightHandOnLedgeEffector.localPosition += new Vector3(-handIKOffset.x * (1 - _animator.GetFloat("HandHorOffset")), 0, 0);

            // set target rotation
            _leftHandOnLedgeEffector.localRotation = handIKRotation;
            _rightHandOnLedgeEffector.localRotation = handIKRotation;


            // cast to find top position
            // right hand
            bool rightHandHit = false;
            Vector3 rightTop = _rightHandOnLedgeEffector.position - transform.forward;
            float step = 1.5f / 20;
            for (int i = 0; i < 20; i++)
            {
                Vector3 startPoint = rightTop + Vector3.up + transform.forward * step * i;
                foreach (var hit in Physics.SphereCastAll(startPoint, 0.1f, Vector3.down, 1.5f, _climbMask, QueryTriggerInteraction.Collide))
                {
                    if (hit.collider == _currentHorizontalHit.collider &&
                        hit.normal.y > 0.75f)
                    {
                        rightTop = hit.point;
                        rightHandHit = true;
                        break;
                    }
                }

                if (rightHandHit) break;
            }

            // left hand
            bool leftHandHit = false;
            Vector3 leftTop = _leftHandOnLedgeEffector.position - transform.forward;
            for (int i = 0; i < 20; i++)
            {
                Vector3 startPoint = leftTop + Vector3.up + transform.forward * step * i;
                foreach (var hit in Physics.SphereCastAll(startPoint, 0.1f, Vector3.down, 1.5f, _climbMask, QueryTriggerInteraction.Collide))
                {
                    if (hit.collider == _currentHorizontalHit.collider &&
                        hit.normal.y > 0.75f)
                    {
                        leftTop = hit.point;
                        leftHandHit = true;
                        break;
                    }
                }

                if (leftHandHit) break;
            }
            /////

            float rhWeight = _animator.GetFloat("RightHand");
            float lhWeight = _animator.GetFloat("LeftHand");

            // fix forward position
            if (_currentHorizontalHit.collider != null)
            {
                // correct hand height pos
                float lhTargetY = Mathf.Lerp(_leftHandOnLedgeEffector.position.y, leftTop.y + handIKOffset.y, lhWeight);
                float rhTargetY = Mathf.Lerp(_rightHandOnLedgeEffector.position.y, rightTop.y + handIKOffset.y, rhWeight);

                _leftHandOnLedgeEffector.position = new Vector3(_leftHandOnLedgeEffector.position.x, lhTargetY, _leftHandOnLedgeEffector.position.z);
                _rightHandOnLedgeEffector.position = new Vector3(_rightHandOnLedgeEffector.position.x, rhTargetY, _rightHandOnLedgeEffector.position.z);

                Vector3 leftForward = transform.InverseTransformPoint(_currentHorizontalHit.point);
                Vector3 rightForward = transform.InverseTransformPoint(_currentHorizontalHit.point);

                float lhTargetZ = Mathf.Lerp(_leftHandOnLedgeEffector.localPosition.z, leftForward.z + handIKOffset.z, lhWeight);
                float rhTargetZ = Mathf.Lerp(_rightHandOnLedgeEffector.localPosition.z, rightForward.z + handIKOffset.z, rhWeight);

                if (rhTargetZ < 0.2f)
                    rhTargetZ = _rightHandOnLedgeEffector.localPosition.z;
                if (lhTargetZ < 0.2f)
                    lhTargetZ = _leftHandOnLedgeEffector.localPosition.z;

                _leftHandOnLedgeEffector.localPosition = new Vector3(_leftHandOnLedgeEffector.localPosition.x, _leftHandOnLedgeEffector.localPosition.y, lhTargetZ);
                _rightHandOnLedgeEffector.localPosition = new Vector3(_rightHandOnLedgeEffector.localPosition.x, _rightHandOnLedgeEffector.localPosition.y, rhTargetZ);
            }

        }
        private void ProccesJumpHandIK()
        {
            _leftHandJumpEffector.position = Vector3.Lerp(_animator.GetIKPosition(AvatarIKGoal.LeftHand), _targetLHJump.position, _animator.GetFloat("LeftHand"));
            _rightHandJumpEffector.position = Vector3.Lerp(_animator.GetIKPosition(AvatarIKGoal.RightHand), _targetRHJump.position, _animator.GetFloat("RightHand"));
        }
        public void SetRightHandIKTarget(TargetHandIK hand)
        {
            _rhTargetWeight = hand == TargetHandIK.OnLedge ? 0 : 1;
            _rhStep = Mathf.Abs(_rightHandIKWeight - _rhTargetWeight) / 0.1f;
        }
        public void SetLeftHandIKTarget(TargetHandIK hand)
        {
            _lhTargetWeight = hand == TargetHandIK.OnLedge ? 0 : 1;
            _lhStep = Mathf.Abs(_leftHandIKWeight - _lhTargetWeight) / 0.1f;
        }

        public void SetLeftHandJumpEffector(Vector3 position)
        {
            _targetLHJump.position = position;
            SetLeftHandIKTarget(TargetHandIK.OnJump);
        }
        public void SetRightHandJumpEffector(Vector3 position)
        {
            _targetRHJump.position = position;
            SetRightHandIKTarget(TargetHandIK.OnJump);
        }

        #endregion

        private void ProccessFootIK()
        {
            // right foot
            CastFootIK(AvatarIKGoal.RightFoot, ref _rightFootDelta);

            // left foot
            CastFootIK(AvatarIKGoal.LeftFoot, ref _leftFootDelta);

            float footWeight =  _animator.GetFloat("FootIK");
            float hangWeight = _animator.GetFloat("HangWeight");

            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, _weightIK * footWeight * (1 - hangWeight));
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, _weightIK * footWeight * (1 - hangWeight));

            _animator.SetIKPosition(AvatarIKGoal.RightFoot, _rightFootEffector.position);
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, _leftFootEffector.position);
        }

        private void CastFootIK(AvatarIKGoal foot, ref float footDelta)
        {
            // choose correct foot
            Transform effector = foot == AvatarIKGoal.RightFoot ? _rightFootEffector : _leftFootEffector;

            effector.position = _animator.GetIKPosition(foot); // get current foot position
            effector.localPosition = Vector3.Scale(effector.localPosition, new Vector3(1, 1, 0)); // set z local position to zero

            Vector3 startCast = effector.position;
            float delta = 0;
            if (Physics.SphereCast(startCast, 0.1f, transform.forward, out RaycastHit hit,
                1f, _footMask, QueryTriggerInteraction.Collide))
            {
                effector.position = hit.point;
                delta = effector.localPosition.z - footOffset;
            }

            footDelta = Mathf.Lerp(footDelta, delta, 0.1f);

            effector.position = _animator.GetIKPosition(foot);          // get current foot position
            effector.localPosition += new Vector3(0, 0, footDelta);     // add local offset
        }
    }
}