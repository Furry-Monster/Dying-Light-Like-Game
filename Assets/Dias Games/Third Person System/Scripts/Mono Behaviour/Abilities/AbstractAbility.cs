using System;
using UnityEngine;

namespace DiasGames.Abilities
{
    public abstract class AbstractAbility : MonoBehaviour
    {
        [SerializeField] private int abilityPriority = 0;

        public bool IsAbilityRunning { get; private set; }

        public event Action<AbstractAbility> abilityStopped = null;
        public event Action<AbstractAbility> abilityStarted = null;

        public int AbilityPriority { get { return abilityPriority; } }

        // Unity components
        protected Animator _animator = null;

        // start time and stop time
        public float StartTime { get; private set; } = 0;
        public float StopTime { get; private set; } = 0;

        // actions reference
        protected CharacterActions _action;

        /// <summary>
        /// Set reference to get actions for character (for input)
        /// </summary>
        /// <param name="newAction"></param>
        public void SetActionReference(ref CharacterActions newAction)
        {
            _action = newAction;
        }

        protected virtual void Start()
        {
            _animator = GetComponent<Animator>();
        }

        public void StartAbility()
        {
            IsAbilityRunning = true;
            StartTime = Time.time;
            OnStartAbility();
            abilityStarted?.Invoke(this);
        }
        public void StopAbility()
        {
            if (Time.time - StartTime < 0.1f)
                return;

            IsAbilityRunning = false;
            StopTime = Time.time;
            OnStopAbility();
            abilityStopped?.Invoke(this);
        }

        public abstract bool ReadyToRun();

        public abstract void OnStartAbility();

        public abstract void UpdateAbility();

        public virtual void OnStopAbility() { }


        protected void SetAnimationState(string stateName, float transitionDuration = 0.1f)
        {
            if (_animator.HasState(0, Animator.StringToHash(stateName)))
                _animator.CrossFadeInFixedTime(stateName, transitionDuration, 0);
        }

        /// <summary>
        /// Check if a specific state has finished
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected bool HasFinishedAnimation(string state)
        {
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            if (_animator.IsInTransition(0)) return false;

            if (stateInfo.IsName(state))
            {
                float normalizeTime = Mathf.Repeat(stateInfo.normalizedTime, 1);
                if (normalizeTime >= 0.95f) return true;
            }

            return false;
        }
    }
}