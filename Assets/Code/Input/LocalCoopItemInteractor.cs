using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChefGame.Items
{
    /// <summary>
    /// Picks up the nearest nearby object tagged Item and attaches it to this
    /// player's item holder while Interact is held, then drops it on release.
    /// If the hand overlaps an IngredientSpawner, the press spawns an item instead.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ChefGame.Input.LocalCoopPlayerController))]
    public sealed class LocalCoopItemInteractor : MonoBehaviour
    {
        private const float DroppedItemPickupCooldownSeconds = 1f;
        private static readonly Dictionary<int, float> DroppedItemPickupCooldowns = new();
        private static readonly Dictionary<int, LocalCoopItemInteractor> HeldItemOwners = new();

        [SerializeField] private Transform itemHolder;
        [SerializeField] private Transform interactionCenter;
        [SerializeField, Min(0f)] private float interactionRadius = 1f;
        [SerializeField] private LayerMask itemLayers = ~0;
        [SerializeField, Tooltip("If enabled, a player can grab an item out of another player's hand.")]
        private bool allowTakingFromOtherPlayers = true;
        [SerializeField] private bool isHoldingItem;

        private ChefGame.Input.LocalCoopPlayerController playerController;
        private InputAction interactAction;
        private Transform heldItem;
        private int heldItemId;
        private Rigidbody heldRigidbody;
        private bool heldRigidbodyWasKinematic;
        private bool heldRigidbodyUsedGravity;
        private Collider[] playerColliders;
        private Collider[] heldColliders;
        private readonly List<Collider> handColliders = new();
        private readonly HashSet<MonoBehaviour> activeInteractionOverrides = new();
        private bool missingInteractActionLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            DroppedItemPickupCooldowns.Clear();
            HeldItemOwners.Clear();
        }

        public bool IsHoldingItem => isHoldingItem && heldItem != null;
        public Transform HeldItem => heldItem;
        public Transform ItemHolder => itemHolder;
        public InputAction InteractAction => playerController != null ? playerController.InteractAction : null;

        private void Awake()
        {
            playerController = GetComponent<ChefGame.Input.LocalCoopPlayerController>();
            playerColliders = GetComponentsInChildren<Collider>(true);

            // The hand colliders (tag "Hand") define whether we are inside a spawner.
            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider != null && playerCollider.CompareTag("Hand"))
                    handColliders.Add(playerCollider);
            }

            if (handColliders.Count == 0)
                Debug.LogWarning($"{nameof(LocalCoopItemInteractor)} on '{name}' found no collider tagged 'Hand'. Ingredient spawners will not work for this player.", this);

            if (interactionCenter == null)
                interactionCenter = transform;
        }

        private void OnEnable()
        {
            if (playerController == null)
                playerController = GetComponent<ChefGame.Input.LocalCoopPlayerController>();

            // OnEnable can run before the controller's Awake has created its
            // private action asset. Try now, then retry in Start after all
            // Awake calls have completed.
            TrySubscribeToInteractAction();
        }

        private void Start()
        {
            if (!TrySubscribeToInteractAction() && !missingInteractActionLogged)
            {
                Debug.LogError($"{nameof(LocalCoopItemInteractor)} on '{name}' could not find the Interact action. Check that the player controller has a LocalChef asset assigned and that it contains a Gameplay/Interact action.", this);
                missingInteractActionLogged = true;
            }
        }

        private bool TrySubscribeToInteractAction()
        {
            if (interactAction != null)
                return true;

            if (playerController == null)
                playerController = GetComponent<ChefGame.Input.LocalCoopPlayerController>();

            interactAction = playerController != null ? playerController.InteractAction : null;
            if (interactAction == null)
                return false;

            interactAction.performed += HandleInteractPressed;
            interactAction.canceled += HandleInteractReleased;
            return true;
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.performed -= HandleInteractPressed;
                interactAction.canceled -= HandleInteractReleased;
                // Must be cleared, otherwise OnEnable thinks we are still subscribed
                // after a disable/enable cycle and never re-subscribes.
                interactAction = null;
            }

            // Clear held-item state even if the serialized/runtime flag has
            // become out of sync with the tracked item reference.
            DropItem();
        }

        private void HandleInteractPressed(InputAction.CallbackContext context)
        {
            // The held item may have been destroyed by another system (cutting, serving...).
            ClearStaleHeldItem();

            if (isHoldingItem)
                return;

            // A spawner the hand really overlaps right now owns this press.
            IngredientSpawner spawner = IngredientSpawner.FindOverlapping(handColliders);
            if (spawner != null)
            {
                spawner.TrySpawnFor(this);
                return;
            }

            if (!HasActiveInteractionOverride())
                TryPickUpItem();
        }

        /// <summary>
        /// Lets other stations claim the press so no item is picked up from the ground.
        /// Kept for compatibility with other scripts.
        /// </summary>
        public void SetInteractionOverride(MonoBehaviour source, bool active)
        {
            if (source == null)
                return;

            if (active)
                activeInteractionOverrides.Add(source);
            else
                activeInteractionOverrides.Remove(source);
        }

        private bool HasActiveInteractionOverride()
        {
            // Drop sources that were destroyed without clearing their override.
            activeInteractionOverrides.RemoveWhere(source => source == null);
            return activeInteractionOverrides.Count > 0;
        }

        /// <summary>Drops the currently held item if it is the specified object.</summary>
        public bool TryDropHeldItem(Transform item)
        {
            if (item == null || heldItem != item)
                return false;

            DropItem();
            return true;
        }

        private void HandleInteractReleased(InputAction.CallbackContext context)
        {
            // A release always ends the hold. DropItem is safe when already
            // empty and also clears stale references/flags left by an item
            // being destroyed or changed by another system.
            DropItem();
        }

        private void ClearStaleHeldItem()
        {
            if (isHoldingItem && heldItem == null)
                DropItem();
        }

        private void TryPickUpItem()
        {
            if (itemHolder == null)
            {
                Debug.LogWarning($"{nameof(LocalCoopItemInteractor)} on '{name}' has no Item Holder assigned.", this);
                return;
            }

            Vector3 center = interactionCenter != null ? interactionCenter.position : transform.position;
            Collider[] nearbyColliders = Physics.OverlapSphere(
                center,
                interactionRadius,
                itemLayers,
                QueryTriggerInteraction.Collide);

            Transform nearestItem = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            foreach (Collider nearbyCollider in nearbyColliders)
            {
                if (nearbyCollider == null)
                    continue;

                Transform taggedItem = FindTaggedItemRoot(nearbyCollider.transform);
                if (taggedItem == null || IsPickupCoolingDown(taggedItem))
                    continue;

                // Skip items that are already in a hand (our own, or another player's
                // if taking is disabled), so they can't block a free item behind them.
                if (TryGetHolder(taggedItem, out LocalCoopItemInteractor holder) &&
                    (holder == this || !allowTakingFromOtherPlayers))
                    continue;

                float distanceSquared = (taggedItem.position - center).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestItem = taggedItem;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            if (nearestItem != null)
                TryHoldItem(nearestItem);
        }

        private static Transform FindTaggedItemRoot(Transform candidate)
        {
            for (Transform current = candidate; current != null; current = current.parent)
            {
                if (current.CompareTag("Item"))
                    return current;
            }

            return null;
        }

        private static bool TryGetHolder(Transform item, out LocalCoopItemInteractor holder)
        {
            if (item != null &&
                HeldItemOwners.TryGetValue(item.GetInstanceID(), out holder) &&
                holder != null && holder.heldItem == item)
                return true;

            holder = null;
            return false;
        }

        /// <summary>Attaches an item to this player's holder using the normal pickup rules.</summary>
        public bool TryHoldItem(Transform item)
        {
            ClearStaleHeldItem();

            if (isHoldingItem || itemHolder == null || item == null || IsPickupCoolingDown(item))
                return false;

            // Item is currently in someone's hand: take it over cleanly or refuse.
            if (TryGetHolder(item, out LocalCoopItemInteractor currentHolder))
            {
                if (currentHolder == this || !allowTakingFromOtherPlayers)
                    return false;

                // Lets the previous holder restore the rigidbody/colliders/flags properly.
                currentHolder.TryDropHeldItem(item);
                DroppedItemPickupCooldowns.Remove(item.GetInstanceID());
            }

            heldItem = item;
            heldItemId = item.GetInstanceID();
            HeldItemOwners[heldItemId] = this;
            heldItem.SetParent(itemHolder, true);
            heldItem.position = itemHolder.position;

            // Keep held colliders enabled for contacts with the ground and
            // world, while ignoring only this player's own hand colliders.
            heldColliders = heldItem.GetComponentsInChildren<Collider>(true);
            foreach (Collider itemCollider in heldColliders)
            {
                if (itemCollider == null)
                    continue;

                foreach (Collider playerCollider in playerColliders)
                {
                    if (playerCollider != null)
                        Physics.IgnoreCollision(itemCollider, playerCollider, true);
                }
            }

            heldRigidbody = heldItem.GetComponent<Rigidbody>();
            if (heldRigidbody != null)
            {
                heldRigidbodyWasKinematic = heldRigidbody.isKinematic;
                heldRigidbodyUsedGravity = heldRigidbody.useGravity;
                heldRigidbody.isKinematic = true;
                heldRigidbody.useGravity = false;
            }

            // Ingredient being held info
            IngredientInstance ingredientInstance = item.GetComponent<IngredientInstance>();
            if (ingredientInstance != null)
                ingredientInstance.isBeingHeld = true;

            isHoldingItem = true;
            return true;
        }

        private void DropItem()
        {
            if (heldItem != null)
            {
                DroppedItemPickupCooldowns[heldItemId] = Time.time + DroppedItemPickupCooldownSeconds;

                // Ingredient drop being held info
                IngredientInstance ingredientInstance = heldItem.GetComponent<IngredientInstance>();
                if (ingredientInstance != null)
                    ingredientInstance.isBeingHeld = false;

                // Unity refuses to re-parent while this hierarchy is being deactivated.
                if (gameObject.activeInHierarchy)
                    heldItem.SetParent(null, true);
            }

            if (heldItemId != 0 &&
                HeldItemOwners.TryGetValue(heldItemId, out LocalCoopItemInteractor owner) &&
                owner == this)
                HeldItemOwners.Remove(heldItemId);

            if (heldColliders != null)
            {
                foreach (Collider itemCollider in heldColliders)
                {
                    if (itemCollider == null)
                        continue;

                    foreach (Collider playerCollider in playerColliders)
                    {
                        if (playerCollider != null)
                            Physics.IgnoreCollision(itemCollider, playerCollider, false);
                    }
                }
            }

            if (heldRigidbody != null)
            {
                heldRigidbody.useGravity = heldRigidbodyUsedGravity;
                heldRigidbody.isKinematic = heldRigidbodyWasKinematic;
            }

            heldItem = null;
            heldItemId = 0;
            heldRigidbody = null;
            heldColliders = null;
            isHoldingItem = false;
        }

        private static bool IsPickupCoolingDown(Transform item)
        {
            if (item == null)
                return false;

            int itemId = item.GetInstanceID();
            if (!DroppedItemPickupCooldowns.TryGetValue(itemId, out float cooldownEndTime))
                return false;

            if (Time.time < cooldownEndTime)
                return true;

            DroppedItemPickupCooldowns.Remove(itemId);
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isHoldingItem ? Color.green : Color.yellow;
            Vector3 center = interactionCenter != null ? interactionCenter.position : transform.position;
            Gizmos.DrawWireSphere(center, interactionRadius);
        }
    }
}