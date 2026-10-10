using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>A distinct peeled-onion piece that, like a tomato half, cannot be cut again.</summary>
    [CreateAssetMenu(fileName = "Peeled Onion Half", menuName = "Chef Game/Items/Ingredients/Peeled Onion Half")]
    public sealed class PeeledOnionHalfDefinition : IngredientDefinition
    {
        public override ItemType TypeId => ItemType.PeeledOnionHalf;
        public override bool CanBeCut => false;
    }
}
