using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePJ.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HeroInputController))]
    [DisallowMultipleComponent]
    public sealed class HeroMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float rotationSpeed = 720f;
        [SerializeField] float gravity = -20f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] Camera playerCamera;

        CharacterController controller;
        HeroInputController input;
        float verticalVelocity;
        Vector3 planarVelocity;
        Vector3 dashVelocity;
        float dashRemaining;
        float moveSpeedMultiplier = 1f;
        float chargeHaste;
        bool movementLocked;

        public float CurrentSpeed => planarVelocity.magnitude;
        public Vector3 PlanarVelocity => planarVelocity;
        public bool IsDashing => dashRemaining > 0f;

        public void SetPlayerCamera(Camera camera) => playerCamera = camera;

        public void SetGroundMask(LayerMask mask) => groundMask = mask;

        public void SetMoveSpeedMultiplier(float value) => moveSpeedMultiplier = Mathf.Max(0f, value);
        public void SetChargeHaste(float value) => chargeHaste = Mathf.Max(0f, value);
        public void SetMovementLocked(bool value) => movementLocked = value;

        public void Dash(Vector3 direction, float distance, float duration)
        {
            var time = Mathf.Max(0.05f, duration);
            var dir = direction;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = transform.forward;
            }

            dashVelocity = dir.normalized * (distance / time);
            dashRemaining = time;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<HeroInputController>();

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        void Update()
        {
            if (dashRemaining > 0f)
            {
                dashRemaining -= Time.deltaTime;
                planarVelocity = dashVelocity;
            }
            else
            {
                var moveInput = input.MoveInput;
                var moveDirection = movementLocked ? Vector3.zero : GetCameraRelativeDirection(moveInput);
                var speed = moveSpeed * moveSpeedMultiplier * (1f + chargeHaste);
                planarVelocity = moveDirection * speed;
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;

            var motion = planarVelocity;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            if (input == null || !input.UiBlocksAiming)
            {
                UpdateFacing();
            }
        }

        Vector3 GetCameraRelativeDirection(Vector2 inputAxis)
        {
            if (inputAxis.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            if (playerCamera == null)
            {
                return new Vector3(inputAxis.x, 0f, inputAxis.y).normalized;
            }

            var camForward = playerCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            var camRight = playerCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            return (camRight * inputAxis.x + camForward * inputAxis.y).normalized;
        }

        public bool TryGetMouseGroundPoint(out Vector3 point)
        {
            point = default;
            if (playerCamera == null || Mouse.current == null)
            {
                return false;
            }

            var ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 500f, groundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            point = hit.point;
            return true;
        }

        void UpdateFacing()
        {
            if (!TryGetMouseGroundPoint(out var hitPoint))
            {
                return;
            }

            var lookDirection = hitPoint - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }
}
