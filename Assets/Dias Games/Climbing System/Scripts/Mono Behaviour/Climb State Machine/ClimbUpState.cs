using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.Climbing
{
    [System.Serializable]
    public class ClimbUpState : ClimbStateBase
    {
        public string braceClimbUpState = "Climb.Climb up";
        public string hangClimbUpState = "Climb.Hang Climb up";
        [Header("Cast parameters")]
        [SerializeField] private LayerMask GroundLayers;
        [SerializeField] private float sphereRadius = 0.3f;
        [SerializeField] private float horizontalOffset = 0.75f;
        [SerializeField] private float minHeight = 0.75f;

        private Vector3 _targetPos;
        private bool _hasClimb;

        public override void EnterState(ClimbStateContext context)
        {
            if (_hasClimb = FreeToClimb(context))
            {
                string animation = context.animator.GetFloat("HangWeight") > 0.6f ? hangClimbUpState : braceClimbUpState;

                context.animator.CrossFadeInFixedTime(animation, 0.1f);

                if (animation.Contains("Hang")) animation = "Hang Climb Up - End";
                context.climb.FinishAfterAnimation(animation, _targetPos, context.transform.rotation);

                context.climb.DrawSphere(_targetPos, 0.1f, Color.blue, 2f);
            }
            else
                context.SetState(context.Idle);
        }

        public override void ExitState(ClimbStateContext context)
        {
            _hasClimb = false;
        }

        private bool FreeToClimb(ClimbStateContext context)
        {
            Vector3 startSphere = context.topHit.point + context.transform.forward * horizontalOffset + Vector3.up * minHeight;

            context.climb.DrawSphere(startSphere, sphereRadius, Color.yellow, 2f);

            // first cast check if thre is a ground
            if(Physics.SphereCast(startSphere, sphereRadius, Vector3.down, out RaycastHit hit, 
                minHeight + 1f, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                // if hit something, means there is no space to climb up
                if (Physics.SphereCast(hit.point, sphereRadius, Vector2.up, out RaycastHit upHit,
                    minHeight, GroundLayers, QueryTriggerInteraction.Ignore))
                    return false;

                context.climb.DrawSphere(hit.point, sphereRadius, Color.yellow, 2f);
                Debug.DrawLine(startSphere, hit.point, Color.yellow, 2f);

                _targetPos = hit.point;
                
                // check if there is something blocking
                Vector3 capsuleBot = hit.point + Vector3.up * (sphereRadius + 0.1f);
                Vector3 capsuleTop = hit.point + Vector3.up * (minHeight - sphereRadius - 0.1f);

                context.climb.DrawSphere(capsuleBot, sphereRadius, Color.green, 2f);
                context.climb.DrawSphere(capsuleTop, sphereRadius, Color.green, 2f);
                Debug.DrawLine(capsuleTop, capsuleBot, Color.green, 2f);

                // overlap position that character will be
                // if nothing is found, can climb up
                if (Physics.OverlapCapsule(capsuleBot, capsuleTop, sphereRadius, GroundLayers, QueryTriggerInteraction.Ignore).Length == 0)
                {
                    _targetPos = hit.point;
                    return true;
                }
            }

            return false;
        }

        public override void ClimbUp(ClimbStateContext context)
        {
        }

        public override void Drop(ClimbStateContext context)
        {
        }

        public override void Idle(ClimbStateContext context)
        {
            if (!_hasClimb)
                context.SetState(context.Idle);
        }
    }
}