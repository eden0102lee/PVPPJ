using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePJ.Player
{
    [DisallowMultipleComponent]
    public sealed class HeroInputController : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string actionMapName = "Player";

        InputAction moveAction;
        InputAction attackAction;

        public Vector2 MoveInput { get; private set; }
        public bool AttackPressedThisFrame { get; private set; }
        public bool Attack1PressedThisFrame { get; private set; }
        public bool Attack1Held { get; private set; }
        public bool Attack1ReleasedThisFrame { get; private set; }
        public bool Attack2PressedThisFrame { get; private set; }
        public bool Attack2Held { get; private set; }
        public bool Attack3PressedThisFrame { get; private set; }
        public bool Attack3Held { get; private set; }
        public bool Attack4PressedThisFrame { get; private set; }
        public bool Attack4Held { get; private set; }
        public bool Attack5PressedThisFrame { get; private set; }
        public bool Attack5Held { get; private set; }
        public bool DodgePressedThisFrame { get; private set; }
        public bool ParryPressedThisFrame { get; private set; }
        public bool TalentTogglePressedThisFrame { get; private set; }
        public bool DummyResetPressedThisFrame { get; private set; }
        public bool DummyAutoTogglePressedThisFrame { get; private set; }
        public bool DummyGuardTogglePressedThisFrame { get; private set; }
        public bool WeaponPrevPressedThisFrame { get; private set; }
        public bool WeaponNextPressedThisFrame { get; private set; }
        public bool RangeTogglePressedThisFrame { get; private set; }
        public bool UiBlocksGameplay { get; set; }
        public bool UiBlocksAiming { get; set; }

        void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(HeroInputController)} requires an InputActionAsset.", this);
                return;
            }

            var map = inputActions.FindActionMap(actionMapName, true);
            moveAction = map.FindAction("Move", true);
            attackAction = map.FindAction("Attack", true);
        }

        void OnEnable()
        {
            moveAction?.Enable();
            attackAction?.Enable();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            attackAction?.Disable();
        }

        void Update()
        {
            MoveInput = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            AttackPressedThisFrame = attackAction != null && attackAction.WasPressedThisFrame();

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            var leftPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            var leftHeld = mouse != null && mouse.leftButton.isPressed;
            var leftReleased = mouse != null && mouse.leftButton.wasReleasedThisFrame;
            if (attackAction != null)
            {
                leftPressed |= attackAction.WasPressedThisFrame();
                leftHeld |= attackAction.IsPressed();
                leftReleased |= attackAction.WasReleasedThisFrame();
            }

            Attack1PressedThisFrame = !UiBlocksGameplay && leftPressed;
            Attack1Held = leftHeld && (!UiBlocksGameplay || Attack1Held);
            Attack1ReleasedThisFrame = leftReleased;

            Attack2PressedThisFrame = !UiBlocksGameplay && mouse != null && mouse.rightButton.wasPressedThisFrame;
            Attack2Held = mouse != null && mouse.rightButton.isPressed;
            Attack3PressedThisFrame = !UiBlocksGameplay && keyboard != null && keyboard.qKey.wasPressedThisFrame;
            Attack3Held = keyboard != null && keyboard.qKey.isPressed;
            Attack4PressedThisFrame = !UiBlocksGameplay && keyboard != null && keyboard.eKey.wasPressedThisFrame;
            Attack4Held = keyboard != null && keyboard.eKey.isPressed;
            Attack5PressedThisFrame = !UiBlocksGameplay && keyboard != null && keyboard.rKey.wasPressedThisFrame;
            Attack5Held = keyboard != null && keyboard.rKey.isPressed;
            DodgePressedThisFrame = !UiBlocksGameplay && keyboard != null &&
                                    (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame);
            ParryPressedThisFrame = !UiBlocksGameplay && keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
            TalentTogglePressedThisFrame = keyboard != null && keyboard.tabKey.wasPressedThisFrame;
            DummyResetPressedThisFrame = keyboard != null && keyboard.fKey.wasPressedThisFrame;
            DummyAutoTogglePressedThisFrame = keyboard != null && keyboard.gKey.wasPressedThisFrame;
            DummyGuardTogglePressedThisFrame = keyboard != null && keyboard.hKey.wasPressedThisFrame;
            WeaponPrevPressedThisFrame = !UiBlocksGameplay && keyboard != null && keyboard.minusKey.wasPressedThisFrame;
            WeaponNextPressedThisFrame = !UiBlocksGameplay && keyboard != null && keyboard.equalsKey.wasPressedThisFrame;
            RangeTogglePressedThisFrame = keyboard != null && keyboard.vKey.wasPressedThisFrame;
        }
    }
}
