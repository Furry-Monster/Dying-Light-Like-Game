using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DiasGames.Components;

namespace DiasGames.Abilities
{
    [DisallowMultipleComponent]
    public class Crouch : AbstractAbility
    {
        private IMover _mover = null;
        private ICapsule _capsule = null;

        [SerializeField] private LayerMask obstaclesMask;
        [SerializeField] private float capsuleHeightOnCrouch = 1f;
        [SerializeField] private float speed = 3f;

        private float _defaultCapsuleHeight = 0;
        private float _defaultCapsuleRadius = 0;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();

            _defaultCapsuleRadius = _capsule.GetCapsuleRadius();
            _defaultCapsuleHeight = _capsule.GetCapsuleHeight();
        }

        public override bool ReadyToRun()
        {
            if (ForceCrouchByHeight())
                return true;

            return _mover.IsGrounded() && _action.crouch;
        }

        public override void OnStartAbility()
        {
            _capsule.SetCapsuleSize(capsuleHeightOnCrouch, _capsule.GetCapsuleRadius());
            _mover.Move(new Vector3(0, 0.5f, 0));
            SetAnimationState("Crouch", 0.25f);
        }


        public override void UpdateAbility()
        {
            _mover.Move(_action.move, speed);

            if (!_action.crouch && !ForceCrouchByHeight())
                StopAbility();
        }

        public override void OnStopAbility()
        {
            _capsule.ResetCapsuleSize();
        }

        private bool ForceCrouchByHeight()
        {
            RaycastHit hit;

            if(Physics.SphereCast(transform.position, _defaultCapsuleRadius, Vector3.up, out hit, 
                _defaultCapsuleHeight, obstaclesMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.point.y - transform.position.y > capsuleHeightOnCrouch)
                    return true;
            }

            return false;
        }
    }
}