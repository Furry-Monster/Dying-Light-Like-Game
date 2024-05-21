using System.Collections.Generic;
using UnityEngine;


namespace DiasGames.Climbing
{
    public enum CornerSide { Right, Left}

    [System.Serializable]
    public class CornerOutState : ClimbStateBase
    {
        [SerializeField] private string rightCornerOut = "Climb.Right Corner Out";
        [SerializeField] private string leftCornerOut = "Climb.Left Corner Out";
        [SerializeField] private float cornerDuration = 0.3f;
        [Space]
        [SerializeField] private float castSideDistance = 1.2f;
        [SerializeField] private float castRadius = 0.2f;
        [SerializeField] private float castHeight = 0.5f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Collider _targetCollider;
        private float _startTime;

        [HideInInspector] public CornerSide cornerSide = CornerSide.Right;

        public override void EnterState(ClimbStateContext context)
        {
            if (CanCornerOut(context))
            {
                context.climb.DoTween(_targetPosition, _targetRotation, cornerDuration, _targetCollider);
                _startTime = Time.time;

                if (cornerSide == CornerSide.Right)
                    context.animator.CrossFadeInFixedTime(rightCornerOut, 0.1f);
                else
                    context.animator.CrossFadeInFixedTime(leftCornerOut, 0.1f);
            }
            else
                Idle(context);
        }

        public override void ExitState(ClimbStateContext context)
        {
        }

        private bool CanCornerOut(ClimbStateContext context)
        {
            int direction = cornerSide == CornerSide.Right ? 1 : -1; 

            // check to corner out
            Vector3 center = context.grabReference.position + context.transform.right * direction * castSideDistance + context.transform.forward;
            Vector3 capsuleBot = center + Vector3.down * (castHeight / 2 - castRadius);
            Vector3 capsuleTop = center + Vector3.up * (castHeight / 2 - castRadius);
            
            List<ClimbablePoint> climbablePoints = new List<ClimbablePoint>();

            context.climb.DrawCapsule(capsuleBot, capsuleTop, castRadius, Color.yellow);
            context.climb.DrawCapsule(capsuleBot - context.transform.right * direction, capsuleTop - context.transform.right * direction, castRadius, Color.yellow);

            foreach (var horHit in Physics.CapsuleCastAll(capsuleBot, capsuleTop, castRadius, -context.transform.right * direction,
                castSideDistance, context.climb.ClimbMask, QueryTriggerInteraction.Collide))
            {
                // valid hit?
                if (horHit.distance == 0) continue;

                Vector3 startTop = horHit.point;
                startTop.y = context.grabReference.position.y + context.climb.SphereCastHeight;

                foreach (var topHit in Physics.SphereCastAll(startTop, context.climb.SphereCastRadius, Vector3.down, context.climb.SphereCastHeight + 1,
                    context.climb.ClimbMask, QueryTriggerInteraction.Collide))
                {
                    // valid hit?
                    if (topHit.distance == 0) continue;

                    // is it the same collider
                    if (topHit.collider != horHit.collider) continue;

                    RaycastHit hor = horHit;
                    RaycastHit top = topHit;

                    if (top.collider.TryGetComponent(out Ledge ledge))
                    {
                        var closest = ledge.GetClosestPoint(top.point);
                        if (closest != null)
                        {
                            hor.normal = closest.forward;
                            top.point = closest.position;
                        }
                    }

                    // dot check to avoid select the same direction character is already climbing
                    if (Vector3.Dot(-context.transform.forward, hor.normal) > 0.5f) continue;

                    // check if character can climb in that position
                    if (!context.climb.PositionFreeToClimb(hor, top)) continue;

                    // add this point to the list
                    ClimbablePoint newPoint = new ClimbablePoint();
                    newPoint.horizontalHit = hor;
                    newPoint.verticalHit = top;
                    newPoint.factor = Vector3.Distance(context.climb.GetCharacterPositionOnLedge(hor, top), context.transform.position);

                    climbablePoints.Add(newPoint);

                    context.climb.DrawSphere(top.point, 0.1f, Color.yellow);
                }
            }

            if (climbablePoints.Count == 0) return false;

            climbablePoints.Sort((x, y) => x.factor.CompareTo(y.factor));
            ClimbablePoint point = climbablePoints[0];

            _targetPosition = context.climb.GetCharacterPositionOnLedge(point.horizontalHit, point.verticalHit);
            _targetRotation = context.climb.GetCharacterRotationOnLedge(point.horizontalHit);
            _targetCollider = point.verticalHit.collider;

            return true;
        }

        public override void Idle(ClimbStateContext context)
        {
            if (Time.time - _startTime < cornerDuration) return;

            context.SetState(context.Idle);
        }
    }
}