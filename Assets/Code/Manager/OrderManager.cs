using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ChefGame.Input;
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
        [SerializeField] private TextMeshProUGUI viewerCountText;

        [Header("Realistic Stream Fluctuation")]
        [SerializeField] private float minJitterDelay = 0.25f;
        [SerializeField] private float maxJitterDelay = 0.75f;
        [SerializeField] private int maxFluctuationOffset = 4;

        [Header("AFK / Idle Viewer Drop")]
        [SerializeField] private float idleTimeout = 20f;
        [SerializeField] private float idleDrainRatePerSecond = 3f;
        [SerializeField] private int minViewerCount = 0;

        private readonly List<OrderCardUI> activeOrders = new List<OrderCardUI>();

        private float baseViewerCount = 100f;
        private int currentJitterOffset = 0;
        private float nextJitterTime = 0f;
        private int lastRenderedCount = -1;
        private float idleTimer;

        public int ViewerCount => Mathf.RoundToInt(baseViewerCount);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            LocalCoopPlayerController.OnAnyInputDetected += ResetIdleTimer;
        }

        private void OnDisable()
        {
            LocalCoopPlayerController.OnAnyInputDetected -= ResetIdleTimer;
        }

        private void Start()
        {
            lastRenderedCount = Mathf.RoundToInt(baseViewerCount);
            UpdateViewerDisplay(lastRenderedCount);
            InitializeOrderCards();
        }

        private void Update()
        {
            HandleIdlePenalty();
            HandleViewerFluctuation();
        }

        private void ResetIdleTimer()
        {
            idleTimer = 0f;
        }

        private void HandleIdlePenalty()
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleTimeout)
            {
                if (baseViewerCount > minViewerCount)
                {
                    baseViewerCount = Mathf.Max(minViewerCount, baseViewerCount - (idleDrainRatePerSecond * Time.deltaTime));
                }
            }
        }

        private void HandleViewerFluctuation()
        {
            if (Time.time >= nextJitterTime)
            {
                nextJitterTime = Time.time + Random.Range(minJitterDelay, maxJitterDelay);
                
                int step = Random.Range(-1, 2);
                currentJitterOffset = Mathf.Clamp(currentJitterOffset + step, -maxFluctuationOffset, maxFluctuationOffset);
            }

            int targetDisplay = Mathf.Max(minViewerCount, Mathf.RoundToInt(baseViewerCount) + currentJitterOffset);

            if (targetDisplay != lastRenderedCount)
            {
                lastRenderedCount = targetDisplay;
                UpdateViewerDisplay(targetDisplay);
            }
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
            Debug.Log($"Order expired! Current viewers: {baseViewerCount}");

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
                    int gainedViewers = Mathf.RoundToInt(50f * qualityMultiplier);
                    ModifyViewers(gainedViewers);

                    Debug.Log($"Order completed! Gained: +{gainedViewers} viewers. Current total: {baseViewerCount}");

                    OrderCardUI completedCard = activeOrders[i];
                    activeOrders.RemoveAt(i);
                    Destroy(completedCard.gameObject);

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
            baseViewerCount = Mathf.Max(minViewerCount, baseViewerCount + amount);
        }

        private void UpdateViewerDisplay(int count)
        {
            if (viewerCountText != null)
            {
                viewerCountText.text = count.ToString();
            }
        }
    }
}