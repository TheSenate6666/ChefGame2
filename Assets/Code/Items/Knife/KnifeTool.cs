using UnityEngine;
using MoreMountains.Feedbacks;

namespace ChefGame.Items
{
    /// <summary>Put this on the knife's trigger-zone object. It cuts each cuttable ingredient once on contact.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class KnifeTool : MonoBehaviour
    {
        [SerializeField] private string itemTag = "Item";

        private Collider cuttingZone;
        private KnifeDefinition definition;
        private Transform vfxLocation;

        private MMF_Player cutFeedback;

        private void Start()
        {
            cutFeedback = GameObject.Find("CutFeedback")?.GetComponent<MMF_Player>();
        }

        private void Awake()
        {
            cuttingZone = GetComponent<Collider>();
            if (!cuttingZone.isTrigger)
                Debug.LogError($"{nameof(KnifeTool)} on '{name}' requires its Collider to have Is Trigger enabled.", this);

            ItemInstance itemInstance = GetComponentInParent<ItemInstance>();
            definition = itemInstance != null ? itemInstance.Definition as KnifeDefinition : null;
            if (definition == null)
                Debug.LogError($"{nameof(KnifeTool)} on '{name}' needs a parent {nameof(ItemInstance)} assigned a {nameof(KnifeDefinition)} asset.", this);

            Transform knifeRoot = itemInstance != null ? itemInstance.transform : transform.root;
            vfxLocation = knifeRoot.Find("VfxLocation");
            if (vfxLocation == null)
                vfxLocation = knifeRoot;
        }

        private void OnTriggerEnter(Collider other) => TryCutIngredient(other);
        private void OnTriggerStay(Collider other) => TryCutIngredient(other);

        private void TryCutIngredient(Collider other)
        {
            if (definition == null || other == null)
                return;

            IngredientInstance ingredient = other.GetComponentInParent<IngredientInstance>();
            if (ingredient == null || !ingredient.CompareTag(itemTag))
                return;

            if (!definition.CanCut(ingredient.GetName()))
                return;

            if (ingredient.TryGetCut())
                SpawnKnifeEffect();
        }

        private void SpawnKnifeEffect()
        {
            if (definition.KnifeEffect == null || vfxLocation == null)
                return;

            GameObject effect = Instantiate(definition.KnifeEffect, vfxLocation.position, vfxLocation.rotation);
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            float lifetime = 0f;

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                lifetime = Mathf.Max(lifetime, main.duration + main.startDelay.constantMax + main.startLifetime.constantMax);
            }

            // Particle-only slash effects should clean themselves up after their longest particle lifetime.
            // Use a short fallback for non-particle prefabs so spawned effects do not accumulate.
            Destroy(effect, lifetime > 0f ? lifetime : 5f);

            if (cutFeedback != null)
            {
                cutFeedback.PlayFeedbacks();
            }
        }
    }
}
