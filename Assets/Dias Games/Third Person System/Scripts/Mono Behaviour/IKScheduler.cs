using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.IK
{
    public class IKScheduler : MonoBehaviour
    {
        private Animator _animator = null;
        private List<IKPass> _ikPassList = new List<IKPass>();

        [SerializeField] private float IKSmoothTime = 0.12f;

        public bool _applyIK = true;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            // update weight for ik
            foreach (IKPass currentIK in _ikPassList)
            {
                currentIK.UpdateWeight(IKSmoothTime);
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            // only apply IK if it was asked to apply
            if (_ikPassList.Count == 0 || !_applyIK) return;

            // only appply IK on base layer
            if (layerIndex != 0) return;

            foreach (IKPass currentIK in _ikPassList)
            {
                if (currentIK.weight < 0.1f) continue;

                _animator.SetIKPositionWeight(currentIK.ikGoal, currentIK.weight * currentIK.positionWeight);
                _animator.SetIKRotationWeight(currentIK.ikGoal, currentIK.weight * currentIK.rotationWeight);

                _animator.SetIKPosition(currentIK.ikGoal, currentIK.position);
                _animator.SetIKRotation(currentIK.ikGoal, currentIK.rotation);
            }
        }

        /// <summary>
        /// Ask system to apply IK
        /// </summary>
        /// <param name="ikPass"></param>
        public void ApplyIK(IKPass ikPass)
        {
            // check if this IK is already in the list
            IKPass currentPass = _ikPassList.Find(x => x.ikGoal == ikPass.ikGoal);
            if (currentPass == null)
            {
                currentPass = new IKPass(ikPass);

                // add current pass to the list
                _ikPassList.Add(currentPass);
            }
            else
                currentPass.CopyParameters(ikPass);

            currentPass.targetWeight = 1;
        }

        /// <summary>
        /// Tells system to stop IK
        /// </summary>
        public void StopIK(AvatarIKGoal goal)
        {
            IKPass currentPass = _ikPassList.Find(x => x.ikGoal == goal);

            if (currentPass == null) return;

            currentPass.targetWeight = 0;
        }
    }

    public class IKPass
    {
        public Vector3 position;
        public Quaternion rotation;
        public AvatarIKGoal ikGoal;
        public float weight;
        public float positionWeight;
        public float rotationWeight;

        public bool isStartingIK { get; private set; } = false; 
        public bool isStoppingIK { get; private set; } = false;

        private float _vel;
        public float targetWeight;

        public IKPass(Vector3 targetPosition, Quaternion targetRotation, AvatarIKGoal goal, float positionWeight, float rotationWeight)
        {
            position = targetPosition;
            rotation = targetRotation;
            ikGoal = goal;
            this.positionWeight = positionWeight;
            this.rotationWeight = rotationWeight;

            weight = 0;
            isStartingIK = false;
        }

        public IKPass(IKPass reference)
        {
            position = reference.position;
            rotation = reference.rotation;
            ikGoal = reference.ikGoal;
            positionWeight = reference.positionWeight;
            rotationWeight = reference.rotationWeight;

            weight = 0;
            isStartingIK = false;
        }

        public void CopyParameters(IKPass instanceToCopy)
        {
            position = instanceToCopy.position;
            rotation = instanceToCopy.rotation;
            positionWeight = instanceToCopy.positionWeight;
            rotationWeight = instanceToCopy.rotationWeight;
        }

        public void UpdateWeight(float smoothTime)
        {
            weight = Mathf.SmoothDamp(weight, targetWeight, ref _vel, smoothTime);
        }
    }
}