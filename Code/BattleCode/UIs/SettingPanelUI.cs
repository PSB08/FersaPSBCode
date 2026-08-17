using CIW.Code.System.Events;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.CoreSystem;
using Work.PSB.Code.CoreSystem.Sounds;
using Work.PSB.Code.FieldCode.MapSaves;

namespace PSB.Code.BattleCode.UIs
{
    public class SettingPanelUI : MonoBehaviour
    {
        [SerializeField] private TransitionController controller;
        [SerializeField] private EndPanelSizeToggle_Model sizeModel;
        [SerializeField] private EndPanelSizeToggle_View sizeView;

        [SerializeField] private SoundSO uiClickSound;
        
        private bool _isShown;
        private bool _isExiting;
        private bool _gameOver;

        private string _cachedExitScene;

        private void Awake()
        {
            sizeView.Init(sizeModel.InitModel());
            _isShown = false;
            sizeView.AnimateTo(sizeModel.GetHideSize(), 0f);
            Bus<BattleEnd>.OnEvent += BattleOver;
        }

        private void OnDestroy()
        {
            Bus<BattleEnd>.OnEvent -= BattleOver;
        }

        public void BattleOver(BattleEnd evt)
        {
            if(evt.IsVictory == false)
            {
                _gameOver = true;
                Hide();
            }
        }

        private void Update()
        {
            if (_isExiting) return;

            if (Input.GetKeyDown(KeyCode.Escape) && _gameOver == false)
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            if (_isShown) Hide();
            else Show();
        }

        private void Show()
        {
            if (_isShown || _isExiting) return;

            _isShown = true;
            Time.timeScale = 0f;

            sizeView.AnimateTo(
                sizeModel.GetShowSize(),
                sizeModel.GetPopTime()
            );
        }

        private void Hide()
        {
            if (!_isShown) return;

            _isShown = false;
            Time.timeScale = 1f;

            sizeView.AnimateTo(
                sizeModel.GetHideSize(),
                sizeModel.GetPopTime()
            );
        }

        public void ExitBtn()
        {
            if (_isExiting) return;
            
            PlaySfx(uiClickSound);
            
            _isExiting = true;

            _cachedExitScene = BattleContext.FieldSceneName;

            Hide();

            Bus<RollbackBattleLootEvent>.Raise(new RollbackBattleLootEvent());

            if (BattleContext.HasContext)
            {
                foreach (string enemyId in BattleContext.InvolvedEnemyIDs)
                {
                    var enemy = SceneObjectRegistry.FindEnemy(enemyId);
                    enemy?.RestoreActiveIfAlive();
                }
            }

            BattleContext.Clear();

            Invoke(nameof(DoTransition), sizeModel.GetPopTime());
        }

        private void DoTransition()
        {
            if (string.IsNullOrEmpty(_cachedExitScene))
            {
                Debug.LogWarning("Exit scene name is empty.");
                _isExiting = false;
                return;
            }

            controller.Transition(_cachedExitScene);
        }
        
        private void PlaySfx(SoundSO soundSO)
        {
            if (soundSO == null || soundSO.clip == null)
                return;

            Bus<PlaySFXEvent>.Raise(SoundEvents.PlaySFXEvent.Initialize(transform.position, soundSO));
        }
        
    }
}