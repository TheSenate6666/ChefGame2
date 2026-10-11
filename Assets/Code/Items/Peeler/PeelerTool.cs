using UnityEngine;
using MoreMountains.Feedbacks;


namespace ChefGame.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class PeelerTool : MonoBehaviour
    {
        [SerializeField] private string itemTag = "Item";
        private PeelerDefinition definition;
        private Transform vfxLocation;

        private MMF_Player peelFeedback;


        private void Start()
        {
            peelFeedback = GameObject.Find("PeelFeedback")?.GetComponent<MMF_Player>();
        }
        private void Awake()
        {
            if (!GetComponent<Collider>().isTrigger)
                Debug.LogError($"{nameof(PeelerTool)} on '{name}' requires its Collider to have Is Trigger enabled.", this);
            ItemInstance itemInstance = GetComponentInParent<ItemInstance>();
            definition = itemInstance != null ? itemInstance.Definition as PeelerDefinition : null;
            if (definition == null)
                Debug.LogError($"{nameof(PeelerTool)} on '{name}' needs a parent {nameof(ItemInstance)} assigned a {nameof(PeelerDefinition)} asset.", this);

            Transform peelerRoot = itemInstance != null ? itemInstance.transform : transform.root;
            vfxLocation = peelerRoot.Find("VfxLocation");
            if (vfxLocation == null)
                vfxLocation = peelerRoot;
        }

        private void OnTriggerEnter(Collider other) => TryPeelIngredient(other);
        private void OnTriggerStay(Collider other) => TryPeelIngredient(other);

        private void TryPeelIngredient(Collider other)
        {
            if (definition == null || other == null)
                return;
            IngredientInstance ingredient = other.GetComponentInParent<IngredientInstance>();
            if (!definition.PeelsIngredients || ingredient == null || !ingredient.CompareTag(itemTag))
                return;

            // The ingredient owns the peelability check. This makes the trigger work
            // for any ingredient whose definition enables CanBePeeled, including carrots.
            if (ingredient.TryGetPeeled())
                SpawnPeelerEffect();
        }

        private void SpawnPeelerEffect()
        {
            if (definition.PeelerEffect == null || vfxLocation == null)
                return;

            GameObject effect = Instantiate(definition.PeelerEffect, vfxLocation.position, vfxLocation.rotation);
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            float lifetime = 0f;

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                lifetime = Mathf.Max(lifetime, main.duration + main.startDelay.constantMax + main.startLifetime.constantMax);
            }

            // Particle effects should clean themselves up after their longest particle lifetime.
            // Use a short fallback for non-particle prefabs so spawned effects do not accumulate.
            Destroy(effect, lifetime > 0f ? lifetime : 5f);

            if (peelFeedback != null)
            {
                peelFeedback.PlayFeedbacks();
            }
        }
    }
}
