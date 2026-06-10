using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(AwakeningComponent))]
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerAnimator _playerAnimator;
    private PlayerMovement _playerMovement;
    private PlayerAttack _playerAttack;
    private HealthComponent _healthComponent;
    private AwakeningComponent _awakeningComponent;

    public static event System.Action OnSettingsToggle;

    public Vector2 MoveInput { get; private set; }

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerAttack = GetComponent<PlayerAttack>();
        _healthComponent = GetComponent<HealthComponent>();
        _awakeningComponent = GetComponent<AwakeningComponent>();
    }

    private void Update()
    {
        HandleMoveInput();
    }

    private bool CanAction
    {
        get
        {
            if (GameStateManager.Instance == null) return false;
            if (!GameStateManager.Instance.IsPlaying) return false;
            if (_healthComponent.IsDead) return false;
            if (_healthComponent.IsInvincible) return false;
            return true;
        }
    }

    private void HandleMoveInput()
    {
        if (!CanAction)
        {
            MoveInput = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector2 input;
        input.x = horizontal;
        input.y = vertical;
        MoveInput = input.normalized;
    }

    public void OnEvade(InputAction.CallbackContext context)
    {
        if (!CanAction) return;

        if (context.performed)
        {
            _playerAttack.ResetAttackState();
            _playerAnimator.PlayEvadeAnimation();
            _playerMovement.Evade();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!CanAction) return;

        _playerAttack.TryAttack();
    }

    public void OnSkill(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!CanAction) return;

        _playerAttack.UseSkill();
    }

    public void OnUltimate(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!CanAction) return;

        _playerAttack.UseUltimate();
    }

    public void OnAwakening(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!CanAction) return;

        if (_awakeningComponent.CanActivate())
        {
            _playerAttack.ResetAttackState();
            _playerAnimator.PlayAwakeningAnimation();
        }
    }

    public void OnSettings(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.IsReady) return;
        if (GameStateManager.Instance.IsGameOver) return;

        OnSettingsToggle?.Invoke();
    }
}
