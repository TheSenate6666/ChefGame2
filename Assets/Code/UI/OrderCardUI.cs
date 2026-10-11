using System;
using UnityEngine;
using UnityEngine.UI;
using ChefGame.Recipes;

namespace ChefGame.UI
{
    public class OrderCardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image timerFillImage;

        private float remainingTime;
        private float totalTime;
        private bool isCounting;

        public Recipe BoundRecipe { get; private set; }
        public event Action<OrderCardUI> OnOrderExpired;

        public void Setup(Recipe recipe)
        {
            BoundRecipe = recipe;

            if (cardImage != null)
                cardImage.sprite = recipe.recipeIcon;

            totalTime = recipe.orderDuration;
            remainingTime = recipe.orderDuration;

            if (timerFillImage != null)
                timerFillImage.fillAmount = 1f;

            isCounting = true;
        }

        private void Update()
        {
            if (!isCounting) return;

            remainingTime -= Time.deltaTime;

            if (timerFillImage != null && totalTime > 0f)
            {
                timerFillImage.fillAmount = Mathf.Clamp01(remainingTime / totalTime);
            }

            if (remainingTime <= 0f)
            {
                isCounting = false;
                OnOrderExpired?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}