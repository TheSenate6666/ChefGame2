using System;
using UnityEngine;
using ChefGame.Input;
using MoreMountains.Feedbacks;

namespace ChefGame.Equipments
{
    public class Stove : MonoBehaviour
    {
        private bool isStoveOn = false;
        private bool isHandsOnMe = false;
        public static event Action<bool> OnStoveToggled;

        [Header("Feedback (found in scene at runtime)")]
        [SerializeField] private string buttonPressFeedbackName = "StoveButtonFeedback";
        private MMF_Player buttonPressFeedback;

        private void OnEnable()
        {
            LocalCoopPlayerController.OnPressPerformed += ToggleStove;
        }

        private void OnDisable()
        {
            LocalCoopPlayerController.OnPressPerformed -= ToggleStove;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Hand"))
                isHandsOnMe = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Hand"))
                isHandsOnMe = false;
        }

        private void ToggleStove()
        {
            if (!isHandsOnMe) return;

            PlayButtonFeedback();

            isStoveOn = !isStoveOn;
            OnStoveToggled?.Invoke(isStoveOn);
            Debug.Log($"Stove Status: {isStoveOn}");
        }

        private void PlayButtonFeedback()
        {
            if (buttonPressFeedback == null)
            {
                GameObject go = GameObject.Find(buttonPressFeedbackName);
                if (go != null) buttonPressFeedback = go.GetComponent<MMF_Player>();
            }
            if (buttonPressFeedback != null) buttonPressFeedback.PlayFeedbacks();
        }
    }
}