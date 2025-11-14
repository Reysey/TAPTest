using System.Collections; 
using UnityEngine;
using Core;

namespace Game
{
    public class CharacterStats : MonoBehaviour
    {
        [SerializeField] private CharacterStatsSO statsSO;

        public int MaxHp => statsSO.maxHP;
        public int Attack => statsSO.attack;
        public int Defense => statsSO.defense;

        public int CurrentHp { get; private set; }

        [SerializeField] private bool isPlayer;
        [SerializeField] private bool isEnemy;

        [Header("Hit Flash")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float hitFlashDuration = 0.15f;
        
        private Color _originalColor;
        private Coroutine _flashCoroutine;
        
        private bool _isDead;
       
        
        private void Awake()
        {
            
            // If not assigned, try to find a SpriteRenderer on this object or its children
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                _originalColor = spriteRenderer.color;
            
            CurrentHp = MaxHp;

            if (isPlayer)
            {
                GameEvents.RaisePlayerHpChanged(CurrentHp, MaxHp);
            }
            else if (isEnemy)
            {
                GameEvents.RaiseEnemyHpChanged(CurrentHp, MaxHp);
            }
        }

        public void TakeDamage(int rawDamage)
        {
            if (_isDead) return;
            
            int damage = Mathf.Max(0, rawDamage - Defense);
            if (damage <= 0) return;

            CurrentHp = Mathf.Max(0, CurrentHp - damage);

            GameEvents.RaiseDamageDealt(damage);

            if (isPlayer)
            {
                GameEvents.RaisePlayerHpChanged(CurrentHp, MaxHp);
                if (CurrentHp <= 0) Die();
            }
            else if (isEnemy)
            {
                // Trigger color flash on enemy
                TriggerHitFlash();
                
                GameEvents.RaiseEnemyHpChanged(CurrentHp, MaxHp);
                if (CurrentHp <= 0)
                {
                    GameEvents.RaiseEnemyDefeated();
                    Die();
                }
            }
        }
        
        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            Destroy(gameObject); // instant removal
        }
        
        private void TriggerHitFlash()
        {
            if (spriteRenderer == null)
                return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);

            _flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            spriteRenderer.color = hitColor;
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = _originalColor;
            _flashCoroutine = null;
        }
    }
}