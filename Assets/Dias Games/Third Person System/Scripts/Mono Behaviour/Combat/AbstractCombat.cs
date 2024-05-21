using System;
using System.Collections.Generic;
using UnityEngine;
using DiasGames.Abilities;

namespace DiasGames.Combat
{
    public abstract class AbstractCombat : MonoBehaviour
    {
        [SerializeField] private AbstractAbility[] AllowedAbilities;

        public bool IsCombatRunning { get; protected set; } = false;
        protected CharacterActions _action = new CharacterActions();
        protected AbstractAbility _abilityRunning = null;

        public event Action OnCombatStop = null;

        protected float startedTime = 0;
        protected float stoppedTime = 0;

        public abstract bool CombatReadyToRun();

        public abstract void UpdateCombat();

        public abstract void OnStartCombat();
        public abstract void OnStopCombat();


        public void StartCombat()
        {
            IsCombatRunning = true;
            OnStartCombat();

            startedTime = Time.time;
        }

        public void StopCombat()
        {
            stoppedTime = Time.time;

            IsCombatRunning = false;
            OnStopCombat();
            _abilityRunning = null;
            OnCombatStop?.Invoke();
        }


        public void SetActionReference(ref CharacterActions action)
        {
            _action = action;
        }

        public bool IsAbilityAllowed(AbstractAbility abilityToCheck)
        {
            foreach(AbstractAbility ability in AllowedAbilities)
            {
                if(ability == abilityToCheck)
                    return true;
            }

            return false;
        }

        public void SetCurrentAbility(AbstractAbility newAbility)
        {
            _abilityRunning = newAbility;
        }
    }
}