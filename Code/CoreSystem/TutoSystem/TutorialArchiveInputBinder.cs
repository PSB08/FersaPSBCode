using Code.Scripts.Entities;
using PSB.Code.BattleCode.Players;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutorialArchiveInputBinder : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO input;
        [SerializeField] private TutorialArchivePanelUI tutoArchivePanel;

        /*private void OnEnable()
        {
            input.OnArchivePressed += OnArchivePressed;
        }

        private void OnDisable()
        {
            input.OnArchivePressed -= OnArchivePressed;
        }*/
        
        #if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                Debug.Log("<color=purple>[Upgrade]초기화</color>");
                
                tutoArchivePanel.RefreshAll();
            }
        }
        #endif

        /*private void OnArchivePressed()
        {
            if (tutoArchivePanel == null) return;
            if (!tutoArchivePanel.CanToggle()) return;

            tutoArchivePanel.Toggle();
        }*/
        
    }
}