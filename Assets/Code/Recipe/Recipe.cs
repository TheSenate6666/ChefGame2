using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace ChefGame.Recipes
{
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe/New Recipe")]
    public class Recipe : ScriptableObject
    {
        [Header("UI")]
        public string recipeName;
        public Sprite recipeIcon;
        
        [Header("Recipe Settings")]
        [Range(1,5)] public int difficulty;

        public float orderDuration;
        
        [Header("Recipe Settings")]
        public List<RecipeLine> recipeLines = new List<RecipeLine>();
        [FormerlySerializedAs("isActive")] public bool isCurrentRecipe;
        
    }
}
    
