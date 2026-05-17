using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using PSW.Code.Talk;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class TalkRewardAction : MonoBehaviour, IInteractAction
    {
        private InteractContext _ctx;

        public void Setup(InteractContext ctx)
        {
            _ctx = ctx;
            if (_ctx.controller != null)
                _ctx.controller.gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (_ctx.controller != null)
            {
                _ctx.controller.OnChangeAction -= HandleChangeAction;
                _ctx.controller.OnTalkClosed -= HandleTalkClosed;
            }
        }

        public bool CanExecute()
        {
            return true;
        }

        public void Execute()
        {
            if (_ctx.actionMode == InteractActionMode.RewardOnly)
            {
                GiveRewardOnly();
                return;
            }

            StartTalk();
        }
        
        private void StartTalk()
        {
            if (_ctx.controller == null)
            {
                FinishAndAfter(); 
                return;
            }

            _ctx.controller.gameObject.SetActive(true);
            _ctx.controller.StartTalk(_ctx.talkData);

            _ctx.controller.OnChangeAction += HandleChangeAction;
            _ctx.controller.OnTalkClosed -= HandleTalkClosed;
            _ctx.controller.OnTalkClosed += HandleTalkClosed;
        }

        private void GiveRewardOnly()
        {
            RaiseTalkFinished();
            AfterFinished();
            _ctx.owner.SetStartTalkFlag(false);
        }

        private void HandleTalkClosed()
        {
            _ctx.controller.OnTalkClosed -= HandleTalkClosed;
            _ctx.controller.OnChangeAction -= HandleChangeAction;

            RaiseTalkFinished();
            AfterFinished();
            _ctx.owner.SetStartTalkFlag(false);
        }

        private void HandleChangeAction(string newValue)
        {
            _ctx.rewardKey = newValue;
        }

        private void RaiseTalkFinished()
        {
            Bus<TalkFinished>.Raise(new TalkFinished(
                _ctx.talkId,
                _ctx.targetEnemyId,
                _ctx.rewardKey,
                _ctx.position
            ));
        }

        private void AfterFinished()
        {
            _ctx.owner.IsFinished = true;
            
            if (!string.IsNullOrEmpty(_ctx.talkId))
            {
                SceneSaveSystem.SetTalkFinished(
                    SceneManager.GetActiveScene().name, 
                    _ctx.talkId, 
                    true
                );
            }

            if (_ctx.disableTalkAfterFinished)
                _ctx.owner.DisableTalk();

            _ctx.owner.DisableSelfNowOrDelayed(_ctx.disableObjectAfterFinished, _ctx.disableDelay);
        }

        private void FinishAndAfter()
        {
            RaiseTalkFinished();
            AfterFinished();
            _ctx.owner.SetStartTalkFlag(false);
        }
        
    }
}