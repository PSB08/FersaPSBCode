using UnityEngine;
using Work.CSH.Scripts.Interacts;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutoEnterInteraction : MonoBehaviour, IInteractable
    {
        [field: SerializeField] public Transform InteractUITrm { get; private set; }
        [field: SerializeField] public bool CanUIShow { get; private set; } = true;
        [SerializeField] private TransitionController transitionController;
        [field:SerializeField] public string Name { get; set; }

        public Transform Transform => transform;
        
        public void OnInteract()
        {
            transitionController.nextScene = "Tuto_Forest";
            transitionController.Transition();
        }
        
    }
}