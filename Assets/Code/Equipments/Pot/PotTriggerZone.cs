using UnityEngine;

namespace ChefGame.Equipments
{
    public class PotTriggerZone : MonoBehaviour
    {
        [SerializeField] private Pot parentPot;

        private void Awake()
        {
            if (parentPot == null)
            {
                parentPot = GetComponentInParent<Pot>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (parentPot != null)
            {
                parentPot.HandleIngredientTrigger(other);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (parentPot != null)
            {
                parentPot.HandleIngredientTrigger(other);
            }
        }
    }
}