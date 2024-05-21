using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.Climbing
{
    [System.Serializable]
    public class ClimbShimmyState : ClimbStateBase
    {
        public string horizontalFloat = "Horizontal";

        public override void ClimbUp(ClimbStateContext context)
        {
            context.animator.SetFloat(horizontalFloat, 0);
            context.SetState(context.ClimbUp);
        }

        public override void Drop(ClimbStateContext context)
        {
        }

        public override void Idle(ClimbStateContext context)
        {
            context.animator.SetFloat(horizontalFloat, 0);
            context.SetState(context.Idle);
        }


        public override void Jump(ClimbStateContext context)
        {
            context.animator.SetFloat(horizontalFloat, 0);
            context.SetState(context.ClimbJump);
        }

        public override void EnterState(ClimbStateContext context)
        {
            throw new System.NotImplementedException();
        }

        public override void ExitState(ClimbStateContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}