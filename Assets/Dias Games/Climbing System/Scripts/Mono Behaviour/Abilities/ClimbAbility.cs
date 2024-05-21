using System.Collections.Generic;
using UnityEngine;
using DiasGames.Components;
using DiasGames.Climbing;
using DiasGames.Debugging;

namespace DiasGames.Abilities
{

    public class ClimbAbility : AbstractAbility
    {    
        [SerializeField] private LayerMask climbMask;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private List<string> ignoreTags = new List<string>();
        [Space]
        [SerializeField] private Transform grabReference;
        [SerializeField] private float globalRadiusDetection = 0.5f;
        [SerializeField] private Vector2 offsetOnLedge;
        [Header("Capsule Cast Parameters")]
        [SerializeField] private float capsuleCastDistance = 0.75f;
        [SerializeField] private float capsuleHeight = 1f;
        [SerializeField] private float capsuleRadius = 0.15f;
        [SerializeField] private int capsuleCastIterations = 10;
        [Header("Sphere Cast Parameters (For Top Detection)")]
        [SerializeField] private float sphereCastMaxHeight = 1f;
        [SerializeField] private float sphereCastDistance = 2f;
        [SerializeField] private float sphereCastRadius = 0.1f;
        [Header("Shimmy Casting")]
        [SerializeField] private int sideCastIterations = 10;
        [SerializeField] private float sideCastRange = 0.5f;
        [SerializeField] private float sideCastRadius = 0.1f;
        [SerializeField] private float sideCastHeight = 0.5f;
        [Header("Foot Casting")]
        [SerializeField] private LayerMask footMask;
        [SerializeField] private float footCastRadius = 0.3f;
        [SerializeField] private float footCapsuleHeight = 1f;
        [SerializeField] private float footCastDistance = 1f;
        [Header("Matching Position Parameters")]
        [SerializeField] private float startClimbMatchTime = 0.15f;
        [SerializeField] private AnimationCurve defaultMatchingCurve;
        [Space]
        // state machine
        [SerializeField] private ClimbStateContext _context;

        [Header("Debug")]
        [SerializeField] private Color debugColor;

        // public getters
        public float CapsuleCastHeight { get { return capsuleHeight; } }
        public float CapsuleCastRadius { get { return capsuleRadius; } }
        public float SphereCastHeight { get { return sphereCastMaxHeight; } }
        public float SphereCastRadius { get { return sphereCastRadius; } }
        public LayerMask ClimbMask { get { return climbMask; } }
        public Collider CurrentCollider { get { return _currentCollider; } }
        public List<string> IgnoreTags { get { return ignoreTags; } }

        // components
        private IMover _mover;
        private ICapsule _capsule;
        private ClimbIK _climbIK;
        private CastDebug _debug;

        // private internal climbing controller
        private Collider _currentCollider;
        private RaycastHit _currentHorizontalHit;
        private RaycastHit _currentTopHit;
        private RaycastHit _wallHit;
        private float _hangWeight = 0;
        private float _hangvel;
        private Transform _targetClimbCharPos; // effector transform to help set character position on movable ledges
        private Transform _climbTargetHit; // effector transform to help set character position on movable ledges (for top and horizontal hits)

        private Vector3 _lastHitPoint;
        private float _timeWithoutLedge = 0;

        // internal vars
        private Camera _mainCamera;
        private Vector2 _localCoordMove;
        private bool _updateTransform = true;

        // waiting animation to finish
        private bool _waitingAnimation;
        private string _animationStateToWait;
        private bool _matchTarget;
        private Vector3 _matchTargetPosition;
        private Quaternion _matchTargetRotation;
        private float _targetNormalizedTime;

        // tween parameters
        private bool _isDoingTween = false;
        private float _currentTweenWeight = 0;
        private float _tweenDuration = 0;
        private float _tweenStartTime;
        private float _tweenStep;
        private Vector3 _tweenStartPosition;
        private Quaternion _tweenStartRotation;
        private Vector3 _tweenBezierPoint;
        private Transform _tweenTarget;
        private AnimationCurve _targetCurve;

        // side shimmy control
        private float _leftDistanceToShimmy;
        private float _rightDistanceToShimmy;
        private float _shimmyMinRatio = 0.5f;
        private bool _stopRightShimmy;
        private bool _stopLeftShimmy;

        // avoid climb the same ledge when droping
        private Collider _ledgeBlocked;
        private float _timeBlockStarted;

        #region State Machine Methods

        public void Idle() => _context.CurrentClimbState.Idle(_context);
        public void ClimbUp() => _context.CurrentClimbState.ClimbUp(_context);
        public void Jump() => _context.CurrentClimbState.Jump(_context);
        public void Drop() => _context.CurrentClimbState.Drop(_context);
        public void CornerOut(CornerSide side)
        {
            _context.CornerOut.cornerSide = side;
            _context.CurrentClimbState.CornerOut(_context);
        }

        #endregion

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();
            _climbIK = GetComponent<ClimbIK>();
            _debug = GetComponent<CastDebug>();

            _mainCamera = Camera.main;

            CreateTransforms();
        }

        private void CreateTransforms()
        {
            if (_targetClimbCharPos == null)
                _targetClimbCharPos = new GameObject("Climb Char Target").transform;

            if (_tweenTarget == null)
                _tweenTarget = new GameObject("Climb Tween Target").transform;

            if(_climbTargetHit == null)
                _climbTargetHit = new GameObject("Climb Hit").transform;
        }

        public override bool ReadyToRun()
        {
            if (_mover.IsGrounded()) return false;

            return HasLedge();
        }
        public override void OnStartAbility()
        {
            UpdateContextVars();

            _mover.StopMovement();
            _mover.DisableGravity();

            _climbIK.RunIK();
            _climbIK.UpdateIKReferences(climbMask, footMask,_currentHorizontalHit);

            _hangWeight = HasWall() ? 0 : 1;
            _animator.SetFloat("HangWeight", _hangWeight);

            _waitingAnimation = false;
            _matchTarget = false;
            _updateTransform = true;

            _context.SetState(_context.Idle);

            DoTween(GetCharacterPositionOnLedge(), GetCharacterRotationOnLedge(), startClimbMatchTime, _currentCollider);

            SetAnimationState("Climb.Start Climb");

            _timeWithoutLedge = 0;
        }

        public override void OnStopAbility()
        {
            _climbIK.StopIK();
            _mover.StopRootMotion();
            _mover.EnableGravity();

            if (!string.IsNullOrEmpty(_animationStateToWait))
            {
                if (_animationStateToWait.Contains("Drop"))
                {
                    _mover.SetVelocity(Vector3.down * 3f);
                }

                if (_animationStateToWait.Contains("Jump"))
                {
                }
            }

            _capsule.EnableCollision();
        }

        public override void UpdateAbility()
        {
            _climbIK.UpdateIKReferences(climbMask, footMask,_currentHorizontalHit);

            UpdateFootWall();
            UpdateTween();

            if (_waitingAnimation)
            {
                if (_animator.IsInTransition(0)) return;

                var state = _animator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = Mathf.Repeat(state.normalizedTime, 1);
                if (state.IsName(_animationStateToWait))
                {
                    if (_matchTarget && !_animator.isMatchingTarget)
                    {
                        _capsule.DisableCollision();
                        _animator.MatchTarget(_matchTargetPosition, _matchTargetRotation, AvatarTarget.RightFoot,
                            new MatchTargetWeightMask(Vector3.one, 0f), 0.4f, 0.9f);

                        _matchTarget = false;
                    }

                    if (normalizedTime >= _targetNormalizedTime)
                    {
                        StopAbility();
                        return;
                    }
                }

                return;
            }

            if (_isDoingTween)
                return;

            if (Vector3.Distance(_lastHitPoint,_targetClimbCharPos.position) < 0.25f && _updateTransform)
            {
                _mover.SetPosition(transform.position + (_targetClimbCharPos.position - _lastHitPoint));
            }

            ProccessInput();

            if (HasCurrentLedge())
            {
                SetCharacterPosition();
                CheckLedgeSide();

                ProccesMovement();

                _mover.ApplyRootMotion(Vector3.one);

                _context.CurrentClimbState.Idle(_context);

                if (_context.CurrentClimbState == _context.Idle)
                {
                    if (_rightDistanceToShimmy < sideCastRange * _shimmyMinRatio)
                        _stopRightShimmy = true;
                    if (_rightDistanceToShimmy >= sideCastRange * 0.95f)
                        _stopRightShimmy = false;

                    if (_leftDistanceToShimmy < sideCastRange * _shimmyMinRatio)
                        _stopLeftShimmy = true;
                    if (_leftDistanceToShimmy >= sideCastRange * 0.95f)
                        _stopLeftShimmy = false;

                    if ((_localCoordMove.x > 0 && _stopRightShimmy) ||
                    (_localCoordMove.x < 0 && _stopLeftShimmy))
                        _animator.SetFloat("Horizontal", 0);
                    else
                        _animator.SetFloat("Horizontal", _localCoordMove.x, 0.1f, Time.deltaTime);
                }
                else
                    _animator.SetFloat("Horizontal", 0);

                _animator.SetFloat("Vertical", _localCoordMove.y, 0.1f, Time.deltaTime);

                _context.CurrentClimbState.CornerIn(_context);
                _lastHitPoint = _targetClimbCharPos.position;
                _timeWithoutLedge = 0;
            }
            else
            {
                if (_updateTransform)
                    _timeWithoutLedge += Time.deltaTime;

                _climbTargetHit.parent = null;

                if (_timeWithoutLedge > 0.1f)
                {
                    BlockCurrentLedge();
                    StopAbility();
                }
            }

            UpdateContextVars();
        }

        public void SetVelocity(Vector3 velocity, bool gravity = false)
        {
            _capsule.EnableCollision();
            _mover.StopRootMotion();
            _mover.SetVelocity(velocity);
            if (gravity) _mover.EnableGravity();
        }

        private void UpdateFootWall()
        {
            float targetWeight = HasWall() ? 0 : 1;
            _hangWeight = Mathf.SmoothDamp(_hangWeight, targetWeight, ref _hangvel, 0.12f);
            _animator.SetFloat("HangWeight", _hangWeight);
        }

        private bool HasLedge()
        {
            // first step: overlap a sphere around climbing grab position. If find some ledge, keep logic
            Collider[] colls = Physics.OverlapSphere(grabReference.position, globalRadiusDetection, climbMask, QueryTriggerInteraction.Collide);

            if (colls.Length == 0) return false;

            // set capsule points to cast
            Vector3 capsuleBotPoint = grabReference.position + Vector3.down * (capsuleHeight*0.5f - capsuleRadius);
            Vector3 capsuleTopPoint = grabReference.position + Vector3.up * (capsuleHeight*0.5f - capsuleRadius);
            float angleStep = 360.0f / capsuleCastIterations;

            // create two lists: one for horizontal hits and other for top hits
            // they must have the same index, to match final result
            List<RaycastHit> horizontalHits = new List<RaycastHit>();
            List<RaycastHit> topHits = new List<RaycastHit>();

            // cast a capsule around all directions
            // it will cast a capsule in all directions to allow choose the ledge
            // that has the best direction to match current character direction
            for(int i=0; i < capsuleCastIterations; i++)
            {
                // get current angle direction in radians
                float currentAngleRad = i*angleStep * Mathf.Deg2Rad;

                // calculate direction to cast
                Vector3 direction = new Vector3(Mathf.Cos(currentAngleRad), 0, Mathf.Sin(currentAngleRad)).normalized;

                // perform capsule cast all. It will allow to check all available ledges
                // also set start point a little back to allow more flexible ledge climbing
                RaycastHit[] hitsArray = Physics.CapsuleCastAll(capsuleBotPoint - direction * capsuleCastDistance, capsuleTopPoint - direction * capsuleCastDistance,
                    capsuleRadius, direction, capsuleCastDistance * 2, climbMask, QueryTriggerInteraction.Collide);

                // loop through all ledges found
                foreach(RaycastHit horHit in hitsArray)
                {
                    // check if this ledge is blocked
                    if (_ledgeBlocked != null && _ledgeBlocked == horHit.collider && Time.time - _timeBlockStarted < 1f)
                        continue;

                    // check if this ledge is to be ignored
                    if (ignoreTags.Contains(horHit.collider.tag)) continue;

                    // is it a valid hit?
                    if (horHit.distance != 0)
                    {  // now, perform a top cast, to check if it's a valid ledge

                        // check angle
                        if (Vector3.Dot(transform.forward, -horHit.normal) < -0.1f) continue;

                        // set start sphere cast
                        Vector3 startSphere = horHit.point;
                        startSphere.y = grabReference.position.y + sphereCastMaxHeight;

                        // perform sphere cast all
                        var topHitsArray = Physics.SphereCastAll(startSphere, sphereCastRadius, Vector3.down, 
                            sphereCastDistance, climbMask, QueryTriggerInteraction.Collide);

                        // create a temporary list to choose the best hit after cast
                        List<RaycastHit> possibleTopHits = new List<RaycastHit>();
                        foreach(var topHit in topHitsArray)
                        {
                            // is it a valid hit?
                            if (topHit.distance == 0) continue;

                            // is it the same collider?
                            if (topHit.collider != horHit.collider) continue;

                            // has possible normal?
                            if (Vector3.Dot(Vector3.up, topHit.normal) < 0.5f) continue;

                            // add this hit in possible hits
                            possibleTopHits.Add(topHit);
                        }

                        // found any possible hit?
                        if (possibleTopHits.Count == 0) continue;

                        // now select the closest hit 
                        RaycastHit closestHit = possibleTopHits[0];
                        float currentDistance = Mathf.Abs(closestHit.point.y - grabReference.position.y);
                        foreach(var closestCandidate in possibleTopHits)
                        {
                            if (Mathf.Abs(closestHit.point.y - grabReference.position.y) < currentDistance)
                                closestHit = closestCandidate;
                        }

                        RaycastHit hor = horHit;
                        RaycastHit top = closestHit;

                        if(top.collider.TryGetComponent(out Ledge ledge))
                        {
                            Transform closest = ledge.GetClosestPoint(top.point);
                            if(closest != null)
                            {
                                if (Vector3.Dot(closest.forward, transform.forward) < 0.2f)
                                {
                                    hor.normal = closest.forward;
                                    top.point = closest.position;
                                }
                            }
                        }

                        // check if point is free to climb
                        if (!PositionFreeToClimb(hor, top)) continue;

                        // finally add both hits to possible selection
                        horizontalHits.Add(hor);
                        topHits.Add(top);
                    }
                }
            }

            // found any valid climbing?
            if (horizontalHits.Count == 0) return false;

            int index = 0;
            float bestDot = -1;
            for (int i = 0; i < horizontalHits.Count && i < topHits.Count; i++)
            {
                // caluclate dot to check wich ledge has the best match
                float dot = Vector3.Dot(transform.forward, -horizontalHits[i].normal);

                // if dot is greater than currento best dot, update best dot
                if(dot > bestDot)
                {
                    bestDot = dot;
                    index = i;
                }
            }

            // set controller vars

            // set current collider in use
            _currentCollider = topHits[index].collider;

            // set current raycast hits to access for positioning methods
            _currentHorizontalHit = horizontalHits[index];
            _currentTopHit = topHits[index];

            UpdateClimbHit();
            _lastHitPoint = _targetClimbCharPos.position;

            return true;
        }

        private void CheckLedgeSide()
        {
            // cast left
            CastShimmy(ref _leftDistanceToShimmy, -1);

            // cast right
            CastShimmy(ref _rightDistanceToShimmy, 1);

        }

        /// <summary>
        /// This function cast multiples spheres in side direction.
        /// It sets how many meters left to shimmy.
        /// </summary>
        /// <param name="shimmyDistance"></param>
        /// <param name="direction"></param>
        private void CastShimmy(ref float shimmyDistance, int direction)
        {
            // calculate steps to cast spheres
            float step = sideCastRange / sideCastIterations;

            // set current max distance to the maximum
            shimmyDistance = sideCastRange;

            // do iterations
            for (int i = 0; i < sideCastIterations; i++)
            {
                // set start position to cast
                Vector3 center = grabReference.position + transform.right * direction * (sideCastRange - step * i);
                Vector3 capsuleTop = center + Vector3.up * (sideCastHeight / 2f - sideCastRadius);
                Vector3 capsuleBot = center + Vector3.down * (sideCastHeight / 2f - sideCastRadius);

                // debug start sphere position
                DrawCapsule(capsuleBot, capsuleTop, sideCastRadius, debugColor);

                // create a list of hits that is available to shimmy
                List<RaycastHit> hits = new List<RaycastHit>();

                // do sphere cast and loop through all
                foreach (var hit in Physics.CapsuleCastAll(capsuleTop, capsuleBot, sideCastRadius, transform.forward,
                    capsuleCastDistance, climbMask, QueryTriggerInteraction.Collide))
                {
                    // is a valid hit?
                    if (hit.distance == 0) continue;

                    // check angle
                    if (Vector3.Dot(_currentHorizontalHit.normal, hit.normal) < 0.7f) continue;

                    // if hit is the same of current collider
                    // TODO: allow climb different collider
                    if (hit.collider == _currentCollider)
                    {
                        // add this hit to the list
                        hits.Add(hit);

                        // debug final hit pos
                        DrawSphere(hit.point, sideCastRadius, debugColor);
                        Debug.DrawLine(center, hit.point, debugColor);
                    }
                }

                // if nothing was found, update max distance available
                if (hits.Count == 0)
                    shimmyDistance = sideCastRange - step * i;
            }

            if (shimmyDistance > sideCastRange * _shimmyMinRatio) return;

            if (Mathf.Abs(_localCoordMove.x) < 0.2f) return;

            if (_localCoordMove.x < 0 && direction == 1) return;
            if (_localCoordMove.x > 0 && direction == -1) return;

            CornerOut(direction == 1 ? CornerSide.Right : CornerSide.Left);
        }

        /// <summary>
        /// this function is called inside update ability. 
        /// It assumes character has already found a ledge
        /// </summary>
        /// <returns></returns>
        private bool HasCurrentLedge()
        {
            // start sphere position for horizontal cast
            Vector3 capsuleBot = grabReference.position + Vector3.down * (sideCastHeight / 2f - sideCastRadius);
            Vector3 capsuleTop = grabReference.position + Vector3.up * (sideCastHeight / 2f - sideCastRadius);

            // debug initial sphere cast
            DrawCapsule(capsuleBot, capsuleTop, capsuleRadius, Color.red);

            // list of climbable points
            List<ClimbablePoint> climbables = new List<ClimbablePoint>();

            // do sphere cast on forward direction and loop through all hits
            foreach (var hit in Physics.CapsuleCastAll(capsuleTop, capsuleBot, capsuleRadius, transform.forward,
                capsuleCastDistance, climbMask, QueryTriggerInteraction.Collide))
            {
                // is it a valid hit?
                if (hit.distance == 0) continue;

                // only keep checking if this hit is the same of current collider or
                // if current collider is null
                // TODO: improve to allow climb other colliders
                if (hit.collider == _currentCollider)
                {
                    // debug horizontal cast found
                    DrawSphere(hit.point, capsuleRadius, Color.red);

                    // set top start cast position
                    Vector3 initial = grabReference.position + Vector3.up;
                    int lineIterations = 20;
                    // loop raycast for top
                    for (int i = 0; i < lineIterations; i++)
                    {
                        Vector3 topStart = initial + transform.forward * i * (1f / lineIterations);

                        foreach (var top in Physics.RaycastAll(topStart, Vector3.down, 3f, climbMask, QueryTriggerInteraction.Collide))
                        {
                            // is top hit valid?
                            if (top.distance == 0) continue;

                            // check if top hit is the same as horizontal hit
                            if (top.collider == hit.collider)
                            {
                                // check if point is free to climb
                                if (!PositionFreeToClimb(hit, top))
                                    continue;

                                if (Vector3.Dot(top.normal, Vector3.up) < 0.5f)
                                    continue;

                                // update current climb parameters
                                _currentCollider = top.collider;
                                _currentTopHit = top;
                                _currentHorizontalHit = hit;

                                if(ignoreTags.Contains(_currentCollider.tag))
                                    return false;
                                

                                // correct top point
                                Vector3 point = _currentHorizontalHit.point;
                                point.y = top.point.y;
                                _currentTopHit.point = point;

                                UpdateClimbHit();

                                // debug final hit found
                                DrawSphere(top.point, sphereCastRadius, Color.red);
                                Debug.DrawLine(topStart, top.point, Color.red);

                                // debug ray
                                Debug.DrawLine(topStart, top.point, Color.red);

                                return true;
                            }
                        }

                        // debug ray
                        Debug.DrawLine(topStart, topStart + Vector3.down * sphereCastMaxHeight, Color.red);
                    }
                }


                // closest precision cast if loose current collider
                if (_currentCollider == null)
                {
                    // set top start cast position
                    Vector3 initial = hit.point;
                    initial.y = grabReference.position.y + SphereCastHeight;

                    // loop through all hits
                    foreach (var top in Physics.SphereCastAll(initial, sphereCastRadius,
                        Vector3.down, 3f, climbMask, QueryTriggerInteraction.Collide))
                    {
                        // is top hit valid?
                        if (top.distance == 0) continue;

                        // check if top hit is the same as horizontal hit
                        if (top.collider == hit.collider)
                        {
                            // check if point is free to climb
                            if (!PositionFreeToClimb(hit, top))
                                continue;

                            if (Vector3.Dot(top.normal, Vector3.up) < 0.5f)
                                continue;

                            // create climbable point
                            ClimbablePoint climbable = new ClimbablePoint();
                            climbable.horizontalHit = hit;
                            climbable.verticalHit = top;

                            // correct top point
                            Vector3 point = hit.point;
                            point.y = top.point.y;
                            climbable.verticalHit.point = point;

                            // try get ledge component
                            if (top.collider.TryGetComponent(out Ledge ledge))
                            {
                                var closest = ledge.GetClosestPoint(climbable.verticalHit.point);
                                if (closest)
                                {
                                    climbable.verticalHit.point = closest.position;
                                    climbable.horizontalHit.normal = closest.forward;
                                }
                            }

                            // calculate factor to get closest point
                            climbable.factor = Mathf.Abs(_currentTopHit.point.y - grabReference.position.y);

                            // add to list
                            climbables.Add(climbable);
                        }
                    }


                    if (climbables.Count > 0)
                    {
                        // sort by closest distance
                        climbables.Sort((x, y) => y.factor.CompareTo(x.factor));
                        var climb = climbables[0];

                        // update current climb parameters
                        _currentCollider = climb.verticalHit.collider;
                        _currentTopHit = climb.verticalHit;
                        _currentHorizontalHit = climb.horizontalHit;

                        UpdateClimbHit();

                        return true;
                    }
                }
            }

            ResetClimbVars();
            return false;
        }

        private void UpdateClimbHit()
        {
            if (Mathf.Abs(_localCoordMove.x) > 0.4f || _climbTargetHit.parent != _currentCollider.transform 
                || !IsAbilityRunning || Time.time - StartTime < 0.1f)
            {
                _climbTargetHit.parent = _currentCollider.transform;
                _climbTargetHit.position = _currentTopHit.point;
                _climbTargetHit.forward = _currentHorizontalHit.normal;
            }

            _targetClimbCharPos.parent = _currentCollider.transform;
            _targetClimbCharPos.position = GetCharacterPositionOnLedge();
        }

        private bool HasWall()
        {
            Vector3 targetPos = _currentHorizontalHit.collider != null && !_isDoingTween ? GetCharacterPositionOnLedge() : transform.position;
            Vector3 direction = _currentHorizontalHit.collider != null && !_isDoingTween ? -_currentHorizontalHit.normal : transform.forward;

            Vector3 capsuleBot = targetPos + Vector3.up * footCastRadius;
            Vector3 capsuleTop = targetPos + Vector3.up * (footCapsuleHeight - footCastRadius);

            DrawCapsule(capsuleTop, capsuleBot, footCastRadius, Color.cyan);

            if (Physics.CapsuleCast(capsuleBot, capsuleTop, footCastRadius, direction, out _wallHit, footCastDistance,
                footMask, QueryTriggerInteraction.Collide))
            {
                DrawCapsule(capsuleTop + direction * _wallHit.distance, capsuleBot + direction * _wallHit.distance, footCastRadius, Color.blue);
                return true;
            }

            return false;
        }

        private void UpdateContextVars()
        {
            _context.climb = this;
            _context.ik = _climbIK;
            _context.animator = _animator;
            _context.grabReference = grabReference;
            _context.transform = transform;
            _context.currentCollider = _currentCollider;
            _context.horizontalHit = _currentHorizontalHit;
            _context.topHit = _currentTopHit;
            _context.input = _localCoordMove;
        }


        public void FinishAfterAnimation(string animationState, Vector3 targetMatchPosition, Quaternion targetMatchRotation, float targetNormalizedTime = 0.9f)
        {
            _animationStateToWait = animationState;
            _waitingAnimation = true;

            _capsule.DisableCollision();

            _matchTarget = true;
            _matchTargetPosition = targetMatchPosition;
            _matchTargetRotation = targetMatchRotation;
            _targetNormalizedTime = targetNormalizedTime;
        }
        public void FinishAfterAnimation(string animationState, float targetNormalizedTime = 0.9f)
        {
            FinishAfterAnimation(animationState, Vector3.zero, Quaternion.identity, targetNormalizedTime);
            _matchTarget = false;
        }
                
        private void ProccesMovement()
        {
            Vector3 CamForward = Vector3.Scale(_mainCamera.transform.forward, new Vector3(1, 0, 1));
            Vector3 cameraRelativeMove = _action.move.x * _mainCamera.transform.right + _action.move.y * CamForward;
            cameraRelativeMove.Normalize();

            _localCoordMove.x = Vector3.Dot(cameraRelativeMove, transform.right);
            _localCoordMove.y = Vector3.Dot(cameraRelativeMove, transform.forward);
        }

        private void ProccessInput()
        {
            if (_action.jump)
            {
                if(Mathf.Approximately(_localCoordMove.x, 0) || _localCoordMove.y > 0.5f)
                    ClimbUp();

                if (_localCoordMove != Vector2.zero)
                    Jump();
            }

            if (_action.drop)
                Drop();
        }

        /// <summary>
        /// This function disable logic that set character position on ledge
        /// </summary>
        public void DisableTransformUpdate()
        {
            _updateTransform = false;
        }

        /// <summary>
        /// Allow logic to set character position on ledge
        /// </summary>
        public void EnableTransformUpdate()
        {
            _updateTransform = true; 
            
            _hangWeight = 0;
            _animator.SetFloat("HangWeight", _hangWeight);
        }

        public void DoTween(Vector3 targetPosition, Quaternion targetRotation, float duration, Collider targetLedge)
        {
            DoTween(targetPosition, targetRotation, duration, defaultMatchingCurve, targetLedge);
        }

        public void DoTween(Vector3 targetPosition, Quaternion targetRotation, float duration, AnimationCurve curve, Collider targetLedge)
        {
            // set target
            _tweenTarget.parent = targetLedge != null ? targetLedge.transform : null;

            // set base parameters for tween
            _isDoingTween = true;
            _currentTweenWeight = 0;
            _tweenDuration = duration;

            // set position paramters
            _tweenStartPosition = transform.position;
            _tweenTarget.position = targetPosition;

            // set rotation parameters
            _tweenStartRotation = transform.rotation;
            _tweenTarget.rotation = targetRotation;

            // set time control parameters
            _tweenStartTime = Time.time;
            _tweenStep = 1 / duration;

            // set curve
            _targetCurve = curve;

            // calculate bezier point
            Quaternion midRot = Quaternion.Lerp(_tweenStartRotation, _tweenTarget.rotation, 0.5f);
            Vector3 forward = midRot * Vector3.forward;
            _tweenBezierPoint = Vector3.Lerp(_tweenStartPosition, _tweenTarget.position, 0.5f) - forward;

            // stops root motion
            _mover.StopRootMotion();
        }

        private void UpdateTween()
        {
            if (!_isDoingTween) return;

            if (Time.time - _tweenStartTime > _tweenDuration + 0.1f || Mathf.Approximately(_currentTweenWeight, 1f))
            {
                if(_tweenTarget.position != Vector3.zero)
                    _mover.SetPosition(_tweenTarget.position);

                transform.rotation = _tweenTarget.rotation;

                _targetClimbCharPos.parent = _tweenTarget.parent;
                _targetClimbCharPos.position = _tweenTarget.position;
                _targetClimbCharPos.rotation = _tweenTarget.rotation;
                _lastHitPoint = _tweenTarget.position;

                _isDoingTween = false;
                return;
            }

            _currentTweenWeight = Mathf.MoveTowards(_currentTweenWeight, 1, _tweenStep * Time.deltaTime);

            float weight = _targetCurve.Evaluate(_currentTweenWeight);

            if (_tweenTarget.position != Vector3.zero)
            {
                if (Quaternion.Dot(_tweenStartRotation, _tweenTarget.rotation) > 0.85f)
                    _mover.SetPosition(Vector3.Lerp(_tweenStartPosition, _tweenTarget.position, weight));
                else
                    _mover.SetPosition(BezierLerp(_tweenStartPosition, _tweenTarget.position, _tweenBezierPoint, weight));
            }

            transform.rotation = Quaternion.Lerp(_tweenStartRotation, _tweenTarget.rotation, weight);
        }

        public Vector3 BezierLerp(Vector3 start, Vector3 end, Vector3 bezier, float t)
        {
            Vector3 point = Mathf.Pow(1 - t, 2) * start;
            point += 2 * (1 - t) * t * bezier;
            point += t * t * end;

            return point;
        }

        // blocks current time to be climbed during 1 second
        public void BlockCurrentLedge()
        {
            _ledgeBlocked = _currentCollider;
            _timeBlockStarted = Time.time;
        }

        public bool PositionFreeToClimb(RaycastHit horHit, RaycastHit topHit)
        {
            Vector3 targetCharacterPosition = GetCharacterPositionOnLedge(horHit, topHit);

            Vector3 bot = targetCharacterPosition + Vector3.up * _capsule.GetCapsuleRadius();
            Vector3 top = targetCharacterPosition + Vector3.up * (_capsule.GetCapsuleHeight() - _capsule.GetCapsuleRadius());

            if (Physics.OverlapCapsule(bot, top, _capsule.GetCapsuleRadius(), obstacleMask, QueryTriggerInteraction.Ignore).Length > 0)
                return false;

            return true;
        }

        private void SetCharacterPosition()
        {
            if (_isDoingTween || !_updateTransform) return;

            _mover.SetPosition(GetCharacterPositionOnLedge());
            transform.rotation = GetCharacterRotationOnLedge();
        }

        private Vector3 GetCharacterPositionOnLedge()
        {
            Vector3 normal = _climbTargetHit.forward;
            normal.y = 0;
            normal.Normalize();

            return _climbTargetHit.position + Vector3.up * offsetOnLedge.y + normal * offsetOnLedge.x;
        }

        public Vector3 GetCharacterPositionOnLedge(RaycastHit horHit, RaycastHit topHit)
        {
            Vector3 normal = horHit.normal;
            normal.y = 0;
            normal.Normalize();

           return topHit.point + Vector3.up * offsetOnLedge.y + normal * offsetOnLedge.x;
        }

        private Quaternion GetCharacterRotationOnLedge()
        {
            Vector3 normal = _climbTargetHit.forward;
            normal.y = 0;
            normal.Normalize();

            return Quaternion.LookRotation(-normal);
        }

        public Quaternion GetCharacterRotationOnLedge(RaycastHit horHit)
        {
            Vector3 normal = horHit.normal;
            normal.y = 0;
            normal.Normalize();

            return Quaternion.LookRotation(-normal);
        }

        public void ResetClimbVars()
        {
            _currentHorizontalHit = new RaycastHit();
            _currentTopHit = new RaycastHit();
            _currentCollider = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            if(grabReference != null && !IsAbilityRunning)
                Gizmos.DrawWireSphere(grabReference.position, globalRadiusDetection);
        }

        public void DrawSphere(Vector3 center, float radius, Color color, float duration = 0)
        {
            if (_debug)
                _debug.DrawSphere(center, radius, color, duration);
        }

        public void DrawCapsule(Vector3 p1, Vector3 p2, float radius, Color color, float duration = 0)
        {
            if (_debug)
                _debug.DrawCapsule(p1, p2, radius, color, duration);
        }

        public void DrawLabel(string text, Vector3 position, Color color, float duration = 0)
        {
            if (_debug)
                _debug.DrawLabel(text, position, color, duration);
        }
    }

}