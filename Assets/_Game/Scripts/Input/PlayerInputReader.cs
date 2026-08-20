using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.Input
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }

        public bool LookFromMouse { get; private set; }

        public bool SprintHeld { get; private set; }
        public bool AimHeld { get; private set; }
        public bool FireHeld { get; private set; }
        public bool FreeLookHeld { get; private set; }

        public bool JumpPressed { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool PronePressed { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool InteractPressed { get; private set; }

        public bool LeanLeftHeld { get; private set; }
        public bool LeanRightHeld { get; private set; }

        public bool ShoulderSwitchPressed { get; private set; }

        public bool WeaponSlot1Pressed { get; private set; }
        public bool WeaponSlot2Pressed { get; private set; }
        public bool WeaponSlot3Pressed { get; private set; }

        public bool HolsterWeaponPressed { get; private set; }

        /// <summary>
        /// -1 = rueda arriba
        ///  0 = sin cambio
        /// +1 = rueda abajo
        /// </summary>
        public int WeaponScrollDirection { get; private set; }

        public bool UiBlocked { get; private set; }

        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _sprint;
        private InputAction _crouch;
        private InputAction _prone;
        private InputAction _aim;
        private InputAction _fire;
        private InputAction _reload;
        private InputAction _interact;
        private InputAction _freeLook;
        private InputAction _leanLeft;
        private InputAction _leanRight;
        private InputAction _shoulderSwitch;
        private InputAction _weaponSlot1;
        private InputAction _weaponSlot2;
        private InputAction _weaponSlot3;
        private InputAction _holsterWeapon;

        private void Awake()
        {
            _move = new InputAction(
                "Move",
                InputActionType.Value
            );

            _move
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _move.AddBinding(
                "<Gamepad>/leftStick"
            );

            _look = new InputAction(
                "Look",
                InputActionType.Value
            );

            _look.AddBinding(
                "<Mouse>/delta"
            );

            _look.AddBinding(
                "<Gamepad>/rightStick"
            );

            _jump = Button(
                "Jump",
                "<Keyboard>/space",
                "<Gamepad>/buttonSouth"
            );

            _sprint = Button(
                "Sprint",
                "<Keyboard>/leftShift",
                "<Gamepad>/leftStickPress"
            );

            _crouch = Button(
                "Crouch",
                "<Keyboard>/c",
                "<Gamepad>/buttonEast"
            );

            _prone = Button(
                "Prone",
                "<Keyboard>/z"
            );

            _aim = Button(
                "Aim",
                "<Mouse>/rightButton",
                "<Gamepad>/leftTrigger"
            );

            _fire = Button(
                "Fire",
                "<Mouse>/leftButton",
                "<Gamepad>/rightTrigger"
            );

            _reload = Button(
                "Reload",
                "<Keyboard>/r",
                "<Gamepad>/buttonWest"
            );

            _interact = Button(
                "Interact",
                "<Keyboard>/f",
                "<Gamepad>/buttonNorth"
            );

            _freeLook = Button(
                "FreeLook",
                "<Keyboard>/leftAlt"
            );

            _leanLeft = Button(
                "LeanLeft",
                "<Keyboard>/q"
            );

            _leanRight = Button(
                "LeanRight",
                "<Keyboard>/e"
            );

            _shoulderSwitch = Button(
                "ShoulderSwitch",
                "<Keyboard>/v"
            );

            _weaponSlot1 = Button(
                "WeaponSlot1",
                "<Keyboard>/1"
            );

            _weaponSlot2 = Button(
                "WeaponSlot2",
                "<Keyboard>/2"
            );

            _weaponSlot3 = Button(
                "WeaponSlot3",
                "<Keyboard>/3"
            );

            _holsterWeapon = Button(
                "HolsterWeapon",
                "<Keyboard>/x"
            );
        }

        private static InputAction Button(
            string name,
            params string[] bindings
        )
        {
            InputAction action =
                new InputAction(
                    name,
                    InputActionType.Button
                );

            foreach (string binding in bindings)
            {
                action.AddBinding(binding);
            }

            return action;
        }

        private void OnEnable()
        {
            foreach (InputAction action in Actions())
            {
                action.Enable();
            }

            ApplyCursorState();
        }

        private void OnDisable()
        {
            foreach (InputAction action in Actions())
            {
                action.Disable();
            }
        }

        private void Update()
        {
            if (UiBlocked)
            {
                ClearGameplayState();
                return;
            }

            Move =
                _move.ReadValue<Vector2>();

            Vector2 mouseLook =
                Mouse.current != null
                    ? Mouse.current.delta.ReadValue()
                    : Vector2.zero;

            if (mouseLook.sqrMagnitude > 0.0001f)
            {
                Look = mouseLook;
                LookFromMouse = true;
            }
            else
            {
                Look =
                    Gamepad.current != null
                        ? Gamepad.current
                            .rightStick
                            .ReadValue()
                        : Vector2.zero;

                LookFromMouse = false;
            }

            SprintHeld =
                _sprint.IsPressed();

            AimHeld =
                _aim.IsPressed();

            FireHeld =
                _fire.IsPressed();

            FreeLookHeld =
                _freeLook.IsPressed();

            LeanLeftHeld =
                _leanLeft.IsPressed();

            LeanRightHeld =
                _leanRight.IsPressed();

            JumpPressed =
                _jump.WasPressedThisFrame();

            CrouchPressed =
                _crouch.WasPressedThisFrame();

            PronePressed =
                _prone.WasPressedThisFrame();

            ReloadPressed =
                _reload.WasPressedThisFrame();

            InteractPressed =
                _interact.WasPressedThisFrame();

            ShoulderSwitchPressed =
                _shoulderSwitch.WasPressedThisFrame();

            WeaponSlot1Pressed =
                _weaponSlot1.WasPressedThisFrame();

            WeaponSlot2Pressed =
                _weaponSlot2.WasPressedThisFrame();

            WeaponSlot3Pressed =
                _weaponSlot3.WasPressedThisFrame();

            HolsterWeaponPressed =
                _holsterWeapon.WasPressedThisFrame();

            UpdateWeaponScroll();
        }

        public void SetUiBlocked(bool blocked)
        {
            UiBlocked = blocked;

            if (blocked)
            {
                ClearGameplayState();
            }

            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            Cursor.lockState =
                UiBlocked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;

            Cursor.visible = UiBlocked;
        }

        private void ClearGameplayState()
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            LookFromMouse = false;
            SprintHeld = false;
            AimHeld = false;
            FireHeld = false;
            FreeLookHeld = false;
            JumpPressed = false;
            CrouchPressed = false;
            PronePressed = false;
            ReloadPressed = false;
            InteractPressed = false;
            LeanLeftHeld = false;
            LeanRightHeld = false;
            ShoulderSwitchPressed = false;
            WeaponSlot1Pressed = false;
            WeaponSlot2Pressed = false;
            WeaponSlot3Pressed = false;
            HolsterWeaponPressed = false;
            WeaponScrollDirection = 0;
        }

        private void UpdateWeaponScroll()
        {
            WeaponScrollDirection = 0;

            if (Mouse.current == null)
                return;

            float scrollY =
                Mouse.current
                    .scroll
                    .ReadValue()
                    .y;

            if (scrollY > 0.01f)
            {
                // Rueda arriba:
                // slot anterior disponible.
                WeaponScrollDirection = -1;
            }
            else if (scrollY < -0.01f)
            {
                // Rueda abajo:
                // slot siguiente disponible.
                WeaponScrollDirection = 1;
            }
        }

        private InputAction[] Actions()
        {
            return new[]
            {
                _move,
                _look,
                _jump,
                _sprint,
                _crouch,
                _prone,
                _aim,
                _fire,
                _reload,
                _interact,
                _freeLook,
                _shoulderSwitch,
                _leanLeft,
                _leanRight,
                _weaponSlot1,
                _weaponSlot2,
                _weaponSlot3,
                _holsterWeapon
            };
        }
    }
}
