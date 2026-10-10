using System;
using UnityEngine;
using System.Collections.Generic;
using ChefGame.Equipments;

namespace ChefGame.Equipments
{
    public class Stove : MonoBehaviour
    {
        private bool isStoveOn = false;
        public static event Action<bool> OnStoveToggled;

        public void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Hand"))
            {
                //check interact button
                isStoveOn = !isStoveOn;
                OnStoveToggled.Invoke(isStoveOn);
            }
        }
    }
}