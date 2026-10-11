using System;
using UnityEngine;
using System.Collections.Generic;
using ChefGame.Items; 
using ChefGame.Recipes;
using Unity.Collections; 

namespace ChefGame.Equipments
{
    public class Pot : MonoBehaviour
    {
        #region Recipe Listing
        [SerializeField] private Recipe activeRecipe;
        #endregion

        #region Required and Current Ingredients
        
        [SerializeField, ReadOnly] private List<RecipeLine> requiredIngredients = new List<RecipeLine>();
        [SerializeField, ReadOnly] private List<RecipeLine> currentIngredients = new List<RecipeLine>();
        #endregion
        
        private bool isLidCoverClosed;

        #region Unity Functions
        private void OnEnable()
        {
            Stove.OnStoveToggled += ToggleLidCover;
        }

        private void OnDisable()
        {
            Stove.OnStoveToggled -= ToggleLidCover;
        }

        private void Start()
        {
            if (activeRecipe != null)
            {
                LoadRecipe(activeRecipe);
            }
        }
        #endregion

        #region Load Recipe
        private void LoadRecipe(Recipe newRecipe)
        {
            activeRecipe = newRecipe;
            requiredIngredients.Clear();
            currentIngredients.Clear();

            if (activeRecipe == null)
            {
                Debug.LogError("No Active Recipe Found");
                return;
            }

            activeRecipe.isActive = true;

            // Populate the required list by creating copies of the recipe lines
            foreach (RecipeLine line in activeRecipe.recipeLines)
            {
                RecipeLine existingLine = requiredIngredients.Find(r => r.ingredientType == line.ingredientType); 
                if (existingLine == null)
                {
                    requiredIngredients.Add(new RecipeLine { ingredientType = line.ingredientType, amount = line.amount }); 
                }
                else
                {
                    existingLine.amount += line.amount; 
                }
            }
        }
        #endregion

        public void OnTriggerEnter(Collider other)
        {
            IngredientInstance ingredient = other.GetComponent<IngredientInstance>();
            if (ingredient != null && !isLidCoverClosed && !ingredient.isBeingHeld)
            {
                ProcessItem(ingredient);
                Debug.Log($"{ingredient.GetName()} has been added to the pot");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            IngredientInstance ingredient = other.GetComponent<IngredientInstance>();
            if (ingredient != null && !isLidCoverClosed && !ingredient.isBeingHeld)
            {
                ProcessItem(ingredient);
                Debug.Log($"{ingredient.GetName()} has been added to the pot");
            }
        }

        private void ProcessItem(IngredientInstance ingredient)
        {
            IngredientType type = ingredient.GetIngredientType();
            
            if (type == IngredientType.None) 
            {
                Debug.LogError($"Ingredient type none: {ingredient.gameObject.name}"); 
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

            
            RecipeLine reqLine = requiredIngredients.Find(item => item.ingredientType == type); 
            if (reqLine != null && reqLine.amount > 0)
            {
                reqLine.amount -= quantity; 
                Debug.Log($"Added {type}. Remaining needed: {reqLine.amount}"); 
            }

            Destroy(ingredient.gameObject);
        }

        private void ToggleLidCover(bool isLidCovered)
        {
            isLidCoverClosed = isLidCovered;
            if (activeRecipe != null)
            {
                activeRecipe.isActive = false;
            }
            CheckNextRecipe();    
        }
        
        private void CheckNextRecipe()
        {
            if (activeRecipe == null || activeRecipe.nextRecipe == null) 
            {
                Debug.Log("No recipes left");
                return;
            }
            
            activeRecipe = activeRecipe.nextRecipe;
            LoadRecipe(activeRecipe);
        }
    }
}