using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Peeler", menuName = "Chef Game/Items/Utensils/Peeler")]
    public sealed class PeelerDefinition : UtensilDefinition
    {
        [SerializeField] private bool peelsIngredients = true;
        [Header("Peeler effect")]
        [SerializeField] private GameObject peelerEffect;

        public override ItemType TypeId => ItemType.Peeler;
        public bool PeelsIngredients => peelsIngredients;
        public GameObject PeelerEffect => peelerEffect;
    }
}
