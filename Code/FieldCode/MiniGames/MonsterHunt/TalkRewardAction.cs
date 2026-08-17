using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using PSW.Code.Talk;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.PSB.Code.FieldCode.MapSaves;
using PSW.Code.BaseSystem;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class TalkRewardAction : MonoBehaviour, IInteractAction
    {
        private InteractContext _ctx;
        private readonly HashSet<string> _requestedRewardKeys = new HashSet<string>();
        private bool _suppressDefaultReward;

        public void Setup(InteractContext ctx)
        {
            _ctx = ctx;
            _requestedRewardKeys.Clear();
            _suppressDefaultReward = false;

            if (_ctx.controller != null)
                _ctx.controller.gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (_ctx.controller == null)
                return;

            _ctx.controller.OnChangeAction -= HandleChangeAction;
            _ctx.controller.OnRewardRequested -= HandleRewardRequested;
            _ctx.controller.OnSuppressDefaultRewardRequested -= HandleSuppressDefaultRewardRequested;
            _ctx.controller.OnTalkClosed -= HandleTalkClosed;
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

            _ctx.controller.OnChangeAction -= HandleChangeAction;
            _ctx.controller.OnChangeAction += HandleChangeAction;
            _ctx.controller.OnRewardRequested -= HandleRewardRequested;
            _ctx.controller.OnRewardRequested += HandleRewardRequested;
            _ctx.controller.OnSuppressDefaultRewardRequested -= HandleSuppressDefaultRewardRequested;
            _ctx.controller.OnSuppressDefaultRewardRequested += HandleSuppressDefaultRewardRequested;
            _ctx.controller.OnTalkClosed -= HandleTalkClosed;
            _ctx.controller.OnTalkClosed += HandleTalkClosed;

            _ctx.controller.gameObject.SetActive(true);
            _ctx.controller.StartTalk(_ctx.talkData);
        }

        private void GiveRewardOnly()
        {
            RequestRewardOnTalkEnd();
            RaiseTalkFinished();
            AfterFinished();
            _ctx.owner.CompleteCurrentStage();
        }

        private void HandleTalkClosed(TalkCloseReason reason)
        {
            _ctx.controller.OnTalkClosed -= HandleTalkClosed;
            _ctx.controller.OnChangeAction -= HandleChangeAction;
            _ctx.controller.OnRewardRequested -= HandleRewardRequested;
            _ctx.controller.OnSuppressDefaultRewardRequested -= HandleSuppressDefaultRewardRequested;

            if (reason == TalkCloseReason.Cancel)
            {
                _ctx.owner.SetStartTalkFlag(false);
                return;
            }

            if (_ctx.actionMode == InteractActionMode.TalkOnly)
            {
                CompleteTalkOnly();
                return;
            }

            if (_ctx.actionMode == InteractActionMode.TalkAndOpenUI)
            {
                OpenUIAfterTalk();
                return;
            }

            RequestRewardOnTalkEnd();
            RaiseTalkFinished();
            AfterFinished();
            _ctx.owner.CompleteCurrentStage();
        }

        private void HandleChangeAction(string newValue)
        {
            _ctx.rewardKey = newValue;
        }

        private void HandleRewardRequested(string rewardKey)
        {
            if (_ctx.actionMode == InteractActionMode.TalkOnly)
            {
                return;
            }

            if (_ctx.actionMode == InteractActionMode.TalkAndOpenUI)
            {
                return;
            }

            RequestReward(rewardKey);
        }

        private void HandleSuppressDefaultRewardRequested(bool suppress)
        {
            if (suppress)
            {
                _suppressDefaultReward = true;
            }
        }

        private void RequestReward(string rewardKey)
        {
            string finalRewardKey = string.IsNullOrEmpty(rewardKey) ? _ctx.rewardKey : rewardKey;

            if (string.IsNullOrEmpty(finalRewardKey))
            {
                return;
            }

            if (!_requestedRewardKeys.Add(finalRewardKey))
            {
                return;
            }

            Bus<TalkRewardRequested>.Raise(new TalkRewardRequested(
                _ctx.talkId,
                _ctx.targetEnemyId,
                finalRewardKey,
                _ctx.position
            ));
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
            if (_ctx.actionMode == InteractActionMode.TalkOnly)
            {
                CompleteTalkOnly();
                return;
            }

            if (_ctx.actionMode == InteractActionMode.TalkAndOpenUI)
            {
                OpenUIAfterTalk();
                return;
            }

            RequestRewardOnTalkEnd();
            RaiseTalkFinished();
            AfterFinished();
            _ctx.owner.CompleteCurrentStage();
        }

        private void RequestRewardOnTalkEnd()
        {
            if (!ShouldRewardOnTalkEnd() || _suppressDefaultReward)
            {
                return;
            }

            RequestReward(_ctx.rewardKey);
        }

        private bool ShouldRewardOnTalkEnd()
        {
            return _ctx.actionMode == InteractActionMode.TalkAndReward ||
                   _ctx.actionMode == InteractActionMode.RewardOnly ||
                   _ctx.actionMode == InteractActionMode.TalkAndBarter;
        }

        private void OpenUIAfterTalk()
        {
            Bus<UIPressrdEvent>.Raise(
                new UIPressrdEvent(_ctx.uiType));
            _ctx.owner.CompleteCurrentStage();
        }

        private void CompleteTalkOnly()
        {
            _ctx.owner.CompleteCurrentStage();
        }
        
    }
}
