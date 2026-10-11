using System.Collections.Generic;
using UnityEngine;
using ChefGame.Recipes;
using ChefGame.UI;

namespace ChefGame.Manager
{
    public class OrderManager : MonoBehaviour
    {
        [SerializeField] private int maxConcurrentOrders = 4;
        [SerializeField] private Transform orderContainer;
        [SerializeField] private GameObject orderCardPrefab;
        [SerializeField] private List<Recipe> availableRecipes;

        // Tracks the currently active 3-4 UI order cards
        private readonly List<OrderCardUI> activeOrders = new();

        public bool CanSpawnOrder => activeOrders.Count < maxConcurrentOrders;

        public void TrySpawnOrder()
        {
            if (!CanSpawnOrder || availableRecipes.Count == 0)
                return;

            Recipe recipe = availableRecipes[Random.Range(0, availableRecipes.Count)];
            GameObject cardObj = Instantiate(orderCardPrefab, orderContainer);

            if (cardObj.TryGetComponent(out OrderCardUI cardUI))
            {
                cardUI.Setup(recipe);
                activeOrders.Add(cardUI);
            }
        }

        public bool TryDeliverRecipe(Recipe deliveredRecipe)
        {
            for (int i = 0; i < activeOrders.Count; i++)
            {
                if (activeOrders[i].BoundRecipe == deliveredRecipe)
                {
                    // Remove matching order regardless of its index
                    Destroy(activeOrders[i].gameObject);
                    activeOrders.RemoveAt(i);
                    return true;
                }
            }

            return false; // No matching active order found
        }

        public void HandleOrderExpired(OrderCardUI expiredCard)
        {
            if (activeOrders.Remove(expiredCard))
            {
                Destroy(expiredCard.gameObject);
            }
        }
    }
}