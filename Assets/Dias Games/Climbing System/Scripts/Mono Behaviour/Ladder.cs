using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.Climbing
{
    public class Ladder : MonoBehaviour
    {
        [Header("Ladder transform references")]
        [Tooltip("This transform is used to set character position on ladder and also set which direction to climb")]
        [SerializeField] private Transform climbDirection;
        [Tooltip("This transform is the limit on top. Character can't climb up over this. " +
            "If find a ground after reach here, character will finish ladder and climb")]
        [SerializeField] private Transform topLimit;
        [Tooltip("This transform is the limit on bottom. Character can't climb down under this")]
        [SerializeField] private Transform bottomLimit;
        [Space]
        [SerializeField] private bool canClimbOnTop;

        public Transform TopLimit { get { return topLimit; } }
        public Transform BottomLimit { get { return bottomLimit; } }
        public Transform PositionAndDirection { get { return climbDirection; } }
        public bool CanClimbTop { get { return canClimbOnTop; } }

    }
}