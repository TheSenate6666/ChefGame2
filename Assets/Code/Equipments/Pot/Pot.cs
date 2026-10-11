using System.Collections.Generic;
using UnityEngine;
using ChefGame.Items;
using ChefGame.Managers;
using ChefGame.Recipes;

namespace ChefGame.Equipments
{
    public enum CookingState
    {
        Idle,
        Underdone,
        Ready,
        Burnt
    }

    public class Pot : MonoBehaviour
    {
        [Header("Cooking Timing Config")]
        [SerializeField] private float cookingTimeWindowStart = 5f;
        [SerializeField] private float burnTimeThreshold = 10f;

        [Header("Runtime State")]
        [SerializeField] private Recipe matchedRecipe;
        [SerializeField] private float cookingQuality = 1f;
        [SerializeField] private CookingState currentState = CookingState.Idle;
        
        [Header("Recipe Assessment Penalties")]
        [SerializeField, Range(0f, 1f)] private float excessPenaltyRate = 0.25f;
        [SerializeField, Range(0f, 1f)] private float unrelatedIngredientPenalty = 0.35f;
        [SerializeField, Range(0f, 0.5f)] private float minimumQualityFloor = 0.10f;

        public List<RecipeLine> currentIngredients = new List<RecipeLine>();
        [SerializeField] private readonly HashSet<IngredientInstance> processedIngredients = new HashSet<IngredientInstance>();
        
        private float cookingTimer;
        private bool isLidCoverClosed;
        private bool isCooking;

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

        // Called directly from PotTriggerZone on the child trigger object
        public void HandleIngredientTrigger(Collider other)
        {
            if (isLidCoverClosed || isCooking) return;

            IngredientInstance ingredient = other.GetComponentInParent<IngredientInstance>();
            
            // Validate: ignore if missing, held by player, or already processed
            if (ingredient == null || ingredient.isBeingHeld || processedIngredients.Contains(ingredient))
                return;

            // Immediately mark as processed to prevent duplicate processing in the same frame
            processedIngredients.Add(ingredient);

            // Disable all child colliders instantly so further physics events are ignored
            Collider[] colliders = ingredient.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            ProcessItem(ingredient);
        }

        private void ProcessItem(IngredientInstance ingredient)
        {
            IngredientType type = ingredient.GetIngredientType();
            if (type == IngredientType.None)
            {
                Debug.LogError($"Invalid ingredient type: {ingredient.gameObject.name}", this);
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

            Debug.Log($"Added {ingredient.GetName()} to pot. Current count for {type}: {currentLine?.amount ?? quantity}");
            Destroy(ingredient.gameObject);
        }

        private void Update()
        {
            if (!isCooking) return;

            cookingTimer += Time.deltaTime;

            if (cookingTimer >= burnTimeThreshold)
            {
                if (currentState != CookingState.Burnt)
                {
                    currentState = CookingState.Burnt;
                    Debug.LogError("The meal has burned!");
                }
            }
            else if (cookingTimer >= cookingTimeWindowStart)
            {
                if (currentState != CookingState.Ready)
                {
                    currentState = CookingState.Ready;
                    Debug.Log("The meal is cooked and ready to serve!");
                }
            }
        }

        private void ToggleLidCover(bool isLidCovered)
        {
            isLidCoverClosed = isLidCovered;

            if (isLidCovered)
            {
                StartCookingProcess();
            }
            else
            {
                FinishOrStopCookingProcess();
            }
        }

        private void StartCookingProcess()
        {
            if (currentIngredients.Count == 0)
            {
                Debug.Log("Stove activated, but the pot is empty.");
                return;
            }

            List<Recipe> activeOrderRecipes = OrderManager.Instance != null 
                ? OrderManager.Instance.GetActiveOrderRecipes() 
                : new List<Recipe>();

            if (activeOrderRecipes.Count == 0)
            {
                Debug.LogWarning("Cannot start cooking: No active orders available.");
                return;
            }

            var ingredientMap = new Dictionary<IngredientType, float>();
            foreach (RecipeLine line in currentIngredients)
            {
                if (ingredientMap.ContainsKey(line.ingredientType))
                    ingredientMap[line.ingredientType] += line.amount;
                else
                    ingredientMap[line.ingredientType] = line.amount;
            }

            RecipeMatchResult result = RecipeMatcher.EvaluateIngredients(
                activeOrderRecipes, 
                ingredientMap, 
                excessPenaltyRate, 
                unrelatedIngredientPenalty, 
                minimumQualityFloor
            );

            if (result.IsValid)
            {
                matchedRecipe = result.MatchedRecipe;
                cookingQuality = result.QualityMultiplier;
                isCooking = true;
                cookingTimer = 0f;
                currentState = CookingState.Underdone;
                Debug.Log($"Started cooking active order: {matchedRecipe.recipeName} | Quality: {(cookingQuality * 100):0}%");
            }
            else
            {
                matchedRecipe = null;
                isCooking = false;
                Debug.LogWarning("Pot contents do not match any active order.");
            }
        }

        private void FinishOrStopCookingProcess()
        {
            if (!isCooking) return;

            isCooking = false;

            switch (currentState)
            {
                case CookingState.Ready:
                    OrderManager.Instance.TryDeliverDish(matchedRecipe, cookingQuality);
                    ClearPot();
                    break;

                case CookingState.Burnt:
                    OrderManager.Instance.HandleBurntMeal();
                    ClearPot();
                    break;

                case CookingState.Underdone:
                    Debug.LogWarning("Stove turned off too early: Food is still raw.");
                    break;
            }
        }

        public void ClearPot()
        {
            currentIngredients.Clear();
            processedIngredients.Clear();
            matchedRecipe = null;
            cookingQuality = 1f;
            cookingTimer = 0f;
            currentState = CookingState.Idle;
            isCooking = false;
        }
    }
}