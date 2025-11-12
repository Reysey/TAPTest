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

        [Header("Health Bars")]
        [SerializeField] private Image playerHpFill;
        [SerializeField] private Image enemyHpFill;
        
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
            
            if (playerHpFill != null)
                playerHpFill.fillAmount = (max > 0) ? (float)current / max : 0f;
        }

        private void HandleEnemyHpChanged(int current, int max)
        {
            if (enemyHpText != null)
                enemyHpText.text = $"Enemy HP: {current}/{max}";
            
            if (enemyHpFill != null)
                enemyHpFill.fillAmount = (max > 0) ? (float)current / max : 0f;
        }

        private void HandleEnemyDefeated()
        {
            if (enemyHpText != null)
                enemyHpText.text += " (Defeated)";
        }
    }
}