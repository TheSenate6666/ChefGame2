using System.Collections.Generic;
using UnityEngine;
using ChefGame.Items;

namespace ChefGame.Recipes
{
    public struct RecipeMatchResult
    {
        public Recipe MatchedRecipe;
        public float QualityMultiplier;
        public bool IsValid => MatchedRecipe != null;
    }

    public static class RecipeMatcher
    {
        public static RecipeMatchResult EvaluateIngredients(
            List<Recipe> allRecipes,
            Dictionary<IngredientType, float> currentIngredients,
            float excessPenaltyRate = 0.25f,
            float unrelatedPenaltyFlat = 0.35f,
            float minQualityFloor = 0.1f)
        {
            Recipe bestRecipe = null;
            float highestQuality = 0f;

            foreach (Recipe recipe in allRecipes)
            {
                if (CanBeRecipe(recipe, currentIngredients, excessPenaltyRate, unrelatedPenaltyFlat, minQualityFloor, out float quality))
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
            float excessPenaltyRate,
            float unrelatedPenaltyFlat,
            float minQualityFloor,
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
                        penalty += (excess / targetLine.amount) * excessPenaltyRate; 
                    }
                }
                else
                {
                    penalty += unrelatedPenaltyFlat; 
                }
            }

            qualityMultiplier = Mathf.Max(minQualityFloor, 1f - penalty);
            return true;
        }
    }
}