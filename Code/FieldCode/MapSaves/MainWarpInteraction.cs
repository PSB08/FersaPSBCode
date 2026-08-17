using UnityEngine;
using Work.CSH.Scripts.Interacts;
using Work.PSB.Code.CoreSystem;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public class MainWarpInteraction : MonoBehaviour, IInteractable
    {
        [field: SerializeField] public Transform InteractUITrm { get; private set; }
        [field: SerializeField] public bool CanUIShow { get; private set; } = true;
        [field:SerializeField] public string Name { get; set; }
        public Transform Transform => transform;

        [SerializeField] private TransitionController controller;
        [SerializeField] private string sceneName;
        
        [Header("MapData")]
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float sensingRange = 1.5f;
        [SerializeField] private Color rangeColor = Color.green;
        
        private bool _isTalkEnabled;

        private void Awake()
        {
            _isTalkEnabled = true;
        }
        
        public void OnInteract()
        {
            if (!_isTalkEnabled) return;
            
            controller.nextScene = sceneName;
            controller.Transition(sceneName);

            SceneSaveSystem.DeleteAllSaves();
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = rangeColor;
            Gizmos.DrawWireSphere(transform.position, sensingRange);
        }
        
    }
}