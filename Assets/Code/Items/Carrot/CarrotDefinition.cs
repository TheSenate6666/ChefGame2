using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Carrot", menuName = "Chef Game/Items/Ingredients/Carrot")]
    public sealed class CarrotDefinition : IngredientDefinition
    {
        public override ItemType TypeId => ItemType.Carrot;
        public override bool CanBeCut => false;
    }
}
