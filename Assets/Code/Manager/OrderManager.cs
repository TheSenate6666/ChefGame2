using System.Collections.Generic;
using UnityEngine;
using ChefGame.Recipes;
using ChefGame.UI;

namespace ChefGame.Managers
{
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private int maxConcurrentOrders = 4;
        [SerializeField] private float orderSpawnInterval = 7f;
        [SerializeField] private List<Recipe> availableRecipes = new List<Recipe>();

        [Header("UI References")]
        [SerializeField] private Transform orderContainer;
        [SerializeField] private GameObject orderCardPrefab;

        private readonly List<OrderCardUI> activeOrders = new List<OrderCardUI>();
        private float spawnTimer;
        public int ViewerCount { get; private set; } = 100;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            for (int i = 0; i < 3; i++)
            {
                SpawnOrder();
            }
        }

        private void Update()
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= orderSpawnInterval)
            {
                spawnTimer = 0f;
                if (activeOrders.Count < maxConcurrentOrders)
                {
                    SpawnOrder();
                }
            }
        }

        private void SpawnOrder()
        {
            if (availableRecipes.Count == 0 || orderCardPrefab == null) return;

            Recipe randomRecipe = availableRecipes[Random.Range(0, availableRecipes.Count)];
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

                    Destroy(activeOrders[i].gameObject);
                    activeOrders.RemoveAt(i);
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