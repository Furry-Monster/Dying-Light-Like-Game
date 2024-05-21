using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.Climbing
{
    [System.Serializable]
    public abstract class ClimbStateBase
    {
        public virtual void Idle(ClimbStateContext context) { }
        public virtual void ClimbUp(ClimbStateContext context) { }
        public virtual void Drop(ClimbStateContext context) { }
        public virtual void Jump(ClimbStateContext context) { }
        public virtual void CornerOut(ClimbStateContext context) { }
        public virtual void CornerIn(ClimbStateContext context) { }

        public abstract void EnterState(ClimbStateContext context);
        public abstract void ExitState(ClimbStateContext context);
    }
}