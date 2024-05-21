using System;
using UnityEngine;
using UnityEngine.Events;

namespace DiasGames.Components
{
    public class Health : MonoBehaviour, IDamage
    {
        [SerializeField] private int MaxHealthPoints = 100;
        [Space]
        [SerializeField] private UnityEvent OnCharacterDeath;

        // internal vars
        private int _currentHP = 100;

        public int CurrentHP { get { return _currentHP; } }
        public int MaxHP { get { return MaxHealthPoints; } }

        public event Action OnHealthChanged;
        public event Action OnDead;

        private void Start()
        {
            _currentHP = MaxHealthPoints;
            OnHealthChanged?.Invoke();
        }

        public void Damage(int damagePoints)
        {
            _currentHP -= damagePoints;

            if (_currentHP <= 0)
            {
                _currentHP = 0;
                OnDead?.Invoke();
                OnCharacterDeath.Invoke();
            }

            OnHealthChanged?.Invoke();
        }

        /// <summary>
        /// Restore an amount of health points
        /// </summary>
        /// <param name="hp">Health points</param>
        public void RestoreHealth(int hp)
        {
            _currentHP += hp;
            if (_currentHP > MaxHealthPoints)
                _currentHP = MaxHealthPoints;

            OnHealthChanged?.Invoke();
        }

        /// <summary>
        /// Restores all character health
        /// </summary>
        public void RestoreFullHealth()
        {
            _currentHP = MaxHealthPoints;

            OnHealthChanged?.Invoke();
        }
    }
}