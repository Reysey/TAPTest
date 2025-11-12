using UnityEngine;
using UnityEngine.InputSystem;
using Game;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;
    
    [Header("Combat")]
    [SerializeField] private CharacterStats heroStats;
    [SerializeField] private CharacterStats enemyStats;
    [SerializeField] private float attackRange = 1.5f;

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Vector2 _lastMoveDir = Vector2.down;
    
    [Header("FX 2D")]
    [SerializeField] private ParticleSystem attackFx;
    [SerializeField] private float attackFxOffset = 0.5f; 

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Apply movement
        Vector2 velocity = _moveInput * moveSpeed;
        _rb.linearVelocity = velocity;

        // Determine if player is moving
        bool isMoving = _moveInput.sqrMagnitude > 0.0001f;

        // Update last move direction if moving
        if (isMoving)
        {
            _lastMoveDir = _moveInput.normalized;
        }

        // If we have an animator, update parameters
        if (animator)
        {
            animator.SetBool("IsMoving", isMoving);
            animator.SetFloat("MoveX", _lastMoveDir.x);
            animator.SetFloat("MoveY", _lastMoveDir.y);
        }
    }

    // Called by PlayerInput → Move event
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (heroStats == null || enemyStats == null)
        {
            // Debug.LogWarning("Attack pressed but stats refs not set.");
            return;
        }

        // Simple range check (optional but nicer)
        float distance = Vector2.Distance(transform.position, enemyStats.transform.position);
        if (distance > attackRange)
        {
            Debug.Log("Enemy out of range.");
            return;
        }
        
        // Determine facing; if never moved, default down
        Vector2 dir = _lastMoveDir.sqrMagnitude > 0.0001f ? _lastMoveDir.normalized : Vector2.down;
        
        // Play attack animation
        if (attackFx != null)
        {
            attackFx.transform.position = transform.position + (Vector3)(dir * attackFxOffset);
            attackFx.Play();
        }
        
        // Deal damage
        enemyStats.TakeDamage(heroStats.Attack);

        // Optional: trigger attack animation
        // if (animator != null)  animator.SetTrigger("Attack");
    }
}
