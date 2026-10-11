using UnityEngine;
using System.Collections.Generic;
using ChefGame.Items;

namespace ChefGame.Recipes 
{
    [System.Serializable]
    public class RecipeLine
    {
        public IngredientType  ingredientType;
        public float amount; 
    }
}
