using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Single responsibility: receive and store player input each frame.
/// Exposes read-only properties for other components to query.
/// ConsumeFrameInputs() must be called at the end of Update() to reset single-frame inputs.
/// </summary>
public class PlayerInputHandler : MonoBehaviour, Actions.IPlayerActions
{
    private Actions _actions;
    private Actions.PlayerActions _playerActions;
    private InputAction _punchAction;
    private InputAction _spaceAction;

    private Vector2 _moveInput;

    /// <summary>Normalized movement input for the current frame.</summary>
    public Vector2 MoveInput => _moveInput;

    /// <summary>True while the run button is held.</summary>
    public bool IsRunInputHeld { get; private set; }

    /// <summary>True while the punch button is held.</summary>
    public bool IsPunchInputHeld { get; private set; }

    /// <summary>True only on the frame the punch was pressed (consumed at end of Update).</summary>
    public bool IsPunchInputPressed { get; private set; }

    /// <summary>True only on the frame space was pressed (consumed at end of Update).</summary>
    public bool IsSpacePressed { get; private set; }

    void Awake()
    {
        _actions = new Actions();
        _playerActions = _actions.Player;
        _playerActions.AddCallbacks(this);

        // Punch is optional in the Player map.
        var playerMap = _playerActions.Get();
        _punchAction = playerMap.FindAction("PunchStarted", throwIfNotFound: false);

        if (_punchAction != null)
        {
            _punchAction.started += OnPunchStarted;
            _punchAction.canceled += OnPunchCanceled;
        }
        else
        {
            Debug.LogWarning(
                "PlayerInputHandler: action 'PunchStarted' not found in 'Player' map of Actions.inputactions.");
        }

        _spaceAction = playerMap.FindAction("Space", throwIfNotFound: false);
        if (_spaceAction == null)
            _spaceAction = playerMap.FindAction("Jump", throwIfNotFound: false);

        if (_spaceAction != null)
        {
            _spaceAction.started += OnSpaceStarted;
        }
    }

    void OnEnable()
    {
        _playerActions.Enable();
    }

    void OnDisable()
    {
        _playerActions.Disable();
        _moveInput = Vector2.zero;
        IsRunInputHeld = false;
        IsPunchInputHeld = false;
        IsPunchInputPressed = false;
        IsSpacePressed = false;
    }

    void OnDestroy()
    {
        if (_punchAction != null)
        {
            _punchAction.started -= OnPunchStarted;
            _punchAction.canceled -= OnPunchCanceled;
        }

        if (_spaceAction != null)
        {
            _spaceAction.started -= OnSpaceStarted;
        }

        _playerActions.RemoveCallbacks(this);
        _actions.Dispose();
    }

    void Update()
    {
        // Direct fallback for Space key if no InputAction is triggered
        if (!IsSpacePressed && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            IsSpacePressed = true;
        }
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        IsRunInputHeld = context.ReadValueAsButton();
    }

    public void OnPunchStarted(InputAction.CallbackContext _)
    {
        IsPunchInputPressed = true;
        IsPunchInputHeld = true;
    }

    private void OnPunchCanceled(InputAction.CallbackContext _)
    {
        IsPunchInputHeld = false;
    }

    private void OnSpaceStarted(InputAction.CallbackContext _)
    {
        IsSpacePressed = true;
    }

    /// <summary>
    /// Resets single-frame flags. Must be called at the end of PlayerController.Update().
    /// </summary>
    public void ConsumeFrameInputs()
    {
        IsPunchInputPressed = false;
        IsSpacePressed = false;
    }

    public void ConsumePunchInput()
    {
        Debug.Log("Punch input consumed");
        IsPunchInputPressed = false;
    }

    public void ConsumeSpaceInput()
    {
        IsSpacePressed = false;
    }
}
