using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace ChefGame.Input
{
    /// <summary>
    /// Local co-op movement for a player hand using a keyboard layout or gamepad.
    /// Movement stays within the Main Camera's viewport and a configurable
    /// depth range around the hand's position when play begins.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalCoopPlayerController : MonoBehaviour
    {
        private static readonly List<LocalCoopPlayerController> GamepadControllers = new();

        public enum InputScheme
        {
            WASD,
            Arrows,
            Gamepad
        }

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField, FormerlySerializedAs("keyboardLayout")] private InputScheme inputScheme = InputScheme.WASD;
        [SerializeField] private bool autoAssignGamepad = true;
        [SerializeField, Min(0)] private int gamepadSlot;
        [SerializeField] private Camera movementCamera;
        [SerializeField, Min(0f)] private float movementAcceleration = 40f;
        [SerializeField, Min(0f)] private float movementZAcceleration = 20f;
        [SerializeField, Min(0f)] private float maximumSpeed = 4.5f;
        [SerializeField, Min(0f)] private float drag = 7f;
        [SerializeField, Min(0f)] private float depthMovementLimit = 2f;
        [SerializeField, Tooltip("When disabled, horizontal movement is not clamped to the camera edges.")]
        private bool clampHorizontalToScreen = true;
        [SerializeField, Range(0f, 0.25f), Tooltip("Horizontal edge margin as a fraction of the viewport. Kept much smaller than the vertical margin.")]
        private float horizontalScreenEdgePadding = 0.005f;
        [SerializeField, Range(0f, 0.25f), Tooltip("Vertical edge margin as a fraction of the viewport.")]
        private float screenEdgePadding = 0.03f;

        private const int MaxClampIterations = 4;

        private InputActionAsset runtimeActions;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction backwardAction;
        private InputAction forwardAction;
        private InputAction interactAction;
        private InputAction bookAction;
        private InputAction pressAction;
        private Rigidbody movementRigidbody;
        private Gamepad assignedGamepad;
        private Vector3 movementCenter;
        private Collider screenBoundsCollider;

        public InputAction InteractAction => interactAction;
        public InputAction PressAction => pressAction;
        public InputAction BookAction => bookAction;

        private string BindingGroup => inputScheme switch
        {
            InputScheme.WASD => "WASD",
            InputScheme.Arrows => "Arrows",
            InputScheme.Gamepad => "Gamepad",
            _ => "WASD"
        };

        public static event Action OnPressPerformed;
        public static event Action OnAnyInputDetected;
        public static event Action<bool> OnBookHoldToggled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            GamepadControllers.Clear();
            OnPressPerformed = null;
            OnAnyInputDetected = null;
            OnBookHoldToggled = null;
        }

        private void Awake()
        {
            movementCenter = transform.position;
            movementRigidbody = GetComponent<Rigidbody>();
            FindScreenBoundsCollider();

            if (movementRigidbody == null)
                Debug.LogWarning($"{nameof(LocalCoopPlayerController)} on '{name}' has no Rigidbody on its root. Physics-aware movement requires a root Rigidbody.", this);
            else
                movementRigidbody.constraints |= RigidbodyConstraints.FreezeRotation;

            if (movementCamera == null)
                movementCamera = Camera.main;

            if (movementCamera == null)
                Debug.LogWarning($"{nameof(LocalCoopPlayerController)} on '{name}' could not find a Main Camera. Screen-edge clamping is disabled.", this);

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(LocalCoopPlayerController)} on '{name}' has no Input Action Asset assigned.", this);
                enabled = false;
                return;
            }

            // Each player gets a private copy so its scheme mask and device
            // filter cannot affect the other player.
            runtimeActions = Instantiate(inputActions);

            gameplayMap = runtimeActions.FindActionMap("Gameplay", true);
            gameplayMap.bindingMask = InputBinding.MaskByGroup(BindingGroup);
            moveAction = gameplayMap.FindAction("Move", true);
            backwardAction = gameplayMap.FindAction("Backward", true);
            forwardAction = gameplayMap.FindAction("Forward", true);
            interactAction = gameplayMap.FindAction("Interact", true);
            pressAction = gameplayMap.FindAction("Press", true);
            bookAction = gameplayMap.FindAction("Book", true);
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += HandleDeviceChange;

            if (inputScheme == InputScheme.Gamepad)
            {
                if (!GamepadControllers.Contains(this))
                    GamepadControllers.Add(this);
                ReassignGamepads();
            }

            if (gameplayMap != null)
            {
                // Refresh the scheme mask before enabling in case it was
                // changed in the Inspector while the component was disabled.
                gameplayMap.bindingMask = InputBinding.MaskByGroup(BindingGroup);
                gameplayMap.Enable();
            }

            if (pressAction != null)
                pressAction.started += OnPressAction;

            if (bookAction != null)
            {
                bookAction.performed += OnBookStarted;
                bookAction.canceled += OnBookCanceled;
            }
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;

            if (gameplayMap != null)
                gameplayMap.Disable();

            if (GamepadControllers.Remove(this))
                ReassignGamepads();

            if (pressAction != null)
                pressAction.started -= OnPressAction;

            if (bookAction != null)
            {
                bookAction.performed -= OnBookStarted;
                bookAction.canceled -= OnBookCanceled;
            }
        }

        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (inputScheme == InputScheme.Gamepad && (device is Gamepad || device == assignedGamepad))
                ReassignGamepads();
        }

        private static void ReassignGamepads()
        {
            GamepadControllers.RemoveAll(controller => controller == null || !controller.isActiveAndEnabled || controller.inputScheme != InputScheme.Gamepad);
            GamepadControllers.Sort((left, right) => string.CompareOrdinal(
                GetHierarchySortKey(left.transform),
                GetHierarchySortKey(right.transform)));

            var claimedSlots = new HashSet<int>();

            // Reserve manual selections before assigning the remaining pads.
            foreach (LocalCoopPlayerController controller in GamepadControllers)
            {
                if (controller.autoAssignGamepad)
                    continue;

                if (controller.gamepadSlot >= 0 && controller.gamepadSlot < Gamepad.all.Count && claimedSlots.Add(controller.gamepadSlot))
                    controller.AssignGamepad(Gamepad.all[controller.gamepadSlot]);
                else
                    controller.AssignGamepad(null);
            }

            foreach (LocalCoopPlayerController controller in GamepadControllers)
            {
                if (!controller.autoAssignGamepad)
                    continue;

                int availableSlot = 0;
                while (availableSlot < Gamepad.all.Count && claimedSlots.Contains(availableSlot))
                    availableSlot++;

                if (availableSlot < Gamepad.all.Count)
                {
                    claimedSlots.Add(availableSlot);
                    controller.AssignGamepad(Gamepad.all[availableSlot]);
                }
                else
                {
                    controller.AssignGamepad(null);
                }
            }
        }

        private void AssignGamepad(Gamepad gamepad)
        {
            if (assignedGamepad == gamepad || runtimeActions == null)
                return;

            assignedGamepad = gamepad;
            // An empty device list is important when a pad is unavailable:
            // a null list would let this player's actions listen to every pad.
            runtimeActions.devices = assignedGamepad != null
                ? new InputDevice[] { assignedGamepad }
                : new InputDevice[0];
        }

        private static string GetHierarchySortKey(Transform target)
        {
            var siblingIndices = new List<int>();
            for (Transform current = target; current != null; current = current.parent)
                siblingIndices.Add(current.GetSiblingIndex());

            string key = target.gameObject.scene.path;
            for (int i = siblingIndices.Count - 1; i >= 0; i--)
                key += "/" + siblingIndices[i].ToString("D4");
            return key;
        }

        private void FixedUpdate()
        {
            if (moveAction == null || movementRigidbody == null)
                return;

            Vector2 planarInput = moveAction.ReadValue<Vector2>();
            float depthInput = (forwardAction.IsPressed() ? 1f : 0f) -
                               (backwardAction.IsPressed() ? 1f : 0f);
            float deltaTime = Time.fixedDeltaTime;
            Vector3 currentVelocity = movementRigidbody.linearVelocity;

            if (planarInput.sqrMagnitude > 0.0001f || Mathf.Abs(depthInput) > 0.0001f)
            {
                OnAnyInputDetected?.Invoke();
                // Integrate planar and depth acceleration separately while
                // retaining collision velocity from the dynamic Rigidbody.
                currentVelocity.x += planarInput.x * movementAcceleration * deltaTime;
                currentVelocity.y += planarInput.y * movementAcceleration * deltaTime;
                currentVelocity.z += depthInput * movementZAcceleration * deltaTime;
            }
            else
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, drag * deltaTime);
            }

            currentVelocity = Vector3.ClampMagnitude(currentVelocity, maximumSpeed);

            Vector3 proposedPosition = movementRigidbody.position + currentVelocity * deltaTime;
            float minimumZ = movementCenter.z - depthMovementLimit;
            float maximumZ = movementCenter.z + depthMovementLimit;
            float clampedZ = Mathf.Clamp(proposedPosition.z, minimumZ, maximumZ);

            if (!Mathf.Approximately(clampedZ, proposedPosition.z))
            {
                bool movingOutward = proposedPosition.z > maximumZ ? currentVelocity.z > 0f : currentVelocity.z < 0f;
                if (movingOutward)
                    currentVelocity.z = 0f;
                proposedPosition.z = clampedZ;
            }

            Vector3 clampedPosition = ClampToScreen(proposedPosition);
            Vector3 boundaryCorrection = clampedPosition - proposedPosition;
            if (boundaryCorrection.sqrMagnitude > 0.000001f)
            {
                // Correct the velocity only when the hand reaches its depth or
                // camera boundary; ordinary movement is handled by PhysX.
                currentVelocity = (clampedPosition - movementRigidbody.position) / deltaTime;
            }

            movementRigidbody.linearVelocity = currentVelocity;
        }

        private Vector3 ClampToScreen(Vector3 position)
        {
            if (movementCamera == null)
                return position;

            Vector3 result = position;

            for (int i = 0; i < MaxClampIterations; i++)
            {
                if (!TryGetViewportRect(result, out Vector2 min, out Vector2 max, out Vector3 pivotViewport))
                    return result;

                float horizontalCorrection = 0f;
                if (clampHorizontalToScreen)
                {
                    if (min.x < horizontalScreenEdgePadding)
                        horizontalCorrection = horizontalScreenEdgePadding - min.x;
                    else if (max.x > 1f - horizontalScreenEdgePadding)
                        horizontalCorrection = 1f - horizontalScreenEdgePadding - max.x;
                }

                float verticalCorrection = 0f;
                if (min.y < screenEdgePadding)
                    verticalCorrection = screenEdgePadding - min.y;
                else if (max.y > 1f - screenEdgePadding)
                    verticalCorrection = 1f - screenEdgePadding - max.y;

                if (Mathf.Abs(horizontalCorrection) < 0.00001f && Mathf.Abs(verticalCorrection) < 0.00001f)
                    return result;

                Vector3 adjustedViewport = new Vector3(
                    pivotViewport.x + horizontalCorrection,
                    pivotViewport.y + verticalCorrection,
                    pivotViewport.z);

                // Move the pivot along the camera ray, but keep it on its fixed-Z plane.
                Ray ray = movementCamera.ViewportPointToRay(adjustedViewport);
                Plane depthPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, result.z));
                if (!depthPlane.Raycast(ray, out float distance) || distance < 0f)
                    return result;

                result = ray.GetPoint(distance);
            }

            return result;
        }

        /// <summary>
        /// Viewport rectangle of the hand at the given position. Only the X/Y size of the
        /// hitbox is used (a flat rectangle at the hitbox's depth), so the camera pitch
        /// can't inflate the hand's on-screen size through the collider's Z extent.
        /// </summary>
        private bool TryGetViewportRect(Vector3 position, out Vector2 min, out Vector2 max, out Vector3 pivotViewport)
        {
            pivotViewport = movementCamera.WorldToViewportPoint(position);
            min = max = new Vector2(pivotViewport.x, pivotViewport.y);

            if (pivotViewport.z <= movementCamera.nearClipPlane)
                return false;

            if (screenBoundsCollider == null)
                return true;

            Bounds bounds = screenBoundsCollider.bounds;
            Vector3 center = bounds.center + (position - transform.position);
            Vector3 extents = bounds.extents;

            min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
            {
                Vector3 corner = center + new Vector3(
                    (cornerIndex & 1) == 0 ? -extents.x : extents.x,
                    (cornerIndex & 2) == 0 ? -extents.y : extents.y,
                    0f);

                Vector3 vp = movementCamera.WorldToViewportPoint(corner);
                if (vp.z <= movementCamera.nearClipPlane)
                    continue;

                min = Vector2.Min(min, new Vector2(vp.x, vp.y));
                max = Vector2.Max(max, new Vector2(vp.x, vp.y));
            }

            if (float.IsPositiveInfinity(min.x))
                min = max = new Vector2(pivotViewport.x, pivotViewport.y);

            return true;
        }

        private void FindScreenBoundsCollider()
        {
            Collider[] childColliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < childColliders.Length; i++)
            {
                Collider candidate = childColliders[i];
                if (candidate != null && candidate.enabled && candidate.CompareTag("Hand"))
                {
                    screenBoundsCollider = candidate;
                    return;
                }
            }
        }

        private void OnPressAction(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                OnPressPerformed?.Invoke();
                OnAnyInputDetected?.Invoke();
            }
        }

        private void OnBookStarted(InputAction.CallbackContext context)
        {
            OnBookHoldToggled?.Invoke(true);
            OnAnyInputDetected?.Invoke();
        }

        private void OnBookCanceled(InputAction.CallbackContext context)
        {
            OnBookHoldToggled?.Invoke(false);
        }
    }
}
