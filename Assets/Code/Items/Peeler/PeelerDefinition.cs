using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Peeler", menuName = "Chef Game/Items/Utensils/Peeler")]
    public sealed class PeelerDefinition : UtensilDefinition
    {
        [SerializeField] private bool peelsIngredients = true;
        [SerializeField] private List<string> peelableIngredientNames = new List<string> { "Onion" };

        public override ItemType TypeId => ItemType.Peeler;

        public bool CanPeel(string ingredientName)
        {
            if (!peelsIngredients || string.IsNullOrWhiteSpace(ingredientName) || peelableIngredientNames == null)
                return false;
            for (int i = 0; i < peelableIngredientNames.Count; i++)
                if (string.Equals(peelableIngredientNames[i]?.Trim(), ingredientName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
