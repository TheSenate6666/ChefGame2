using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Onion", menuName = "Chef Game/Items/Ingredients/Onion")]
    public sealed class OnionDefinition : IngredientDefinition
    {
        public override ItemType TypeId => ItemType.Onion;
        public override bool CanBeCut => false;
    }
}
