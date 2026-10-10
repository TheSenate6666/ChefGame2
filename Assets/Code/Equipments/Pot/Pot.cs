using System;
using UnityEngine; 
using System.Collections.Generic;
using ChefGame.Items;
using ChefGame.Recipes;


namespace ChefGame.Equipments
{
    public class Pot : MonoBehaviour
    {
        #region Recipe Listing
        [SerializeField] private Recipe activeRecipe;
        #endregion

        #region RequiredIngr and Counting the ings.
        private Dictionary<IngredientType,float> requiredIngredients = new Dictionary<IngredientType, float>();
        private Dictionary<IngredientType,float> currentIngredients = new Dictionary<IngredientType, float>();
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
            activeRecipe.isActive = true;
            requiredIngredients.Clear(); //List clearing
            currentIngredients.Clear();

            if (activeRecipe == null)
            {
                Debug.LogError("No Active Recipe Found");
                return;
            }

            foreach (RecipeLine line in activeRecipe.recipeLines)
            {
                if (!requiredIngredients.ContainsKey(line.ingredientType))
                {
                    requiredIngredients.Add(line.ingredientType,line.amount);
                }

                else
                {
                    requiredIngredients[line.ingredientType] += line.amount; // if type already there, just increase the number
                }
            }
        }
        #endregion

        public void OnTriggerEnter(Collider other)
        {
            IngredientInstance ingredient = other.GetComponent<IngredientInstance>();
            if (ingredient != null &&  !isLidCoverClosed)
            {
                ProcessItem(ingredient);
            }
        }

        private void ProcessItem(IngredientInstance ingredient)
        {
            IngredientType type = ingredient.GetIngredientType();
            
            if (type == IngredientType.None) {Debug.LogError($"Ingredient type none: {ingredient.gameObject.name}"); return; }

            float quantity = 1f;
            if (currentIngredients.ContainsKey(type))
            {
                currentIngredients[type] += quantity;
            }
            else 
            { 
                currentIngredients.Add(type,quantity);
            }
            
            if (requiredIngredients.ContainsKey(type) && requiredIngredients[type] > 0)
            {
                requiredIngredients[type] -= quantity;
                Debug.Log($"Added {type}. Remaining needed: {requiredIngredients[type]}");
            }

            Destroy(ingredient.gameObject);
        }

        private void ToggleLidCover(bool isLidCovered)
        {
            isLidCoverClosed = isLidCovered;
            activeRecipe.isActive = false;
            CheckNextRecipe();    
        }
        
        private void CheckNextRecipe()
        {
            if (activeRecipe.nextRecipe == null) {Debug.Log("No recipes left");}
            activeRecipe = activeRecipe.nextRecipe;
            LoadRecipe(activeRecipe);
        }
    }
}
    
