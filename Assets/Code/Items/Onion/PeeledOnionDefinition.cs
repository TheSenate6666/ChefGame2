using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Peeled Onion", menuName = "Chef Game/Items/Ingredients/Peeled Onion")]
    public sealed class PeeledOnionDefinition : IngredientDefinition
    {
        public override ItemType TypeId => ItemType.PeeledOnion;
    }
}
