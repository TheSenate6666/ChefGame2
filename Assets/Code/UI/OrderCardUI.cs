using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChefGame.Recipes;

namespace ChefGame.UI
{
    public class OrderCardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image dishIcon;
        [SerializeField] private TextMeshProUGUI dishNameText;
        [SerializeField] private Image timerFillBar;

        private float remainingTime;
        private float totalTime;
        private bool isCounting;

        public Recipe BoundRecipe { get; private set; }

        public void Setup(Recipe recipe)
        {
            BoundRecipe = recipe;

            if (dishIcon != null)
                dishIcon.sprite = recipe.recipeIcon;

            if (dishNameText != null)
                dishNameText.text = recipe.recipeName;

            totalTime = recipe.orderDuration;
            remainingTime = recipe.orderDuration;
            isCounting = true;
        }

        private void Update()
        {
            if (!isCounting) return;

            remainingTime -= Time.deltaTime;

            if (timerFillBar != null && totalTime > 0f)
            {
                timerFillBar.fillAmount = remainingTime / totalTime;
            }

            if (remainingTime <= 0f)
            {
                isCounting = false;
                Destroy(gameObject);
            }
        }
    }
}