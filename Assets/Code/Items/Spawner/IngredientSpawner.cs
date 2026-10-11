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
                interactor.SetSpawnerInteraction(this, false);
            }
            subscribedInteractors.Clear();
            occupants.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || occupants.ContainsKey(other))
                return;

            LocalCoopItemInteractor interactor = other.GetComponentInParent<LocalCoopItemInteractor>();
            if (interactor == null)
                return;

            occupants.Add(other, interactor);
            if (subscribedInteractors.Add(interactor))
            {
                interactor.SetSpawnerInteraction(this, true);
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

            if (subscribedInteractors.Remove(interactor))
            {
                Unsubscribe(interactor);
                interactor.SetSpawnerInteraction(this, false);
            }
        }

        private void HandleInteractPressed(InputAction.CallbackContext context)
        {
            LocalCoopItemInteractor interactor = context.action != null
                ? FindInteractorForAction(context.action)
                : null;
            if (interactor == null || interactor.IsHoldingItem)
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
