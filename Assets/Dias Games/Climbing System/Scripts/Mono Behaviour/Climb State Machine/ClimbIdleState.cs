using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.Climbing
{
    [System.Serializable]
    public class ClimbIdleState : ClimbStateBase
    {
        public string climbIdleState = "Climb.Brace Idle";

        private float _exitTime;

        public override void Idle(ClimbStateContext context)
        {
        }


        public override void ClimbUp(ClimbStateContext context)
        {
            context.SetState(context.ClimbUp);
        }

        public override void Drop(ClimbStateContext context)
        {
            context.SetState(context.ClimbDrop);
        }

        public override void Jump(ClimbStateContext context)
        {
            context.SetState(context.ClimbJump);
        }

        public override void CornerOut(ClimbStateContext context)
        {
            context.SetState(context.CornerOut);
        }

        public override void CornerIn(ClimbStateContext context)
        {
            context.SetState(context.CornerIn);
        }

        public override void EnterState(ClimbStateContext context)
        {
            if(Time.time - _exitTime > 0.1f)
                context.animator.CrossFadeInFixedTime(climbIdleState, 0.15f);
        }

        public override void ExitState(ClimbStateContext context)
        {
            _exitTime = Time.time;
        }
    }
}