using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChefGame.Items
{
    /// <summary>Spawns and immediately holds one configured item when a player presses Interact inside the trigger.</summary>
    [DisallowMultipleComponent]
    public sealed class IngredientSpawner : MonoBehaviour
    {
        [SerializeField] private Collider spawnArea;
        [SerializeField] private GameObject prefabToSpawn;

        private readonly Dictionary<Collider, LocalCoopItemInteractor> occupants = new();
        private readonly HashSet<LocalCoopItemInteractor> subscribedInteractors = new();

        private void Awake()
        {
            if (spawnArea == null)
                spawnArea = GetComponent<Collider>();

            if (spawnArea == null || !spawnArea.isTrigger)
                Debug.LogError($"{nameof(IngredientSpawner)} on '{name}' requires a trigger collider assigned as its spawn area.", this);
        }

        private void OnDisable()
        {
            foreach (LocalCoopItemInteractor interactor in subscribedInteractors)
            {
                Unsubscribe(interactor);
                interactor.SetInteractionOverride(this, false);
            }
            subscribedInteractors.Clear();
            occupants.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Held items are parented beneath the player too, so walking up
            // the hierarchy alone can mistake an ingredient collider for a
            // player collider and leave this spawner subscribed after the
            // ingredient is cut/destroyed. Only the actual hand collider can
            // establish player occupancy.
            if (other == null || !other.CompareTag("Hand") || occupants.ContainsKey(other))
                return;

            LocalCoopItemInteractor interactor = other.GetComponentInParent<LocalCoopItemInteractor>();
            if (interactor == null)
                return;

            occupants.Add(other, interactor);
            if (subscribedInteractors.Add(interactor))
            {
                interactor.SetInteractionOverride(this, true);
                InputAction action = interactor.InteractAction;
                if (action != null)
                    action.performed += HandleInteractPressed;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null || !occupants.Remove(other, out LocalCoopItemInteractor interactor))
                return;

            foreach (LocalCoopItemInteractor remaining in occupants.Values)
            {
                if (remaining == interactor)
                    return;
            }

            UnregisterInteractor(interactor);
        }

        private void HandleInteractPressed(InputAction.CallbackContext context)
        {
            LocalCoopItemInteractor interactor = context.action != null
                ? FindInteractorForAction(context.action)
                : null;
            if (interactor == null)
                return;

            if (!IsHandColliderStillInside(interactor))
            {
                RemoveInteractorOccupancy(interactor);
                UnregisterInteractor(interactor);
                return;
            }

            if (interactor.IsHoldingItem)
                return;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"'{name}' has no prefab assigned to {nameof(IngredientSpawner)}.", this);
                return;
            }

            Transform holder = interactor.ItemHolder;
            if (holder == null)
            {
                Debug.LogWarning($"The nearby player '{interactor.name}' has no item holder assigned.", interactor);
                return;
            }

            GameObject spawned = Instantiate(prefabToSpawn, holder.position, holder.rotation);
            if (!interactor.TryHoldItem(spawned.transform))
                Destroy(spawned);
        }

        private LocalCoopItemInteractor FindInteractorForAction(InputAction action)
        {
            foreach (LocalCoopItemInteractor interactor in subscribedInteractors)
            {
                if (interactor != null && interactor.InteractAction == action)
                    return interactor;
            }

            return null;
        }

        private bool IsHandColliderStillInside(LocalCoopItemInteractor interactor)
        {
            if (spawnArea == null || !spawnArea.enabled || !spawnArea.gameObject.activeInHierarchy)
                return false;

            Bounds spawnBounds = spawnArea.bounds;
            foreach (KeyValuePair<Collider, LocalCoopItemInteractor> occupant in occupants)
            {
                Collider handCollider = occupant.Key;
                if (occupant.Value == interactor && handCollider != null && handCollider.enabled &&
                    handCollider.gameObject.activeInHierarchy && handCollider.CompareTag("Hand") &&
                    spawnBounds.Intersects(handCollider.bounds))
                    return true;
            }

            return false;
        }

        private void RemoveInteractorOccupancy(LocalCoopItemInteractor interactor)
        {
            List<Collider> collidersToRemove = new();
            foreach (KeyValuePair<Collider, LocalCoopItemInteractor> occupant in occupants)
            {
                if (occupant.Value == interactor)
                    collidersToRemove.Add(occupant.Key);
            }

            foreach (Collider handCollider in collidersToRemove)
                occupants.Remove(handCollider);
        }

        private void UnregisterInteractor(LocalCoopItemInteractor interactor)
        {
            if (subscribedInteractors.Remove(interactor))
            {
                Unsubscribe(interactor);
                interactor.SetInteractionOverride(this, false);
            }
        }

        private void Unsubscribe(LocalCoopItemInteractor interactor)
        {
            if (interactor != null && interactor.InteractAction != null)
                interactor.InteractAction.performed -= HandleInteractPressed;
        }

        private void OnValidate()
        {
            if (spawnArea == null)
                spawnArea = GetComponent<Collider>();
        }
    }
}
