using System;
using UnityEngine;
using ChefGame.Input;

namespace ChefGame.Equipments
{
    public class Stove : MonoBehaviour
    {
        private bool isStoveOn = false;
        private bool isHandsOnMe = false;
        public static event Action<bool> OnStoveToggled;


        private void OnEnable()
        {
            LocalCoopPlayerController.OnPressPerformed += ToggleStove;
        }

        private void OnDisable()
        {
            LocalCoopPlayerController.OnPressPerformed -= ToggleStove;
        }


        private void OnCollisionEnter(Collision other)
        {
            if (!other.gameObject.CompareTag("Player")) return;
            
            Debug.Log("Hands on stove!");
            isHandsOnMe = true;
        }

        private void OnCollisionExit(Collision other)
        {
            if (!other.gameObject.CompareTag("Player")) return;

            isHandsOnMe = false;
        }

        private void ToggleStove()
        {
            if (!isHandsOnMe) return;
            isStoveOn =  !isStoveOn;
            OnStoveToggled?.Invoke(isStoveOn);
            Debug.Log("Stove Status: " + isStoveOn);
        }
    }
}