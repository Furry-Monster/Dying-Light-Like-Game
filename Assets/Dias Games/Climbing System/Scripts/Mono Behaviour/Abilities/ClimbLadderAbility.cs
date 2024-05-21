using UnityEngine;
using DiasGames.Components;
using DiasGames.Climbing;

namespace DiasGames.Abilities
{
    public class ClimbLadderAbility : AbstractAbility
    {
        [Header("Overlap")]
        [SerializeField] private LayerMask ladderMask;
        [SerializeField] private Transform grabReference;
        [SerializeField] private float overlapRange = 1f;
        [Header("Animation")]
        [SerializeField] private string climbLadderAnimState = "Ladder";
        [SerializeField] private string climbUpAnimState = "Climb.Climb up";
        [SerializeField] private string ladderAnimFloat = "Vertical";
        [Header("Movement")]
        [SerializeField] private float climbSpeed = 1.2f;
        [SerializeField] private float charOffset = 0.3f;
        [SerializeField] private float smoothnessTime = 0.12f;

        private IMover _mover;
        private ICapsule _capsule;

        private Ladder _currentLadder;
        private Ladder _blockedLadder;
        private float _blockedTime;

        private bool _stopDown;
        private bool _stopUp;
        private bool _climbingUp;

        // parameters to set smooth position and rotation on ladder
        private Vector3 _startPosition, _targetPosition;
        private Quaternion _startRotation, _targetRotation;
        private float _step;
        private float _weight;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();
        }

        public override bool ReadyToRun()
        {
            return !_mover.IsGrounded() && FoundLadder();
        }

        public override void OnStartAbility()
        {
            _animator.CrossFadeInFixedTime(climbLadderAnimState, 0.1f);
            _mover.DisableGravity();

            // setting position
            _weight = 0;
            _step = 1 / smoothnessTime;
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _targetPosition = GetCharPosition();
            _targetRotation = GetCharRotation();

            // control vars
            _stopDown = false;
            _stopUp = false;
            _climbingUp = false;
        }

        public override void UpdateAbility()
        {
            // call when ability enter and need to set character position and rotation
            if (!Mathf.Approximately(_weight, 1f))
            {
                UpdatePositionOnLadder();
                return;
            }

            // if climb up on top of ladder, wait animation ends
            if (_climbingUp)
            {
                if (_animator.IsInTransition(0)) return;

                var state = _animator.GetCurrentAnimatorStateInfo(0);
                var normalizedTime = Mathf.Repeat(state.normalizedTime,1f);
                if (normalizedTime > 0.95f)
                    StopAbility();

                return;
            }

            // vertical parameter to move
            float vertical = _action.move.y;

            // check limits
            CheckVerticalLimits();

            // stop down movement if reach limit
            if (_stopDown && vertical < 0) vertical = 0;

            // check if reach top limit
            if (_stopUp && vertical > 0)
            {
                // climb up
                if (_currentLadder.CanClimbTop)
                {
                    _animator.CrossFadeInFixedTime(climbUpAnimState, 0.1f);
                    _mover.ApplyRootMotion(Vector3.one);
                    _capsule.DisableCollision();
                    _climbingUp = true;
                    return;
                }

                // stop up climbing on ladder after reach top
                vertical = 0;
            }

            // set climb ladder animation float
            _animator.SetFloat(ladderAnimFloat, vertical, 0.1f, Time.deltaTime);
            // move character up or down
            _mover.Move(Vector3.up * vertical * climbSpeed);

            // if climbing down and find ground, finish ability
            if (_action.move.y < 0f && _mover.IsGrounded())
                StopAbility();

            // drop from ladder
            if (_action.drop)
            {
                StopAbility();
                BlockLadder();
            }
        }

        public override void OnStopAbility()
        {
            _mover.EnableGravity();
            _capsule.EnableCollision();
            _mover.StopRootMotion();
        }

        /// <summary>
        /// Do smooth transition to set position and rotation
        /// </summary>
        private void UpdatePositionOnLadder()
        {
            _weight = Mathf.MoveTowards(_weight, 1f, _step * Time.deltaTime);
            _mover.SetPosition(Vector3.Lerp(_startPosition, _targetPosition, _weight));
            transform.rotation = Quaternion.Lerp(_startRotation, _targetRotation, _weight);
        }

        /// <summary>
        /// Block current ladder when dropping
        /// </summary>
        private void BlockLadder()
        {
            _blockedLadder = _currentLadder;
            _blockedTime = Time.time;
        }

        /// <summary>
        /// check vertical limits of ladder to avoid climb beyond top or bottom
        /// </summary>
        private void CheckVerticalLimits()
        {
            if (transform.position.y < _currentLadder.BottomLimit.position.y)
                _stopDown = true;
            else if (transform.position.y > _currentLadder.BottomLimit.position.y + 0.15f)
                _stopDown = false;

            if (transform.position.y + _capsule.GetCapsuleHeight() > _currentLadder.TopLimit.position.y)
                _stopUp = true;
            else if (transform.position.y + _capsule.GetCapsuleHeight() < _currentLadder.TopLimit.position.y - 0.15f)
                _stopUp = false;
        }

        private bool FoundLadder()
        {
            var overlaps = Physics.OverlapSphere(grabReference.position, overlapRange, ladderMask, QueryTriggerInteraction.Collide);

            // loop through all overlaps
            foreach(var coll in overlaps)
            {
                if(coll.TryGetComponent(out Ladder ladder))
                {
                    if (ladder == _blockedLadder && Time.time - _blockedTime < 2f)
                        continue;

                    if (CanGrab(ladder))
                    {
                        _currentLadder = ladder;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// check if character can grab ladder from its current position and rotation
        /// </summary>
        /// <param name="character"></param>
        /// <param name="characterHeight"></param>
        /// <returns></returns>
        public bool CanGrab(Ladder ladder)
        {
            // can't grab if character is not looking on ladder
            if (Vector3.Dot(transform.forward, -ladder.PositionAndDirection.forward) < -0.1f) return false;

            // check bottom
            if (transform.position.y < ladder.BottomLimit.position.y - 0.15f) return false;

            // check top
            if (transform.position.y + _capsule.GetCapsuleHeight() > ladder.TopLimit.position.y + 0.15f) return false;

            return true;
        }

        /// <summary>
        /// get target character position on this ladder
        /// </summary>
        /// <param name="character"></param>
        /// <param name="characterHeight"></param>
        /// <returns></returns>
        public Vector3 GetCharPosition()
        {
            Vector3 position = _currentLadder.PositionAndDirection.position + _currentLadder.PositionAndDirection.forward * charOffset;
            position.y = transform.position.y;

            if (position.y < _currentLadder.BottomLimit.position.y)
                position.y = _currentLadder.BottomLimit.position.y;

            float height = _capsule.GetCapsuleHeight();
            if (position.y + height > _currentLadder.TopLimit.position.y)
                position.y = _currentLadder.TopLimit.position.y - height;

            return position;
        }

        public Quaternion GetCharRotation()
        {
            return Quaternion.LookRotation(-_currentLadder.PositionAndDirection.forward);
        }
    }
}