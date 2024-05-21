using UnityEngine;
using DiasGames.Components;

namespace DiasGames.Abilities
{
    public class Strafe : AbstractAbility
    {
        [SerializeField] private float strafeWalkSpeed = 2f;

        [Header("Animation")]
        [SerializeField] private string strafeAnimState = "Strafe";
        [SerializeField] private string horizontalAnimFloat = "Horizontal";
        [SerializeField] private string verticalAnimFloat = "Vertical";

        private IMover _mover = null;
        private GameObject _camera = null;

        private int _animHorizontalID;
        private int _animVerticalID;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _camera = Camera.main.gameObject;

            _animHorizontalID = Animator.StringToHash(horizontalAnimFloat);
            _animVerticalID = Animator.StringToHash(verticalAnimFloat);
        }


        public override bool ReadyToRun()
        {
            return _mover.IsGrounded() && _action.zoom;
        }

        public override void OnStartAbility()
        {
            SetAnimationState(strafeAnimState);
        }

        public override void UpdateAbility()
        {
            _mover.Move(_action.move, strafeWalkSpeed, false);
            transform.rotation = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0);

            // update animator
            _animator.SetFloat(_animHorizontalID, _action.move.x, 0.1f, Time.deltaTime);
            _animator.SetFloat(_animVerticalID, _action.move.y, 0.1f, Time.deltaTime);

            if (!_action.zoom || !_mover.IsGrounded())
                StopAbility();
        }
    }
}