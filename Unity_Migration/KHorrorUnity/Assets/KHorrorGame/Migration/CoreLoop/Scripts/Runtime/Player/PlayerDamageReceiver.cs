using System;
using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class PlayerDamageReceiver : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;

        public int MaxHealth
        {
            get { return maxHealth; }
        }

        public int CurrentHealth
        {
            get { return currentHealth; }
        }

        public bool IsDowned
        {
            get { return currentHealth <= 0; }
        }

        public EnemyKind? LastDamageSource { get; private set; }

        private void Awake()
        {
            if (maxHealth <= 0)
            {
                maxHealth = 100;
            }

            if (currentHealth <= 0 || currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }

        public void ResetHealth(int newMaxHealth)
        {
            maxHealth = Math.Max(1, newMaxHealth);
            currentHealth = maxHealth;
            LastDamageSource = null;
        }

        public bool ApplyDamage(int amount, EnemyKind source)
        {
            if (amount <= 0 || IsDowned)
            {
                return false;
            }

            currentHealth = Math.Max(0, currentHealth - amount);
            LastDamageSource = source;
            return true;
        }
    }
}
