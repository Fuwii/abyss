using System;
using UnityEngine;

namespace Game.Player.Input
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private Vector2 mouseSensitivity = Vector2.one * 100f;
        public static event Action<Vector2> OnMoveAxisChanged;
        public static event Action<Vector2> OnLookDeltaChanged;

        public static event Action OnJumpPressed;
        public static event Action OnLeftClickStarted;
        public static event Action OnLeftClickCanceled;
        private PlayerInputActions _actions;

        private void Awake()
        {
            _actions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _actions.Enable();

            _actions.Player.Move.performed += ctx => OnMoveAxisChanged?.Invoke(ctx.ReadValue<Vector2>());
            _actions.Player.Move.canceled += ctx => OnMoveAxisChanged?.Invoke(Vector2.zero);
            _actions.Player.Look.performed += ctx => OnLookDeltaChanged?.Invoke(Vector2.Scale(ctx.ReadValue<Vector2>(), mouseSensitivity));
            _actions.Player.Look.canceled += ctx => OnLookDeltaChanged?.Invoke(Vector2.zero);

            _actions.Player.Jump.performed += ctx => OnJumpPressed?.Invoke();

            _actions.Player.LeftClick.started += ctx => OnLeftClickStarted?.Invoke();
            _actions.Player.LeftClick.canceled += ctx => OnLeftClickCanceled?.Invoke();
        }

        private void OnDisable()
        {
            _actions.Disable();
        }
    }
}