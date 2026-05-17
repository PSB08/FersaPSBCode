using System;
using UnityEngine;
using Work.CSH.Scripts.Interacts;
using Work.PSB.Code.CoreSystem.Sounds;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.FieldCode
{
    public class WarpInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject ui;
        [field:SerializeField] public string Name { get; set; }
        public Transform Transform => transform;
        
        [Header("MapData")]
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float sensingRange = 1.5f;
        [SerializeField] private Color rangeColor = Color.green;

        private bool _isTalkEnabled;

        private void Awake()
        {
            _isTalkEnabled = true;
        }

        private void Start()
        {
            ui.gameObject.SetActive(false);
        }

        public void OnInteract()
        {
            if (!_isTalkEnabled) return;
            
            if (ui.gameObject.activeSelf) return;
            if (ui != null)
                ui.gameObject.SetActive(true);
            Debug.Log("WarpInteraction: OnInteract called");

            SceneSaveSystem.DeleteAllSaves();
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = rangeColor;
            Gizmos.DrawWireSphere(transform.position, sensingRange);
        }
        
    }
}