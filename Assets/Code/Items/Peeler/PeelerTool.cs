using UnityEngine;

namespace ChefGame.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class PeelerTool : MonoBehaviour
    {
        [SerializeField] private string itemTag = "Item";
        private PeelerDefinition definition;

        private void Awake()
        {
            if (!GetComponent<Collider>().isTrigger)
                Debug.LogError($"{nameof(PeelerTool)} on '{name}' requires its Collider to have Is Trigger enabled.", this);
            ItemInstance itemInstance = GetComponentInParent<ItemInstance>();
            definition = itemInstance != null ? itemInstance.Definition as PeelerDefinition : null;
            if (definition == null)
                Debug.LogError($"{nameof(PeelerTool)} on '{name}' needs a parent {nameof(ItemInstance)} assigned a {nameof(PeelerDefinition)} asset.", this);
        }

        private void OnTriggerEnter(Collider other) => TryPeelIngredient(other);
        private void OnTriggerStay(Collider other) => TryPeelIngredient(other);

        private void TryPeelIngredient(Collider other)
        {
            if (definition == null || other == null)
                return;
            IngredientInstance ingredient = other.GetComponentInParent<IngredientInstance>();
            if (!definition.PeelsIngredients || ingredient == null || !ingredient.CompareTag(itemTag))
                return;

            // The ingredient owns the peelability check. This makes the trigger work
            // for any ingredient whose definition enables CanBePeeled, including carrots.
            ingredient.TryGetPeeled();
        }
    }
}
