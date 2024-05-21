using UnityEngine;
using DiasGames.Abilities;

namespace DiasGames.Climbing
{
    [System.Serializable]
    public class ClimbStateContext
    {
        public ClimbStateBase CurrentClimbState { get; private set; }

        public ClimbIdleState Idle = new ClimbIdleState();
        public ClimbUpState ClimbUp = new ClimbUpState();
        public ClimbJumpState ClimbJump = new ClimbJumpState();
        public ClimbDropState ClimbDrop = new ClimbDropState();
        public CornerOutState CornerOut = new CornerOutState();
        public CornerInState CornerIn = new CornerInState();

        [HideInInspector] public Animator animator;
        [HideInInspector] public Transform transform;
        [HideInInspector] public Transform grabReference;
        [HideInInspector] public Collider currentCollider;
        [HideInInspector] public RaycastHit horizontalHit;
        [HideInInspector] public RaycastHit topHit;
        [HideInInspector] public Vector2 input;
        [HideInInspector] public ClimbAbility climb;
        [HideInInspector] public ClimbIK ik;

        public void SetState(ClimbStateBase newState)
        {
            if(CurrentClimbState != null)
                CurrentClimbState.ExitState(this);

            CurrentClimbState = newState;

            CurrentClimbState.EnterState(this);
        }
    }
}