using System.Collections.Generic;
using UnityEngine;
using ChefGame.Items;
using ChefGame.Recipes;

namespace ChefGame.Equipments
{
    public class Pot : MonoBehaviour
    {
        [Header("Recipe Database")]
        [SerializeField] private List<Recipe> availableRecipes = new List<Recipe>();

        [Header("Runtime State")]
        [SerializeField] private Recipe matchedRecipe;
        [SerializeField] private float cookingQuality = 1f;
        [SerializeField] private bool isCooking;

        private readonly List<RecipeLine> currentIngredients = new List<RecipeLine>();
        private bool isLidCoverClosed;

        public Recipe MatchedRecipe => matchedRecipe;
        public float CookingQuality => cookingQuality;
        public bool IsCooking => isCooking;

        private void OnEnable()
        {
            Stove.OnStoveToggled += ToggleLidCover;
        }

        private void OnDisable()
        {
            Stove.OnStoveToggled -= ToggleLidCover;
        }

        public void OnTriggerEnter(Collider other)
        {
            TryAddIngredient(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryAddIngredient(other);
        }

        private void TryAddIngredient(Collider other)
        {
            if (isLidCoverClosed || isCooking) return;

            IngredientInstance ingredient = other.GetComponent<IngredientInstance>();
            if (ingredient != null && !ingredient.isBeingHeld)
            {
                ProcessItem(ingredient);
            }
        }

        private void ProcessItem(IngredientInstance ingredient)
        {
            IngredientType type = ingredient.GetIngredientType();

            if (type == IngredientType.None)
            {
                Debug.LogError($"Ingredient type none: {ingredient.gameObject.name}", this);
                return;
            }

            float quantity = 1f;

            RecipeLine currentLine = currentIngredients.Find(item => item.ingredientType == type);
            if (currentLine != null)
            {
                currentLine.amount += quantity;
            }
            else
            {
                currentIngredients.Add(new RecipeLine { ingredientType = type, amount = quantity });
            }

            Debug.Log($"{ingredient.GetName()} added to the pot. Total {type}: {currentLine?.amount ?? quantity}");
            Destroy(ingredient.gameObject);
        }

        private void ToggleLidCover(bool isLidCovered)
        {
            isLidCoverClosed = isLidCovered;

            if (isLidCovered)
            {
                EvaluateRecipe();
            }
            else
            {
                StopCooking();
            }
        }

        private void EvaluateRecipe()
        {
            if (currentIngredients.Count == 0)
            {
                Debug.Log("Stove turned on, but the pot is empty.");
                return;
            }

            // Convert List<RecipeLine> to Dictionary<IngredientType, float>
            var ingredientMap = new Dictionary<IngredientType, float>();
            foreach (RecipeLine line in currentIngredients)
            {
                if (ingredientMap.ContainsKey(line.ingredientType))
                    ingredientMap[line.ingredientType] += line.amount;
                else
                    ingredientMap[line.ingredientType] = line.amount;
            }

            RecipeMatchResult result = RecipeMatcher.EvaluateIngredients(availableRecipes, ingredientMap);

            if (result.IsValid)
            {
                matchedRecipe = result.MatchedRecipe;
                cookingQuality = result.QualityMultiplier;
                isCooking = true;

                Debug.Log($"Cooking started: {matchedRecipe.recipeName} | Quality: %{(cookingQuality * 100):0}");
            }
            else
            {
                matchedRecipe = null;
                cookingQuality = 0f;
                isCooking = false;

                Debug.LogWarning("Required ingredients are missing. Stove turned on with an invalid mix.");
            }
        }

        private void StopCooking()
        {
            isCooking = false;
            Debug.Log("Cooking paused / stopped.");
        }

        public void ClearPot()
        {
            currentIngredients.Clear();
            matchedRecipe = null;
            cookingQuality = 1f;
            isCooking = false;
        }
    }
}