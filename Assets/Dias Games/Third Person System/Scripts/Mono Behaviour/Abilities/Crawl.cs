using UnityEngine;
using DiasGames.Components;

namespace DiasGames.Abilities
{
    public class Crawl : AbstractAbility
    {
        [SerializeField] private float crawlSpeed = 2f;
        [SerializeField] private float capsuleHeightOnCrawl = 0.5f;

        [Header("Cast Parameters")]
        [SerializeField] private LayerMask obstaclesMask;
        [Tooltip("This is the height that sphere cast can reach to know when should force crawl state")]
        [SerializeField] private float MaxHeightToStartCrawl = 0.75f;

        [Header("Animation States")]
        [SerializeField] private string startCrawlAnimationState = "Stand to Crawl";
        [SerializeField] private string stopCrawlAnimationState = "Crawl to Stand";

        private IMover _mover;
        private ICapsule _capsule;

        private bool _startingCrawl = false;
        private bool _stoppingCrawl = false;

        private float _defaultCapsuleRadius = 0;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();

            _defaultCapsuleRadius = _capsule.GetCapsuleRadius();
        }

        public override bool ReadyToRun()
        {
            if (!_mover.IsGrounded()) return false;

            if (_action.crawl || ForceCrawlByHeight())
                return true;

            return false;
        }


        public override void OnStartAbility()
        {
            // stop any movement
            _mover.StopMovement();

            // Tells system that it's starting crawl
            _startingCrawl = true;

            // set crawl animation
            SetAnimationState(startCrawlAnimationState);

            // resize capsule collider
            _capsule.SetCapsuleSize(capsuleHeightOnCrawl, _capsule.GetCapsuleRadius());
        }


        public override void UpdateAbility()
        {
            // wait start crawl animation finishes
            if (_startingCrawl)
            {
                if (_animator.IsInTransition(0)) return;

                if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(startCrawlAnimationState))
                    _startingCrawl = false;

                return;
            }

            // wait stop crawl finishes to stop this ability
            if (_stoppingCrawl)
            {
                if (_animator.IsInTransition(0)) return;

                if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.85f)
                    StopAbility();

                return;
            }

            _mover.Move(_action.move, crawlSpeed);

            // if crawl was true again, it means should stop ability
            if (_action.crawl && !ForceCrawlByHeight())
            {
                SetAnimationState(stopCrawlAnimationState);
                _stoppingCrawl = true;
                _mover.StopMovement();
            }
        }

        public override void OnStopAbility()
        {
            // reset control variables
            _startingCrawl = false;
            _stoppingCrawl = false;

            // reset capsule size
            _capsule.ResetCapsuleSize();
        }

        private bool ForceCrawlByHeight()
        {
            RaycastHit hit;

            if (Physics.SphereCast(transform.position, _defaultCapsuleRadius, Vector3.up, out hit,
                MaxHeightToStartCrawl, obstaclesMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.point.y - transform.position.y > capsuleHeightOnCrawl)
                    return true;
            }

            return false;
        }
    }
}