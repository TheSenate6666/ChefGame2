using System;
using UnityEngine;
using ChefGame.Input;

namespace ChefGame.Equipments
{
    public class Stove : MonoBehaviour
    {
        private bool isStoveOn = false;
        public static event Action<bool> OnStoveToggled;
        

        public void OnCollisionEnter(Collision other)
        {
            if (!other.gameObject.CompareTag("Player")) return;
            
            LocalCoopPlayerController controller = other.gameObject.GetComponent<LocalCoopPlayerController>();
            
            Debug.Log("Hands on stove!");
            if (controller != null && controller.InteractAction != null)
            {
                Debug.Log("Got the controller");
                if (controller.InteractAction.IsPressed())
                {
                    ToggleStove();
                }
            }
        }

        private void ToggleStove()
        {
            isStoveOn =  !isStoveOn;
            OnStoveToggled?.Invoke(isStoveOn);
            Debug.Log("Stove Status: " + isStoveOn);
        }
    }
}