using System;
using UnityEngine;

namespace Core
{
    public static class GameEvents
    {
        // Raised when any damage is dealt
        public static event Action<int> OnDamageDealt;

        // Player HP change
        public static event Action<int, int> OnPlayerHpChanged;

        // Enemy HP change
        public static event Action<int, int> OnEnemyHpChanged;

        // Enemy defeated
        public static event Action OnEnemyDefeated;

        public static void RaiseDamageDealt(int amount)
        {
            OnDamageDealt?.Invoke(amount);
        }

        public static void RaisePlayerHpChanged(int current, int max)
        {
            OnPlayerHpChanged?.Invoke(current, max);
        }

        public static void RaiseEnemyHpChanged(int current, int max)
        {
            OnEnemyHpChanged?.Invoke(current, max);
        }

        public static void RaiseEnemyDefeated()
        {
            OnEnemyDefeated?.Invoke();
        }
    }
}