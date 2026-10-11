using System.Collections.Generic;
using UnityEngine;
using ChefGame.Items;

namespace ChefGame.Recipes
{
    public struct RecipeMatchResult
    {
        public Recipe MatchedRecipe;
        public float QualityMultiplier; // 1.0f = %100, 0.5f = %50 score etc.
        public bool IsValid => MatchedRecipe != null;
    }

    public static class RecipeMatcher
    {
        public static RecipeMatchResult EvaluateIngredients(
            List<Recipe> allRecipes,
            Dictionary<IngredientType, float> currentIngredients)
        {
            Recipe bestRecipe = null;
            float highestQuality = 0f;

            foreach (Recipe recipe in allRecipes)
            {
                if (CanBeRecipe(recipe, currentIngredients, out float quality))
                {
                    if (quality > highestQuality)
                    {
                        highestQuality = quality;
                        bestRecipe = recipe;
                    }
                }
            }

            return new RecipeMatchResult
            {
                MatchedRecipe = bestRecipe,
                QualityMultiplier = Mathf.Clamp01(highestQuality)
            };
        }

        private static bool CanBeRecipe(
            Recipe recipe, 
            Dictionary<IngredientType, float> ingredients, 
            out float qualityMultiplier)
        {
            qualityMultiplier = 0f;

            
            foreach (RecipeLine line in recipe.recipeLines)
            {
                if (!ingredients.TryGetValue(line.ingredientType, out float amount))
                    return false; 

                if (amount < line.amount)
                    return false; 
            }

            
            float penalty = 0f;

            foreach (var kvp in ingredients)
            {
                IngredientType type = kvp.Key;
                float currentAmount = kvp.Value;

                RecipeLine targetLine = recipe.recipeLines.Find(l => l.ingredientType == type);

                if (targetLine != null)
                {
                    
                    if (currentAmount > targetLine.amount)
                    {
                        float excess = currentAmount - targetLine.amount;
                        
                        penalty += (excess / targetLine.amount) * 0.25f; 
                    }
                }
                else
                {
                   
                    penalty += 0.35f; 
                }
            }

            
            qualityMultiplier = Mathf.Max(0.1f, 1f - penalty);
            return true;
        }
    }
}