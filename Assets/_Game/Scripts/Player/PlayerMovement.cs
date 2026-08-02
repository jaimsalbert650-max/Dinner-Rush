using UnityEngine;
using UnityEngine.InputSystem;

namespace DinnerRush
{
    /// <summary>
    /// Moves the player Rigidbody2D. Input comes from a self-contained InputAction that
    /// binds keyboard WASD/arrows AND the gamepad left stick — an on-screen touch joystick
    /// (OnScreenStick set to "&lt;Gamepad&gt;/leftStick") drives the same path on phones.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private InputAction _move;
        private Vector2 _input;

        // Injected by PlayerRoot so movement reads live, upgradeable stats.
        public PlayerStats Stats { get; set; } = new PlayerStats();

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");
        }

        private void OnEnable() => _move.Enable();
        private void OnDisable() => _move.Disable();

        private void Update() => _input = _move.ReadValue<Vector2>();

        private void FixedUpdate()
        {
            Vector2 v = Vector2.ClampMagnitude(_input, 1f);
            _rb.MovePosition(_rb.position + v * Stats.MoveSpeed * Time.fixedDeltaTime);
        }
    }
}
