using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Cut Carrot", menuName = "Chef Game/Items/Ingredients/Cut Carrot")]
    public sealed class CutCarrotDefinition : IngredientDefinition
    {
        public override ItemType TypeId => ItemType.QuarterCarrot;
        public override bool CanBeCut => false;
    }
}
