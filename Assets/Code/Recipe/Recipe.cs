using UnityEngine;
using System.Collections.Generic;

namespace ChefGame.Recipes
{
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe/New Recipe")]
    public class Recipe : ScriptableObject
    {
        [Range(1,5)] public int difficulty;
        public string recipeName;
        public List<RecipeLine> recipeLines = new List<RecipeLine>();
        public bool isActive;
        public Recipe nextRecipe; // Linked List - One-Way
    }
}
    
