using UnityEngine;
using UnityEngine.UI;
using Core;
using TMPro;


namespace UI
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text enemyHpText;

        private void OnEnable()
        {
            GameEvents.OnPlayerHpChanged += HandlePlayerHpChanged;
            GameEvents.OnEnemyHpChanged += HandleEnemyHpChanged;
            GameEvents.OnEnemyDefeated += HandleEnemyDefeated;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHpChanged -= HandlePlayerHpChanged;
            GameEvents.OnEnemyHpChanged -= HandleEnemyHpChanged;
            GameEvents.OnEnemyDefeated -= HandleEnemyDefeated;
        }

        private void HandlePlayerHpChanged(int current, int max)
        {
            if (playerHpText != null)
                playerHpText.text = $"Player HP: {current}/{max}";
        }

        private void HandleEnemyHpChanged(int current, int max)
        {
            if (enemyHpText != null)
                enemyHpText.text = $"Enemy HP: {current}/{max}";
        }

        private void HandleEnemyDefeated()
        {
            if (enemyHpText != null)
                enemyHpText.text += " (Defeated)";
        }
    }
}