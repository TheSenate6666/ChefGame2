using System.Collections.Generic;
using UnityEngine;
using ChefGame.Recipes;
using ChefGame.UI;

namespace ChefGame.Managers
{
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        [Header("Recipe Pool")]
        [SerializeField] private List<Recipe> recipePool = new List<Recipe>();
        [SerializeField] private int maxActiveOrders = 3;

        [Header("UI References")]
        [SerializeField] private Transform orderContainer;
        [SerializeField] private GameObject orderCardPrefab;

        private readonly List<OrderCardUI> activeOrders = new List<OrderCardUI>();
        public int ViewerCount { get; private set; } = 100;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            InitializeOrderCards();
        }

        private void InitializeOrderCards()
        {
            for (int i = 0; i < maxActiveOrders; i++)
            {
                SpawnRandomOrder();
            }
        }

        private void SpawnRandomOrder()
        {
            if (recipePool.Count == 0 || orderCardPrefab == null || orderContainer == null) return;

            Recipe randomRecipe = recipePool[Random.Range(0, recipePool.Count)];
            GameObject cardObj = Instantiate(orderCardPrefab, orderContainer);

            if (cardObj.TryGetComponent(out OrderCardUI cardUI))
            {
                cardUI.Setup(randomRecipe);
                cardUI.OnOrderExpired += HandleOrderExpired;
                activeOrders.Add(cardUI);
            }
        }

        private void HandleOrderExpired(OrderCardUI expiredCard)
        {
            activeOrders.Remove(expiredCard);
            ModifyViewers(-20);
            Debug.Log($"Order expired! Current viewers: {ViewerCount}");

            // Immediately replenish the slot with a new randomized recipe
            SpawnRandomOrder();
        }

        public List<Recipe> GetActiveOrderRecipes()
        {
            var recipes = new List<Recipe>();
            for (int i = 0; i < activeOrders.Count; i++)
            {
                if (activeOrders[i] != null && activeOrders[i].BoundRecipe != null)
                {
                    if (!recipes.Contains(activeOrders[i].BoundRecipe))
                    {
                        recipes.Add(activeOrders[i].BoundRecipe);
                    }
                }
            }
            return recipes;
        }

        public bool TryDeliverDish(Recipe cookedRecipe, float qualityMultiplier)
        {
            for (int i = 0; i < activeOrders.Count; i++)
            {
                if (activeOrders[i].BoundRecipe == cookedRecipe)
                {
                    int gainedViewers = Mathf.RoundToInt(50 * qualityMultiplier);
                    ModifyViewers(gainedViewers);

                    Debug.Log($"Order completed! Gained: +{gainedViewers} viewers. Current total: {ViewerCount}");

                    OrderCardUI completedCard = activeOrders[i];
                    activeOrders.RemoveAt(i);
                    Destroy(completedCard.gameObject);

                    // Immediately replenish the slot with a new randomized recipe
                    SpawnRandomOrder();
                    return true;
                }
            }

            ModifyViewers(-10);
            Debug.LogWarning("Cooked dish does not match any active order! Lost 10 viewers.");
            return false;
        }

        public void HandleBurntMeal()
        {
            ModifyViewers(-30);
            Debug.LogError("Meal burned! Lost 30 viewers.");
        }

        private void ModifyViewers(int amount)
        {
            ViewerCount = Mathf.Max(0, ViewerCount + amount);
        }
    }
}