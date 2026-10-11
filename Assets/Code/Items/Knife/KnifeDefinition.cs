using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Knife", menuName = "Chef Game/Items/Utensils/Knife")]
    public sealed class KnifeDefinition : UtensilDefinition
    {
        [SerializeField] private bool cutsIngredients = true;
        [SerializeField] private List<string> cuttableIngredientNames = new List<string> { "Tomato" };
        [Header("Knife effect")]
        [SerializeField] private GameObject knifeEffect;

        public override ItemType TypeId => ItemType.Knife;
        public bool CutsIngredients => cutsIngredients;
        public GameObject KnifeEffect => knifeEffect;

        public bool CanCut(string ingredientName)
        {
            if (!cutsIngredients || string.IsNullOrWhiteSpace(ingredientName) || cuttableIngredientNames == null)
                return false;

            for (int i = 0; i < cuttableIngredientNames.Count; i++)
            {
                if (string.Equals(cuttableIngredientNames[i]?.Trim(), ingredientName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
