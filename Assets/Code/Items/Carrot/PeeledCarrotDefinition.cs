using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Peeled Carrot", menuName = "Chef Game/Items/Ingredients/Peeled Carrot")]
    public sealed class PeeledCarrotDefinition : IngredientDefinition
    {
        public override ItemType TypeId => ItemType.PeeledCarrot;
    }
}
