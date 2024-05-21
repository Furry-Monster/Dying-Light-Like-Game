using System.Collections;
using UnityEngine;

namespace DiasGames.Climbing
{
    [System.Serializable]
    public class CornerInState : ClimbStateBase
    {
        [SerializeField] private string rightCornerIn = "Climb.Hop Right";
        [SerializeField] private string leftCornerIn = "Climb.Hop Left";
        [SerializeField] private float cornerDuration = 0.3f;
        [Space]
        [SerializeField] private float castSideDistance = 1.2f;
        [SerializeField] private float castRadius = 0.2f;
        [SerializeField] private float capsuleHeight = 0.2f;

        private ClimbablePoint _targetPoint;
        private float _startTime;

        public override void EnterState(ClimbStateContext context)
        {
            if (Time.time - _startTime < cornerDuration) return;

            if (Mathf.Abs(context.input.x) > 0.5f && FoundLedge(context))
            {
                Vector3 targetPos = context.climb.GetCharacterPositionOnLedge(_targetPoint.horizontalHit, _targetPoint.verticalHit);
                Quaternion targetRot = context.climb.GetCharacterRotationOnLedge(_targetPoint.horizontalHit);

                // do transition to target point
                context.climb.DoTween(targetPos, targetRot, cornerDuration, _targetPoint.horizontalHit.collider);

                // set animation
                if(context.input.x > 0)
                {
                    context.animator.CrossFadeInFixedTime(rightCornerIn, 0.1f);
                }
                else
                {
                    context.animator.CrossFadeInFixedTime(leftCornerIn, 0.1f);
                }

                _startTime = Time.time;
                return;
            }

            context.SetState(context.Idle);
        }

        public override void ExitState(ClimbStateContext context)
        {
        }

        private bool FoundLedge(ClimbStateContext context)
        {
            float direction = context.input.x > 0 ? 1 : -1;
            Vector3 castDirection = context.transform.right * direction;

            Vector3 center = context.grabReference.position;
            Vector3 capsuleBot = center + Vector3.down * (capsuleHeight / 2 - castRadius);
            Vector3 capsuleTop = center + Vector3.up * (capsuleHeight / 2 - castRadius);

            context.climb.DrawCapsule(capsuleBot, capsuleTop, castRadius, Color.red);
            context.climb.DrawCapsule(capsuleBot + castDirection * castSideDistance, capsuleTop + castDirection * castSideDistance , castRadius, Color.red);

            context.climb.DrawLabel("Corner In Cast [Start]", center, Color.yellow);
            context.climb.DrawLabel("Corner In Cast [End]", center + castDirection * castSideDistance, Color.yellow);

            foreach(var hit in Physics.CapsuleCastAll(capsuleBot, capsuleTop, castRadius, castDirection, castSideDistance,
                context.climb.ClimbMask, QueryTriggerInteraction.Collide))
            {
                // valid hit?
                if (hit.distance == 0) continue;

                context.climb.DrawLabel("Corner In Cast Hit found", hit.point, Color.yellow);

                // has enough angle?
                if (Vector3.Dot(context.transform.forward, hit.normal) < -0.7f) continue;

                // cast top
                Vector3 startTop = hit.point;
                startTop.y = context.grabReference.position.y + 1;

                foreach(var top in Physics.SphereCastAll(startTop, castRadius, Vector3.down, 1.5f, 
                    context.climb.ClimbMask, QueryTriggerInteraction.Collide))
                {
                    // valid hit?
                    if (top.distance == 0) continue;

                    // is the same collider?
                    if (top.collider != hit.collider) continue;

                    context.climb.DrawSphere(top.point, 0.1f, Color.magenta);

                    // check top angle
                    if (top.normal.y < 0.4f) continue;

                    RaycastHit finalHor = hit;
                    RaycastHit finalTop = top;

                    // try get ledge component
                    if(finalTop.collider.TryGetComponent(out Ledge ledge))
                    {
                        Transform closest = ledge.GetClosestPoint(finalTop.point);
                        if (closest)
                        {
                            finalHor.normal = closest.forward;
                            finalTop.point = closest.position;
                        }
                    }

                    // check if point is free
                    if (!context.climb.PositionFreeToClimb(finalHor, finalTop)) continue;

                    _targetPoint = new ClimbablePoint();
                    _targetPoint.horizontalHit = finalHor;
                    _targetPoint.verticalHit = finalTop;

                    return true;
                }
            }

            return false;
        }

        public override void Idle(ClimbStateContext context)
        {
            if (Time.time - _startTime < cornerDuration) return;

            context.SetState(context.Idle);
        }
    }
}