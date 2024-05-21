using System.Collections.Generic;
using UnityEngine;
using DiasGames.Components;
using DiasGames.IK;

namespace DiasGames.Abilities
{
    public class PushAbility : AbstractAbility
    {
        [SerializeField] private float speed = 2f;
        [SerializeField] private float positionSmoothnessTime = 0.12f;
        [Header("Animation")]
        [SerializeField] private string pushAnimationBlendState = "Push Blend";
        [SerializeField] private string horizontalFloatParam = "Horizontal";
        [SerializeField] private string verticalFloatParam = "Vertical";

        // components
        private IMover _mover;
        private IKScheduler _ikScheduler;
        private Transform _camera;

        // interaction components
        private IDraggable _draggable;
        private List<Collider> _triggeredObjs = new List<Collider>();

        // private internal vars
        // to positioning
        private bool _isMatchingTarget;
        private float _step;

        // animations ids
        private int _animHorizontalID;
        private int _animVerticalID;
        private int _animMotionSpeedID;

        private Vector3 _lastPosition;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _ikScheduler = GetComponent<IKScheduler>();

            _camera = Camera.main.transform;

            // assign animations ids
            _animHorizontalID = Animator.StringToHash(horizontalFloatParam);
            _animVerticalID = Animator.StringToHash(verticalFloatParam);
            _animMotionSpeedID = Animator.StringToHash("Motion Speed");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsAbilityRunning || Time.time - StopTime < 0.1f) return;

            _triggeredObjs.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsAbilityRunning || Time.time - StopTime < 0.1f) return;

            if (_triggeredObjs.Contains(other))
                _triggeredObjs.Remove(other);
        }

        private bool IsDraggable()
        {
            if (_triggeredObjs.Count == 0) return false;

            foreach (var trigger in _triggeredObjs)
            {
                if (trigger.TryGetComponent(out _draggable))
                    return true;
            }

            return false;
        }

        public override void OnStartAbility()
        {
            _draggable.StartDrag();
            _mover.StopMovement();
            SetAnimationState(pushAnimationBlendState);

            _step = Vector3.Distance(transform.position, _draggable.GetTarget().position) / positionSmoothnessTime;
            _isMatchingTarget = true;
        }

        public override bool ReadyToRun()
        {
            if (!_mover.IsGrounded() || Time.time - StopTime < 0.1f) return false;

            return _action.interact && IsDraggable();
        }

        public override void UpdateAbility()
        {
            HandleIK();
            UpdateTransform();

            if (_isMatchingTarget) return;

            if (_action.interact)
                StopAbility();

            Vector3 targetPos = _draggable.GetTarget().position;
            targetPos.y = transform.position.y;

            _mover.SetPosition(targetPos);
            transform.rotation = _draggable.GetTarget().rotation;

            // calculate input for realtive move
            Vector3 cameraFwd = Vector3.Scale(_camera.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 relativeMove = _action.move.x * _camera.transform.right + _action.move.y * cameraFwd;
            relativeMove.Normalize();

            // send move input to drag object
            _draggable.Move(relativeMove * speed);

            float currentSpeed = (_lastPosition - transform.position).magnitude / Time.deltaTime;
            bool hasMoved = currentSpeed > 0.1f;

            _animator.SetFloat(_animMotionSpeedID, hasMoved ? currentSpeed / speed : 1, 0.1f, Time.deltaTime);

            float hor = Vector3.Dot(transform.right, relativeMove);
            float ver = Vector3.Dot(transform.forward, relativeMove);

            // update animator
            _animator.SetFloat(_animHorizontalID, hor, 0.1f, Time.deltaTime);
            _animator.SetFloat(_animVerticalID, ver, 0.1f, Time.deltaTime);

            _lastPosition = transform.position;
        }

        public override void OnStopAbility()
        {
            _draggable.StopDrag();

            // reset vars
            _isMatchingTarget = false;

            // stop IK
            if (_ikScheduler != null)
            {
                _ikScheduler.StopIK(AvatarIKGoal.LeftHand);
                _ikScheduler.StopIK(AvatarIKGoal.RightHand);
            }
        }


        private void HandleIK()
        {
            if (_draggable != null && _ikScheduler != null)
            {
                // left hand
                Transform lhEffector = _draggable.GetLeftHandTarget();
                if (lhEffector != null)
                {
                    IKPass leftHandPass = new IKPass(lhEffector.position,
                        lhEffector.rotation,
                        AvatarIKGoal.LeftHand,
                        1, 1);

                    _ikScheduler.ApplyIK(leftHandPass);
                }

                // right hand
                Transform rhEffector = _draggable.GetRightHandTarget();
                if (rhEffector != null)
                {
                    IKPass rightHandPass = new IKPass(rhEffector.position,
                        rhEffector.rotation,
                        AvatarIKGoal.RightHand,
                        1, 1);

                    _ikScheduler.ApplyIK(rightHandPass);
                }

            }
        }

        private void UpdateTransform()
        {
            if (!_isMatchingTarget || _draggable == null) return;

            Vector3 targetPos = _draggable.GetTarget().position;
            targetPos.y = transform.position.y;

            _mover.SetPosition(Vector3.MoveTowards(transform.position, targetPos, _step * Time.deltaTime));
            transform.rotation = Quaternion.Lerp(transform.rotation, _draggable.GetTarget().rotation, positionSmoothnessTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.05f)
            {
                _isMatchingTarget = false;
                _mover.SetPosition(targetPos);
                transform.rotation = _draggable.GetTarget().rotation;
            }
        }
    }
}