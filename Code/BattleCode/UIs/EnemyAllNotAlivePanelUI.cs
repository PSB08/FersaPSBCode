using System.Collections;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.CoreSystem;
using Work.Scripts.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyAllNotAlivePanelUI : MonoBehaviour
    {
        [SerializeField] private TransitionController controller;

        [Header("Size Animation")]
        [SerializeField] private EndPanelSizeToggle_Model sizeModel;
        [SerializeField] private EndPanelSizeToggle_View sizeView;

        [SerializeField] private StageUIDataSO stageUIDataSO;

        private bool _isShown;

        private void Awake()
        {
            sizeView.Init(sizeModel.InitModel());
            _isShown = false;
        }
        
        private void Start()
        {
            Bus<EnemyAllNotAlive>.OnEvent += OnAllEnemiesDead;
        }

        private void OnDisable()
        {
            Bus<EnemyAllNotAlive>.OnEvent -= OnAllEnemiesDead;
        }

        private void OnAllEnemiesDead(EnemyAllNotAlive evt)
        {
            StartCoroutine(ShowCoroutine());
        }

        private void Show()
        {
            if (_isShown) return;
            _isShown = true;
            Time.timeScale = 0;

            sizeView.AnimateTo(
                sizeModel.GetShowSize(),
                sizeModel.GetPopTime()
            );
        }

        private IEnumerator ShowCoroutine()
        {
            yield return null;
            Show();
        }

        public void Hide()
        {
            if (!_isShown) return;
            _isShown = false;
            Time.timeScale = 1;

            sizeView.AnimateTo(
                sizeModel.GetHideSize(),
                sizeModel.GetPopTime()
            );
        }

        public void ExitBtn()
        {
            Hide();
            stageUIDataSO.DeleteJson();
            Invoke(nameof(DoTransition), sizeModel.GetPopTime());
        }

        private void DoTransition()
        {
            PlayerPrefs.SetInt("Stage", 1);
            PlayerPrefs.Save();
            controller.nextScene = "PSB_Field";
            controller.Transition();
        }
        
    }
}