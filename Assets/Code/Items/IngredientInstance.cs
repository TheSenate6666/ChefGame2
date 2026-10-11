using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>Runtime state for a physical ingredient instance.</summary>
    [DisallowMultipleComponent]
    public sealed class IngredientInstance : ItemInstance
    {
        [SerializeField, HideInInspector] private bool hasBeenCut;
        [SerializeField, HideInInspector] private bool hasBeenPeeled;
        public bool isBeingHeld;

        [SerializeField] private IngredientType ingredientTypeOverride = IngredientType.None;
        private bool warnedMissingCutResults;

        private IngredientDefinition IngredientData => Definition as IngredientDefinition;
        public bool HasBeenCut => hasBeenCut;

        /// <summary>Returns this ingredient's definition name, or an empty string if no definition is assigned.</summary>
        public string GetName() => IngredientData != null ? IngredientData.GetName() : string.Empty;

        /// <returns>True only when cut results were spawned and this ingredient was scheduled for removal.</returns>
        
        public IngredientType GetIngredientType()
        {
            if (ingredientTypeOverride != IngredientType.None)
                return ingredientTypeOverride;

            return IngredientData != null ? IngredientData.GetIngredientType() : IngredientType.None;
        }
        
        public bool TryGetCut()
        {
            IngredientDefinition ingredientData = IngredientData;
            if (hasBeenCut || ingredientData == null || !ingredientData.CanBeCut)
                return false;

            if (!ingredientData.HasCutResults)
            {
                if (!warnedMissingCutResults)
                {
                    Debug.LogWarning($"'{name}' uses ingredient definition '{ingredientData.CustomName}', but no cut-result prefabs are configured. The ingredient was left intact.", this);
                    warnedMissingCutResults = true;
                }

                return false;
            }

            if (!ingredientData.GettingCut(gameObject))
                return false;

            hasBeenCut = true;
            return true;
        }

        public bool TryGetPeeled()
        {
            IngredientDefinition ingredientData = IngredientData;
            if (hasBeenPeeled || ingredientData == null || !ingredientData.CanBePeeled)
                return false;

            if (!ingredientData.GettingPeeled(gameObject))
                return false;

            hasBeenPeeled = true;
            return true;
        }
    }
}
