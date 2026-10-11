using System.Collections.Generic;
using ChefGame.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChefGame.Items
{
    /// <summary>Returns the active knife and peeler to their assigned tool-station positions on Interact.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ToolResetStation : MonoBehaviour
    {
        [SerializeField] private Collider interactionArea;
        [SerializeField] private Transform knifeSpawn;
        [SerializeField] private Transform peelerSpawn;

        private readonly Dictionary<Collider, LocalCoopItemInteractor> occupants = new();
        private readonly HashSet<LocalCoopItemInteractor> subscribedInteractors = new();

        private void Awake()
        {
            if (interactionArea == null)
                interactionArea = GetComponent<Collider>();

            if (interactionArea == null || !interactionArea.isTrigger)
                Debug.LogError($"{nameof(ToolResetStation)} on '{name}' requires an interaction trigger collider.", this);
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
            if (other == null || !other.CompareTag("Hand") || occupants.ContainsKey(other))
                return;

            LocalCoopItemInteractor interactor = other.GetComponentInParent<LocalCoopItemInteractor>();
            if (interactor == null)
                return;

            occupants.Add(other, interactor);
            if (!subscribedInteractors.Add(interactor))
                return;

            interactor.SetInteractionOverride(this, true);
            InputAction action = interactor.InteractAction;
            if (action != null)
                action.performed += HandleInteractPressed;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null || !occupants.Remove(other, out LocalCoopItemInteractor interactor))
                return;

            if (HasOccupant(interactor))
                return;

            UnregisterInteractor(interactor);
        }

        private void HandleInteractPressed(InputAction.CallbackContext context)
        {
            LocalCoopItemInteractor interactor = context.action != null
                ? FindInteractorForAction(context.action)
                : null;
            if (interactor == null)
                return;

            if (!IsHandStillInside(interactor))
            {
                RemoveOccupancy(interactor);
                UnregisterInteractor(interactor);
                return;
            }

            ResetTools();
        }

        private void ResetTools()
        {
            if (knifeSpawn == null || peelerSpawn == null)
            {
                Debug.LogWarning($"Assign both Knife Spawn and Peeler Spawn on '{name}'.", this);
                return;
            }

            ItemInstance[] activeItems = FindObjectsByType<ItemInstance>(FindObjectsSortMode.None);
            LocalCoopItemInteractor[] interactors = FindObjectsByType<LocalCoopItemInteractor>(FindObjectsSortMode.None);
            bool foundKnife = false;
            bool foundPeeler = false;

            foreach (ItemInstance item in activeItems)
            {
                if (item == null)
                    continue;

                Transform destination = item.TypeId switch
                {
                    ItemType.Knife => knifeSpawn,
                    ItemType.Peeler => peelerSpawn,
                    _ => null
                };

                if (destination == null)
                    continue;

                if (item.TypeId == ItemType.Knife)
                    foundKnife = true;
                else
                    foundPeeler = true;

                foreach (LocalCoopItemInteractor itemInteractor in interactors)
                    itemInteractor.TryDropHeldItem(item.transform);

                // Ensure even an item whose parent/state became desynchronized is returned to the station.
                item.transform.SetParent(null, true);
                item.transform.SetPositionAndRotation(destination.position, destination.rotation);

                Rigidbody itemRigidbody = item.GetComponent<Rigidbody>();
                if (itemRigidbody != null)
                {
                    itemRigidbody.linearVelocity = Vector3.zero;
                    itemRigidbody.angularVelocity = Vector3.zero;
                }
            }

            if (!foundKnife)
                Debug.LogWarning($"No active knife with an {nameof(ItemInstance)} was found in the scene.", this);
            if (!foundPeeler)
                Debug.LogWarning($"No active peeler with an {nameof(ItemInstance)} was found in the scene.", this);
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

        private bool IsHandStillInside(LocalCoopItemInteractor interactor)
        {
            if (interactionArea == null || !interactionArea.enabled || !interactionArea.gameObject.activeInHierarchy)
                return false;

            Bounds areaBounds = interactionArea.bounds;
            foreach (KeyValuePair<Collider, LocalCoopItemInteractor> occupant in occupants)
            {
                Collider hand = occupant.Key;
                if (occupant.Value == interactor && hand != null && hand.enabled &&
                    hand.gameObject.activeInHierarchy && hand.CompareTag("Hand") && areaBounds.Intersects(hand.bounds))
                    return true;
            }

            return false;
        }

        private bool HasOccupant(LocalCoopItemInteractor interactor)
        {
            foreach (LocalCoopItemInteractor occupant in occupants.Values)
            {
                if (occupant == interactor)
                    return true;
            }

            return false;
        }

        private void RemoveOccupancy(LocalCoopItemInteractor interactor)
        {
            List<Collider> collidersToRemove = new();
            foreach (KeyValuePair<Collider, LocalCoopItemInteractor> occupant in occupants)
            {
                if (occupant.Value == interactor)
                    collidersToRemove.Add(occupant.Key);
            }

            foreach (Collider hand in collidersToRemove)
                occupants.Remove(hand);
        }

        private void UnregisterInteractor(LocalCoopItemInteractor interactor)
        {
            if (!subscribedInteractors.Remove(interactor))
                return;

            Unsubscribe(interactor);
            interactor.SetInteractionOverride(this, false);
        }

        private void Unsubscribe(LocalCoopItemInteractor interactor)
        {
            if (interactor != null && interactor.InteractAction != null)
                interactor.InteractAction.performed -= HandleInteractPressed;
        }

        private void OnValidate()
        {
            if (interactionArea == null)
                interactionArea = GetComponent<Collider>();
        }
    }
}
