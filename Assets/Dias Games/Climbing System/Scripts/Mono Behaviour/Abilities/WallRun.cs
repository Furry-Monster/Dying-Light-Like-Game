using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DiasGames.Components;
using DiasGames.Climbing;

namespace DiasGames.Abilities
{
    public class WallRun : AbstractAbility
    {
        [SerializeField] private LayerMask wallRunMask;
        [SerializeField] private float wallRunSpeed = 5f;
        [SerializeField] private float smoothnessTime = 0.1f;
        [SerializeField] private float offsetFromWall = 0.3f;
        [Header("Animation")]
        [SerializeField] private string wallRunAnimState = "Wall Run";
        [SerializeField] private string mirrorBoolParameter = "Mirror";

        private IMover _mover;
        private ICapsule _capsule;
        private WallRunTrigger _wall;

        // smoothness positioning
        private Vector3 _startPos, _targetPos;
        private Quaternion _startRot, _targetRot;
        private float _weight;
        private float _step;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();
        }

        public override bool ReadyToRun()
        {
            return !_mover.IsGrounded() && _mover.GetVelocity().y > 1f && FoundWall();
        }

        public override void OnStartAbility()
        {
            _weight = 0;
            _step = 1f / smoothnessTime;
            _startPos = transform.position;
            _startRot = transform.rotation;

            // caluclate target position and rotation
            _targetPos = _wall.WallContact.position + _wall.WallContact.forward * offsetFromWall;
            _targetPos.y = transform.position.y;
            _targetRot = Quaternion.LookRotation(_wall.WallMoveDirection);

            // set vars
            _mover.DisableGravity();

            // play animation
            _animator.CrossFadeInFixedTime(wallRunAnimState, 0.1f);
            _animator.SetBool(mirrorBoolParameter, _wall.IsRightMove);
        }


        public override void UpdateAbility()
        {
            // mover char on wall
            _mover.Move(_wall.WallMoveDirection * wallRunSpeed);

            if(!Mathf.Approximately(_weight, 1f))
            {
                _weight = Mathf.MoveTowards(_weight, 1f, _step * Time.deltaTime);

                _mover.SetPosition(Vector3.Lerp(_startPos, _targetPos, _weight));
                transform.rotation = Quaternion.Lerp(_startRot, _targetRot, _weight);

                return;
            }

            // check to finish ability
            if (_animator.IsInTransition(0)) return;

            var state = _animator.GetCurrentAnimatorStateInfo(0);
            var normalizedTime = Mathf.Repeat(state.normalizedTime,1f);
            if (normalizedTime > 0.95f)
                StopAbility();

        }

        public override void OnStopAbility()
        {
            _mover.EnableGravity();
        }

        private bool FoundWall()
        {
            float radius = _capsule.GetCapsuleRadius();
            Vector3 p1 = transform.position + Vector3.up * radius;
            Vector3 p2 = transform.position + Vector3.up *(_capsule.GetCapsuleHeight() - radius);

            foreach(var coll in Physics.OverlapCapsule(p1,p2, radius, wallRunMask, QueryTriggerInteraction.Collide))
            {
                if (coll.TryGetComponent(out _wall))
                {
                    // is character moving through wall move direction?
                    if (Vector3.Dot(_wall.WallContact.forward, transform.forward) < 0.5f &&
                        Vector3.Dot(_wall.WallMoveDirection, transform.forward) > 0.1f)
                        return true;
                }
            }

            return false;
        }
    }
}