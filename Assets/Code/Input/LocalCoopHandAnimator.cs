using UnityEngine;
using ChefGame.Items;

namespace ChefGame.Input
{
    /// <summary>Copies this player's held-item state to a bool parameter on a hand visual Animator.</summary>
    [DisallowMultipleComponent]
    public sealed class LocalCoopHandAnimator : MonoBehaviour
    {
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private string holdingParameter = "IsHoldingItem";

        private LocalCoopItemInteractor itemInteractor;
        private int holdingParameterHash;
        private bool hasValidParameter;
        private bool warnedAboutMissingParameter;

        private void Awake()
        {
            if (targetAnimator == null)
                targetAnimator = GetComponent<Animator>();

            itemInteractor = GetComponentInParent<LocalCoopItemInteractor>();
            holdingParameterHash = Animator.StringToHash(holdingParameter);
            CacheParameterValidity();
        }

        private void Update()
        {
            if (targetAnimator == null || itemInteractor == null || !hasValidParameter)
                return;

            targetAnimator.SetBool(holdingParameterHash, itemInteractor.IsHoldingItem);
        }

        private void CacheParameterValidity()
        {
            hasValidParameter = false;
            if (targetAnimator == null || string.IsNullOrWhiteSpace(holdingParameter))
                return;

            foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
            {
                if (parameter.nameHash == holdingParameterHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasValidParameter = true;
                    return;
                }
            }

            if (!warnedAboutMissingParameter)
            {
                Debug.LogWarning($"Animator on '{targetAnimator.name}' has no bool parameter named '{holdingParameter}'. Add it to the Animator Controller or update {nameof(LocalCoopHandAnimator)}.", targetAnimator);
                warnedAboutMissingParameter = true;
            }
        }

        private void OnValidate()
        {
            if (targetAnimator == null)
                targetAnimator = GetComponent<Animator>();

            holdingParameterHash = Animator.StringToHash(holdingParameter ?? string.Empty);
        }
    }
}
