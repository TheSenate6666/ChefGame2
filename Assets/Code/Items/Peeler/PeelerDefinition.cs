using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Peeler", menuName = "Chef Game/Items/Utensils/Peeler")]
    public sealed class PeelerDefinition : UtensilDefinition
    {
        [SerializeField] private bool peelsIngredients = true;

        public override ItemType TypeId => ItemType.Peeler;
        public bool PeelsIngredients => peelsIngredients;
    }
}
