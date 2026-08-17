using System.Collections;
using System;
using UnityEngine;
using Work.PSB.Code.CoreSystem;

namespace PSB.Code.BattleCode.UIs
{
    public class FailLootPanelUI : MonoBehaviour
    {
        [SerializeField] private TransitionController controller;

        [Header("Size Animation")]
        [SerializeField] private EndPanelSizeToggle_Model sizeModel;
        [SerializeField] private EndPanelSizeToggle_View sizeView;

        private bool _isShown;
        private Action _exitOverride;
        private Action _pendingExitOverride;

        private void Awake()
        {
            sizeView.Init(sizeModel.InitModel());
            _isShown = false;
        }

        private void Show()
        {
            if (_isShown) return;
            _isShown = true;
            //Time.timeScale = 0;

            sizeView.AnimateTo(
                sizeModel.GetShowSize(),
                sizeModel.GetPopTime()
            );
        }
        
        public IEnumerator ShowCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            Show();
        }

        public void Hide()
        {
            if (!_isShown) return;
            _isShown = false;
            Time.timeScale = 1;

            sizeView.AnimateTo(
                sizeModel.GetHideSize(),
                0.25f
            );
        }

        public void ExitBtn()
        {
            if (!_isShown)
                return;
    
            Action exitOverride = _exitOverride;
            _exitOverride = null;
    
            Hide();
    
            if (exitOverride == null)
            {
                Invoke(nameof(DoTransition), sizeModel.GetPopTime());
                return;
            }
    
            _pendingExitOverride = exitOverride;
            Invoke(nameof(DoExitOverride), sizeModel.GetPopTime());
        }

        private void DoExitOverride()
        {
            Action exitOverride = _pendingExitOverride;
            _pendingExitOverride = null;
            exitOverride?.Invoke();
        }

        private void DoTransition()
        {
            controller.nextScene = "SW_Title";
            controller.Transition();
        }

        public void SetExitOverride(Action exitOverride)
        {
            _exitOverride = exitOverride;
        }
        
    }
}