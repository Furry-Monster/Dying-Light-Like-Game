using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.Climbing
{
    public class ClimbablePoint
    {
        public RaycastHit horizontalHit;
        public RaycastHit verticalHit;
        public float factor;
    }

    [System.Serializable]
    public class ClimbJumpState : ClimbStateBase
    {
        [SerializeField] private LayerMask jumpObstacleMask;
        [Header("Animation")]
        public string hopRightState = "Climb.Hop Right";
        public string hopLeftState = "Climb.Hop Left";
        public string hopUpState = "Climb.Hop Up";
        public string hopDropState = "Climb.Hop Drop";
        public string jumpBackState = "Climb.Jump From Wall";

        public float horizontalHopDuration = 0.5f;
        public float HopUpDuration = 0.25f;
        public float HopDropDuration = 0.25f;
        [Header("Jump Back Parameters")]
        [SerializeField] private float jumpBackSpeed = 10f;
        [SerializeField] private AnimationCurve moveCurve;

        [Header("Cast Parameters")]
        public float maxForwardDistance = 1f;
        public int iterations = 10;
        [Space]
        [SerializeField] private float capsuleCastRadius = 0.25f;
        [SerializeField] private float maxHeight = 1f;
        [SerializeField] private float minHeight = -1f;
        [SerializeField] private float minJumpDistance = 1f;
        [SerializeField] private float maxJumpDistance = 2.25f;
        [SerializeField] private float maxJumpBackDistance = 5f;
        [SerializeField] private float maxHangHeightJump = 0.5f;
        [Header("Jump IK")]
        [SerializeField] private float handZOffset = 0.4f;

        [Header("Debug")]
        public Color debugColor = Color.cyan;
        public Color debugFoundHitColor = Color.magenta;

        Vector3 _targetPos;
        Quaternion _targetRot;

        private bool _hasJump;
        private float _startTime;
        private float _targetDuration;
        private bool _jumpBack;
        private Vector3 _jumpBackDirection;

        private List<ClimbablePoint> _availablePoints = new List<ClimbablePoint>();

        public override void EnterState(ClimbStateContext context)
        {
            _jumpBack = false;
            if (_hasJump && Time.time - _startTime < _targetDuration) return;

            float back = Vector3.Dot(-context.transform.forward, context.transform.forward * context.input.y);

            if (back > 0.85f && context.animator.GetFloat("HangWeight") < 0.6f)
            {
                CastLedgesBack(context);
                CheckBackLedgesAvailable(context);
                _jumpBack = true;
            }

            if (!_jumpBack)
            {
                CastLedgesForward(context);
                CastLedgesHorizontal(context);
            }

            if (_hasJump = FoundBestLedge(context))
            {
                SetAnimation(context);
                context.climb.StartCoroutine(ResetIK(context));

                if (_jumpBack)
                {
                    context.climb.DisableTransformUpdate();
                    // call coroutine to wait animation and then, jump
                    context.climb.StartCoroutine(WaitJumpBackAnimation(0.62f, context));
                }
                else
                {
                    context.climb.DoTween(_targetPos, _targetRot, _targetDuration, _availablePoints[0].verticalHit.collider);
                    context.climb.ResetClimbVars();
                    _startTime = Time.time;
                }

                return;
            }
            else if (_jumpBack)
            {
                context.animator.CrossFadeInFixedTime(jumpBackState, 0.1f);
                context.climb.FinishAfterAnimation(jumpBackState);

                context.climb.DisableTransformUpdate();

                _hasJump = true;
                _jumpBackDirection = -context.transform.forward;
                context.climb.StartCoroutine(WaitJumpBackAnimation(0.6f, context));
                return;
            }

            // if any situation happened, check if ledge allows to freely jump
            if(context.climb.CurrentCollider != null &&
                context.climb.CurrentCollider.TryGetComponent(out Ledge ledge))
            {
                if (ledge.CanFreelyJump)
                {
                    if (Mathf.Abs(context.input.x) > 0.4f)
                    {
                        context.animator.CrossFadeInFixedTime("Climb.Hop Side", 0.1f);
                        context.animator.SetBool("Mirror", context.input.x < 0);
                        context.climb.FinishAfterAnimation("Climb.Hop Side", 0.4f);

                        context.climb.DisableTransformUpdate();
                        context.climb.BlockCurrentLedge();

                        int dir = context.input.x > 0 ? 1 : -1;
                        context.climb.SetVelocity(context.transform.right * dir * 6f + Vector3.up * 4f);

                        _hasJump = true;
                        _startTime = Time.time;
                        _targetDuration = horizontalHopDuration;
                        return;
                    }
                }
            }

            context.SetState(context.Idle);
        }

        private void SetRightHandIK(ClimbStateContext context)
        {
            context.ik.SetRightHandJumpEffector(_availablePoints[0].verticalHit.point + _availablePoints[0].horizontalHit.normal * handZOffset);
        }
        private void SetLefttHandIK(ClimbStateContext context)
        {
            context.ik.SetLeftHandJumpEffector(_availablePoints[0].verticalHit.point + _availablePoints[0].horizontalHit.normal * handZOffset);
        }

        private IEnumerator ResetIK(ClimbStateContext context)
        {
            yield return new WaitForSeconds(_targetDuration * 0.6f);
            context.ik.SetLeftHandIKTarget(ClimbIK.TargetHandIK.OnLedge);
            context.ik.SetRightHandIKTarget(ClimbIK.TargetHandIK.OnLedge);
        }

        private IEnumerator WaitJumpBackAnimation(float targetNormalizedtime, ClimbStateContext context)
        {
            float normalizedTime = 0;
            while(Mathf.Repeat(normalizedTime, 1) < targetNormalizedtime)
            {
                var state = context.animator.GetCurrentAnimatorStateInfo(0);
                
                if(state.IsName(jumpBackState))
                    normalizedTime = state.normalizedTime;

                // constantly update start time to avoid call this method twice
                _startTime = Time.time;
                yield return null;
            }

            if (_availablePoints.Count > 0)
            {
                // calculate correct duration based on jump speed
                float distance = Vector3.Distance(context.transform.position, _targetPos);
                _targetDuration = distance / jumpBackSpeed;

                // finish animation start, so perform jump back
                context.climb.DoTween(_targetPos, _targetRot, _targetDuration, moveCurve, _availablePoints[0].verticalHit.collider);
                context.climb.ResetClimbVars();

                context.climb.EnableTransformUpdate();
            }
            else
            {
                _targetDuration = 2f;
                context.transform.rotation = Quaternion.LookRotation(_jumpBackDirection);
                context.climb.SetVelocity(_jumpBackDirection * jumpBackSpeed + Vector3.up * 3 , true);
            }

            _startTime = Time.time;
        }

        private void SetAnimation(ClimbStateContext context)
        {
            Vector3 direction = (_targetPos - context.transform.position).normalized;

            float horizontal = Vector3.Dot(context.transform.right, direction);
            float vertical = Vector3.Dot(Vector3.up, direction);

            if(_jumpBack)
            {
                context.animator.CrossFadeInFixedTime(jumpBackState, 0.1f);
                _targetDuration = 10f; // set a cosmetic duration to avoid calling method twice

                // check to mirror
                float dot = Vector3.Dot(direction, context.transform.right);
                context.animator.SetBool("Mirror", dot < 0);

                return;
            }

            if (Mathf.Abs(horizontal) > 0.3f)
            {
                if (horizontal > 0)
                {
                    context.animator.CrossFadeInFixedTime(hopRightState, 0.1f);
                    SetRightHandIK(context);
                }
                else
                {
                    context.animator.CrossFadeInFixedTime(hopLeftState, 0.1f);
                    SetLefttHandIK(context);
                }

                _targetDuration = horizontalHopDuration;
            }
            else
            {
                if (vertical > 0)
                {
                    context.animator.CrossFadeInFixedTime(hopUpState, 0.1f);
                    _targetDuration = HopUpDuration;
                    //SetRightHandIK(context);
                }
                else
                {
                    context.animator.CrossFadeInFixedTime(hopDropState, 0.1f);
                    _targetDuration = HopDropDuration;
                    SetLefttHandIK(context);
                }
            }
        }

        public override void ExitState(ClimbStateContext context)
        {
            _hasJump = false;
            _jumpBack = false;
            _startTime = Time.time - _targetDuration + 0.1f; // useful to avoid double jump
            _availablePoints.Clear();

            context.ik.SetLeftHandIKTarget(ClimbIK.TargetHandIK.OnLedge);
            context.ik.SetRightHandIKTarget(ClimbIK.TargetHandIK.OnLedge);
        }

        private void CastLedgesOnDirection(ClimbStateContext context, Vector3 center, Vector3 castDirection, float maxdistance, Vector3 inputDirection, float maxDistanceAllowed)
        {
            Vector3 capsuleBot = center + Vector3.up * (minHeight + capsuleCastRadius);
            Vector3 capsuleTop = center + Vector3.up * (maxHeight - capsuleCastRadius);

            context.climb.DrawCapsule(capsuleBot, capsuleTop, capsuleCastRadius, debugColor, 2f);

            // loop trough all hits to check valid ones and add to the list
            foreach (var hit in Physics.CapsuleCastAll(capsuleBot, capsuleTop, capsuleCastRadius,
                castDirection, maxdistance, context.climb.ClimbMask, QueryTriggerInteraction.Collide))
            {
                // is a valid hit?
                if (hit.distance == 0) continue;

                context.climb.DrawSphere(hit.point, 0.1f, debugColor, 2f);

                // do top cast
                Vector3 startSphere = hit.point;
                startSphere.y = capsuleTop.y;
                foreach (var top in Physics.SphereCastAll(startSphere, context.climb.SphereCastRadius, Vector3.down,
                   maxHeight + Mathf.Abs(minHeight), context.climb.ClimbMask, QueryTriggerInteraction.Collide))
                {
                    // is valid hit?
                    if (top.distance == 0) continue;

                    // is it the same collider?
                    if (top.collider != hit.collider) continue;

                    // has a valid angle
                    if (Vector3.Dot(Vector3.up, top.normal) < 0.4f) continue;

                    // create new vars to change hit point and normal direction by ledges
                    RaycastHit horHit = hit;
                    RaycastHit topHit = top;

                    // this hit has a ledge component?
                    if (topHit.collider.TryGetComponent(out Ledge ledge))
                    {
                        // change target point and normal direction
                        Transform closest = ledge.GetClosestPoint(topHit.point, horHit.normal);
                        if (closest != null)
                        {
                            topHit.point = closest.position;
                            horHit.normal = closest.forward;
                        }
                    }

                    // check if point is free to climb
                    if (!context.climb.PositionFreeToClimb(horHit, topHit)) continue;

                    // calculate char pos for this ledge
                    Vector3 targetCharPos = context.climb.GetCharacterPositionOnLedge(horHit, topHit);
                    float distance = Vector3.Distance(context.transform.position, targetCharPos);

                    float minDistance = context.animator.GetFloat("HangWeight") > 0.6f ? minJumpDistance / 2 : 0.2f;
                    // distance is possible to reach? or is it to close?
                    if (distance > maxDistanceAllowed || distance < minDistance) continue;

                    // if hanging, check max y position
                    if (context.animator.GetFloat("HangWeight") > 0.6f)
                        if (Mathf.Abs(targetCharPos.y - context.transform.position.y) > maxHangHeightJump) continue;

                    // check direction
                    Vector3 direction = (targetCharPos - context.transform.position).normalized;

                    float dot = Vector3.Dot(direction, inputDirection);

                    // is this point in the input direction?
                    if (dot < 0.3f) continue;

                    // check if target tag is allowed
                    if (context.climb.IgnoreTags.Contains(topHit.collider.tag)) continue;

                    // check if trajectory is free
                    Vector3 obstDirection = (targetCharPos - context.transform.position).normalized;
                    float obstDistance = Vector3.Distance(targetCharPos, context.transform.position);
                    Vector3 charBot = context.transform.position + Vector3.up * 0.2f;
                    Vector3 charTop = context.transform.position + Vector3.up * 1.6f;
                    if (Physics.CapsuleCast(charBot, charTop, 0.2f, obstDirection, obstDistance, jumpObstacleMask, QueryTriggerInteraction.Ignore))
                        continue;

                    // add this hit to the list of possible hits
                    ClimbablePoint climbable = new ClimbablePoint();
                    climbable.horizontalHit = horHit;
                    climbable.verticalHit = topHit;
                    climbable.factor = distance * dot + dot;

                    context.climb.DrawSphere(topHit.point, 0.1f, Color.yellow, 1f);

                    _availablePoints.Add(climbable);
                }
            }
        }

        private void CastLedgesForward(ClimbStateContext context)
        {
            float step = (maxJumpDistance * 2) / iterations;
            Vector3 inputDirection = context.transform.right * context.input.x + Vector3.up * context.input.y;
            for (int i = 0; i < iterations; i++)
            {
                Vector3 center = context.grabReference.position - context.transform.right * maxJumpDistance - context.transform.forward * maxForwardDistance;
                center += context.transform.right * step * i;

                float allowedJumpDistance = context.animator.GetFloat("HangWeight") > 0.6f ? maxJumpDistance / 2 : maxJumpDistance;

                CastLedgesOnDirection(context, center, context.transform.forward, maxForwardDistance * 2, inputDirection, allowedJumpDistance);
            }
        }

        private void CastLedgesHorizontal(ClimbStateContext context)
        {
            if (Mathf.Abs(context.input.x) < 0.1f) return;

            Vector3 castDirection = (context.transform.right * context.input.x).normalized;

            Vector3 center = context.grabReference.position - castDirection * capsuleCastRadius;
            Vector3 inputDirection = context.transform.right * context.input.x + Vector3.up * context.input.y;

            float allowedJumpDistance = context.animator.GetFloat("HangWeight") > 0.6f ? maxJumpDistance / 2 : maxJumpDistance;

            CastLedgesOnDirection(context, center, castDirection, maxJumpDistance, inputDirection, allowedJumpDistance);

            context.climb.DrawLabel( "Side Jump [START]", center, Color.yellow, 1f);
            context.climb.DrawLabel( "Side Jump [END]", center + castDirection * allowedJumpDistance, Color.yellow, 1f);
        }

        private void CastLedgesBack(ClimbStateContext context)
        {
            float step = (maxJumpDistance * 2) / iterations;
            Vector3 castDirection = -context.transform.forward;
            Vector3 inputDirection = -context.transform.forward;

            for (int i = 0; i < iterations; i++)
            {
                Vector3 center = context.grabReference.position - context.transform.right * maxJumpDistance - castDirection * capsuleCastRadius;
                center += context.transform.right * step * i;

                context.climb.DrawSphere(center, capsuleCastRadius, Color.red, 2f);
                context.climb.DrawSphere(center + castDirection * maxJumpBackDistance * 2, capsuleCastRadius, Color.red, 2f);
                Debug.DrawLine(center, center + castDirection * maxJumpBackDistance * 2, Color.red, 2f);

                CastLedgesOnDirection(context, center, castDirection, maxJumpBackDistance * 2, inputDirection, maxJumpBackDistance);
            }

        }

        private void CheckBackLedgesAvailable(ClimbStateContext context)
        {
            for(int i = 0; i <_availablePoints.Count; i++)
            {
                var climbable = _availablePoints[i];

                if (Vector3.Dot(climbable.horizontalHit.normal, -context.transform.forward) > 0.1f)
                {
                    _availablePoints.RemoveAt(i);
                    i--;
                    continue;
                }

                // update climbable factor
                // this step is important to avoid character choose too far ledges
                float verticalDistance = Mathf.Abs(context.grabReference.position.y - climbable.verticalHit.point.y);
                climbable.factor += verticalDistance;
            }
        }

        private bool FoundBestLedge(ClimbStateContext context)
        {
            if (_availablePoints.Count > 0) 
            {
                // if character is jumping back, select the closest ledge, in this case, order by closest to furthest ledge
                if(_jumpBack)
                    _availablePoints.Sort((x, y) => x.factor.CompareTo(y.factor));
                else
                    _availablePoints.Sort((x, y) => y.factor.CompareTo(x.factor)); // if not jumping back, choose the furthest ledge

                ClimbablePoint climbable = _availablePoints[0];

                _targetPos = context.climb.GetCharacterPositionOnLedge(climbable.horizontalHit, climbable.verticalHit);
                _targetRot = context.climb.GetCharacterRotationOnLedge(climbable.horizontalHit);

                return true;
            }

            return false;
        }

        public override void ClimbUp(ClimbStateContext context)
        {
            if (_hasJump && Time.time - _startTime < horizontalHopDuration) return;

            context.SetState(context.ClimbUp);
        }

        public override void Drop(ClimbStateContext context)
        {
        }

        public override void Idle(ClimbStateContext context)
        {
            if (_hasJump && Time.time - _startTime < _targetDuration) return;

            context.SetState(context.Idle);
        }

        public override void Jump(ClimbStateContext context)
        {
            if (_hasJump && Time.time - _startTime < _targetDuration + 0.1f) return;

            EnterState(context);
        }
    }
}