using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DiasGames.Components;
using DiasGames.Abilities;

namespace DiasGames.Climbing
{
    public class DropToClimb : AbstractAbility
    {
        [SerializeField] private bool holdWalkButtonToDrop = true;
        [Header("Cast Parameters")]
        [SerializeField] private LayerMask ClimbLayerMask;
        [SerializeField] private LayerMask ObstacleLayerMask;
        [SerializeField] private float SphereCastRadius = 0.1f;

        [Header("Position")]
        [Tooltip("The distance that character should be on edge to start drop.")]
        [SerializeField] private float DistanceOnLedgeToDrop = 0.5f;
        [SerializeField] private float positionSmoothTime = 0.1f;

        [Header("Animation")]
        [SerializeField] private string DropAnimationState = "Climb.Drop to Ledge Bellow";

        // components
        private IMover _mover = null;
        private ICapsule _capsule = null;
        private ClimbAbility _climb; // not ideal, but needs to check if it's possible to grab ledge,
                                                 // and for this, needs to know character position on ledge,
                                                 // and it is calculated by this ability

        // internal vars
        private RaycastHit _hit;
        private Vector3 _startPosition = Vector3.zero;
        private Vector3 _targetPosition = Vector3.zero;
        private Quaternion _startRotation = Quaternion.identity;
        private Quaternion _targetRotation = Quaternion.identity;
        private bool _setTransformOnStart = false;
        private float _positionStep;
        private float _weightPosition;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();
            _climb = GetComponent<ClimbAbility>();
        }


        public override bool ReadyToRun()
        {
            if (!_mover.IsGrounded()) return false;

            if (holdWalkButtonToDrop && !_action.walk) return false;

            if (_action.move != Vector2.zero && CanDrop())
                return true;

            return false;
        }

        public override void OnStartAbility()
        {
            // Set target rotation
            _targetRotation = _mover.GetRotationFromDirection(_hit.normal);

            // Set target position
            _targetPosition = _hit.point - _hit.normal * DistanceOnLedgeToDrop;
            _targetPosition.y = transform.position.y;

            // set animation to play
            SetAnimationState(DropAnimationState);

            // set mover conditions
            _mover.StopRootMotion();
            _capsule.DisableCollision();

            // set initial transform
            _startPosition = transform.position;
            _startRotation = transform.rotation;

            _setTransformOnStart = true;
            _weightPosition = 0;
            _positionStep = 1f / positionSmoothTime;
        }


        public override void UpdateAbility()
        {
            // set position and rotation
            if (_setTransformOnStart)
            {
                _weightPosition = Mathf.MoveTowards(_weightPosition, 1f, _positionStep * Time.deltaTime);
                transform.position = Vector3.Lerp(_startPosition, _targetPosition, _weightPosition);
                transform.rotation = Quaternion.Lerp(_startRotation, _targetRotation, _weightPosition);

                if(Mathf.Approximately(_weightPosition, 1))
                {
                    transform.position = _targetPosition;
                    transform.rotation = _targetRotation;
                    _setTransformOnStart = false;
                }

                return;
            }

            _mover.ApplyRootMotion(Vector3.one, true);

            // check if animation ended to finish this ability
            if (!_animator.IsInTransition(0))
            {
                float normalizedtime = Mathf.Repeat(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1);

                if (!_animator.isMatchingTarget)
                {
                    Vector3 targetPos = _climb.GetCharacterPositionOnLedge(_hit, _hit);
                    Quaternion targetRot = _climb.GetCharacterRotationOnLedge(_hit);
                    _animator.MatchTarget(targetPos, targetRot, AvatarTarget.Root, new MatchTargetWeightMask(Vector3.one, 1f), 0.4f, 0.95f);
                }

                if (normalizedtime > 0.95f)
                    StopAbility();
            }
        }

        public override void OnStopAbility()
        {
            // Reset mover condiitons
            _mover.StopRootMotion();
            _capsule.EnableCollision();
        }

        private bool CanDrop()
        {
            Vector3 moveDirection = _mover.GetRelativeInput(_action.move);
            Vector3 sphereCenter = transform.position + Vector3.down * SphereCastRadius + moveDirection;
            if(Physics.SphereCast(sphereCenter, SphereCastRadius, -moveDirection, out _hit, 1f,
                ClimbLayerMask, QueryTriggerInteraction.Collide))
            {
                // check if it's a ledge component
                if(_hit.collider.TryGetComponent(out Ledge ledge))
                {
                    Transform closest = ledge.GetClosestPoint(_hit.point);
                    if (closest)
                    {
                        if (Vector3.Dot(closest.forward, _hit.normal) > 0.3f)
                        {
                            _hit.point = closest.position;
                            _hit.normal = closest.forward;
                        }
                    }
                }

                if (_climb.PositionFreeToClimb(_hit, _hit))
                    return true;
            }

            return false;
        }
    }
}