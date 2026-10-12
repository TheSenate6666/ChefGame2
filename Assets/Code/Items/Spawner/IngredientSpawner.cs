using System.Collections.Generic;
using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>
    /// Spawns and immediately holds one configured item when a player presses Interact
    /// while their hand collider truly overlaps the spawn area. Range is checked at
    /// press time (no stored enter/exit state), so it can never go stale.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IngredientSpawner : MonoBehaviour
    {
        private static readonly List<IngredientSpawner> ActiveSpawners = new();

        [SerializeField] private Collider spawnArea;
        [SerializeField] private GameObject prefabToSpawn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ActiveSpawners.Clear();
        }

        /// <summary>Returns the spawner the given hand colliders overlap right now, or null.</summary>
        public static IngredientSpawner FindOverlapping(IReadOnlyList<Collider> handColliders)
        {
            if (handColliders == null)
                return null;

            for (int i = 0; i < ActiveSpawners.Count; i++)
            {
                IngredientSpawner spawner = ActiveSpawners[i];
                if (spawner != null && spawner.Overlaps(handColliders))
                    return spawner;
            }

            return null;
        }

        private void Awake()
        {
            if (spawnArea == null)
                spawnArea = GetComponent<Collider>();

            if (spawnArea == null || !spawnArea.isTrigger)
                Debug.LogError($"{nameof(IngredientSpawner)} on '{name}' requires a trigger collider assigned as its spawn area.", this);
        }

        private void OnEnable()
        {
            if (!ActiveSpawners.Contains(this))
                ActiveSpawners.Add(this);
        }

        private void OnDisable()
        {
            ActiveSpawners.Remove(this);
        }

        private void OnValidate()
        {
            if (spawnArea == null)
                spawnArea = GetComponent<Collider>();
        }

        private bool Overlaps(IReadOnlyList<Collider> handColliders)
        {
            if (spawnArea == null || !spawnArea.enabled || !spawnArea.gameObject.activeInHierarchy)
                return false;

            Transform areaTransform = spawnArea.transform;

            for (int i = 0; i < handColliders.Count; i++)
            {
                Collider hand = handColliders[i];
                if (hand == null || !hand.enabled || !hand.gameObject.activeInHierarchy)
                    continue;

                Transform handTransform = hand.transform;

                bool overlapping;
                if (SupportsPenetration(spawnArea) && SupportsPenetration(hand))
                {
                    // Real shape-vs-shape test (not AABB).
                    overlapping = Physics.ComputePenetration(
                        spawnArea, areaTransform.position, areaTransform.rotation,
                        hand, handTransform.position, handTransform.rotation,
                        out _, out _);
                }
                else
                {
                    // Fallback for collider types ComputePenetration can't handle.
                    overlapping = spawnArea.bounds.Intersects(hand.bounds);
                }

                if (overlapping)
                    return true;
            }

            return false;
        }

        private static bool SupportsPenetration(Collider collider)
        {
            return collider is BoxCollider
                   || collider is SphereCollider
                   || collider is CapsuleCollider
                   || (collider is MeshCollider mesh && mesh.convex);
        }

        /// <summary>Spawns the configured prefab straight into the interactor's hand.</summary>
        public bool TrySpawnFor(LocalCoopItemInteractor interactor)
        {
            if (interactor == null || interactor.IsHoldingItem)
                return false;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"'{name}' has no prefab assigned to {nameof(IngredientSpawner)}.", this);
                return false;
            }

            Transform holder = interactor.ItemHolder;
            if (holder == null)
            {
                Debug.LogWarning($"The nearby player '{interactor.name}' has no item holder assigned.", interactor);
                return false;
            }

            GameObject spawned = Instantiate(prefabToSpawn, holder.position, holder.rotation);
            if (!interactor.TryHoldItem(spawned.transform))
            {
                Destroy(spawned);
                return false;
            }

            return true;
        }
    }
}
