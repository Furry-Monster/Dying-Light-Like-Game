using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.Climbing
{
    [System.Serializable]
    public class ClimbDropState : ClimbStateBase
    {
        [SerializeField] private string dropToFall = "Climb.Braced Drop";
        [SerializeField] private string dropHop = "Climb.Hop Drop";
        [SerializeField] private float dropDuration = 0.3f;
        [Header("Casting")]
        [SerializeField] private float maxHeightBelow = 1.5f;
        [SerializeField] private float maxCastingDistance = 1.5f;
        [SerializeField] private float castRadius = 0.75f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _startTime;

        private RaycastHit _targetHorizontalHit;
        private RaycastHit _targetVerticalHit;

        public override void EnterState(ClimbStateContext context)
        {
            if(context.animator.GetFloat("HangWeight") < 0.6f && FoundLedgeToDrop(context))
            {
                context.animator.CrossFadeInFixedTime(dropHop, 0.1f);
                context.climb.DoTween(_targetPosition, _targetRotation, dropDuration, _targetVerticalHit.collider);
                SetLefttHandIK(context);
                context.climb.StartCoroutine(ResetIK(context));
            }
            else
            {
                context.animator.CrossFadeInFixedTime(dropToFall, 0.1f);
                context.climb.FinishAfterAnimation(dropToFall);
                context.climb.BlockCurrentLedge();
            }

            _startTime = Time.time;
        }

        public override void ExitState(ClimbStateContext context)
        {
            _startTime = 0;
        }

        private void SetLefttHandIK(ClimbStateContext context)
        {
            context.ik.SetLeftHandJumpEffector(_targetVerticalHit.point);
        }

        private IEnumerator ResetIK(ClimbStateContext context)
        {
            yield return new WaitForSeconds(dropDuration * 0.6f);
            context.ik.SetLeftHandIKTarget(ClimbIK.TargetHandIK.OnLedge);
            context.ik.SetRightHandIKTarget(ClimbIK.TargetHandIK.OnLedge);
        }

        public override void Idle(ClimbStateContext context)
        {
            if (Time.time - _startTime < dropDuration) return;

            context.SetState(context.Idle);
        }

        private bool FoundLedgeToDrop(ClimbStateContext context)
        {
            Vector3 capsuleTop = context.grabReference.position + Vector3.down * castRadius - context.transform.forward * maxCastingDistance;
            Vector3 capsuleBot = context.grabReference.position + Vector3.down * (maxHeightBelow + castRadius) - context.transform.forward * maxCastingDistance;
            List<ClimbablePoint> _availablePoints = new List<ClimbablePoint>();

            foreach (var forwardHit in Physics.CapsuleCastAll(capsuleBot, capsuleTop, castRadius, context.transform.forward, 
                maxCastingDistance * 2, context.climb.ClimbMask, QueryTriggerInteraction.Collide))
            {
                // valid hit?
                if (forwardHit.distance == 0) continue;

                // calculate top start position
                Vector3 startTop = forwardHit.point;
                startTop.y = context.grabReference.position.y;

                foreach(var topHit in Physics.SphereCastAll(startTop, 0.2f, Vector3.down, maxHeightBelow,
                    context.climb.ClimbMask, QueryTriggerInteraction.Collide))
                {
                    // is a valid hit?
                    if (topHit.distance == 0) continue;

                    // is it the same collider?
                    if (topHit.collider != forwardHit.collider) continue;

                    RaycastHit hit = forwardHit;
                    RaycastHit top = topHit;

                    // try get ledge component
                    if(top.collider.TryGetComponent(out Ledge ledge))
                    {
                        var closest = ledge.GetClosestPoint(top.point);
                        if (closest)
                        {
                            top.point = closest.position;
                            hit.normal = closest.forward;
                        }
                    }

                    // check if point is free to climb
                    if (!context.climb.PositionFreeToClimb(hit, topHit)) continue;

                    // check point direction
                    if (Vector3.Dot(-hit.normal, context.transform.forward) < 0.7f) continue;

                    // calculate target position to get direction
                    Vector3 targetPosition = context.climb.GetCharacterPositionOnLedge(hit, top);
                    Vector3 direction = (targetPosition - context.transform.position).normalized;

                    // create new climb point to add to list
                    ClimbablePoint newPoint = new ClimbablePoint();
                    newPoint.horizontalHit = hit;
                    newPoint.verticalHit = top;
                    newPoint.factor = Vector3.Dot(Vector3.down, direction);

                    // add point to the list
                    _availablePoints.Add(newPoint);
                }
            }


            // found any point?
            if(_availablePoints.Count > 0)
            {
                _availablePoints.Sort((x, y) => y.factor.CompareTo(x.factor));
                var point = _availablePoints[0];

                _targetPosition = context.climb.GetCharacterPositionOnLedge(point.horizontalHit, point.verticalHit);
                _targetRotation = context.climb.GetCharacterRotationOnLedge(point.horizontalHit);

                _targetHorizontalHit = point.horizontalHit;
                _targetVerticalHit = point.verticalHit;
                return true;
            }

            return false;
        }
    }
}