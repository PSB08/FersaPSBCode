using System;
using System.Collections.Generic;
using PSB.Code.CoreSystem.Events;
using PSW.Code.BaseSystem;
using PSW.Code.Talk;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.CoreSystem;

namespace Work.PSB.Code.RunSystem
{
    public class RunEventController : MonoBehaviour
    {
        [SerializeField] private Transform entitySpawnPoint;
        [SerializeField] private Transform systemSpawnPoint;
        [SerializeField] private Talk_Controller fallbackTalkController;
        
        private GameObject _entity;
        private GameObject _system;
        private Action _onComplete;
        
        private bool _completed;
        private RunEventSO _data;
        private TalkRewardGiver _rewardGiver;
        private readonly HashSet<string> _requestedRewardKeys = new HashSet<string>();
        
        private TalkStage _currentStage;
        private string _currentRewardKey;
        private bool _suppressStageReward;
        private RunEventBattleAction _runEventBattleAction = RunEventBattleAction.Default;
        
        public bool ShouldSkipBattle => _runEventBattleAction == RunEventBattleAction.SkipBattle;
        
        public void Begin(RunEventSO data, Action onComplete)
        {
            TalkStage stage = data != null ? data.GetTalkStage() : null;
            Begin(data, stage, onComplete);
        }
        
        public void Begin(RunEventSO data, TalkStage stage, Action onComplete)
        {
            Cleanup();
            _data = data;
            _onComplete = onComplete;
            _completed = false;
            _requestedRewardKeys.Clear();
            _currentStage = stage;
            _currentRewardKey = data != null ? data.GetStageRewardKey(_currentStage) : string.Empty;
            _suppressStageReward = false;
            _runEventBattleAction = RunEventBattleAction.Default;
            
            if (data == null)
            {
                Complete();
                return;
            }
            
            if (data.systemPrefab != null)
            {
                Transform parent = systemSpawnPoint != null ? systemSpawnPoint : transform;
                _system = Instantiate(data.systemPrefab, parent.position, Quaternion.identity, parent);
            }
            
            if ((_currentStage == null || _currentStage.talkData == null) && _system == null)
            {
                SetupRewardGiver(data);
                CompleteCurrentStage();
                return;
            }
            
            if (data.entityPrefab == null)
            {
                SetupRewardGiver(data);
                
                if (_system == null) 
                    CompleteCurrentStage();
                
                return;
            }
            
            Vector3 pos = entitySpawnPoint != null ? entitySpawnPoint.position : transform.position;
            _entity = Instantiate(data.entityPrefab, pos, Quaternion.identity);
            
            ApplyVisualOverrides(data);
            
            EnsureEntityActive();
            SetupRewardGiver(data);
            BindRunTalk(data);
        }
        
        public void DisableEntityAfterFinished()
        {
            if (_data == null || !_data.disableEntityAfterFinished || _entity == null)
                return;
            
            _entity.SetActive(false);
        }
        
        public void NotifyEventFinished()
        {
            CompleteCurrentStage();
        }
        
        private void BindRunTalk(RunEventSO data)
        {
            if (data == null || _currentStage == null)
            {
                Complete();
                return;
            }
            
            Talk_Controller talkController = _currentStage.talkData != null ? ResolveTalkController() : null;
            if (_currentStage.talkData != null && talkController == null)
            {
                Debug.LogWarning("Run event has TalkStage.talkData, but no Talk_Controller was found.", _entity);
            }
            
            RunTalkEntity.BindTo(_entity, _currentStage.talkData, talkController, CompleteCurrentStage,
                RequestRewardFromTalk, ChangeRewardKey, SuppressStageReward,
                ChangeRunEventBattleAction);
        }
        
        private void SetupRewardGiver(RunEventSO data)
        {
            if (data == null)
                return;
            
            GameObject host = _entity != null ? _entity : _system != null ? _system : gameObject;
            TalkRewardGiver.RewardEntry[] runtimeRewardEntries = data.CreateRuntimeRewardEntries(_currentStage);
            bool hasRuntimeRewards = runtimeRewardEntries != null && runtimeRewardEntries.Length > 0;
            
            _rewardGiver = FindAnyObjectByType<TalkRewardGiver>();
            
            if (_rewardGiver != null && hasRuntimeRewards)
                _rewardGiver.ConfigureRuntime(runtimeRewardEntries, host.transform);
        }
        
        private void CompleteCurrentStage()
        {
            if (_currentStage == null)
            {
                Complete();
                return;
            }
            
            switch (_currentStage.actionMode)
            {
                case InteractActionMode.TalkOnly:
                    Complete();
                    return;
                case InteractActionMode.TalkAndOpenUI:
                    OpenStageUI(_currentStage);
                    Complete();
                    return;
                case InteractActionMode.TalkAndReward:
                case InteractActionMode.RewardOnly:
                case InteractActionMode.TalkAndBarter:
                    if (!_suppressStageReward)
                        RequestReward(_currentRewardKey);
                    RaiseTalkFinished(_currentRewardKey);
                    Complete();
                    return;
                default:
                    Complete();
                    return;
            }
        }
        
        private void RequestRewardFromTalk(string rewardKey)
        {
            if (_data == null || !_data.CanStageRequestReward(_currentStage))
                return;
            
            RequestReward(rewardKey);
        }
        
        private void OpenStageUI(TalkStage stage)
        {
            Bus<UIPressrdEvent>.Raise(new UIPressrdEvent(stage.uiType));
        }
        
        private void RaiseTalkFinished(string rewardKey)
        {
            if (_data == null)
                return;
            
            string targetEnemyId = _currentStage != null ? _currentStage.targetEnemyId : string.Empty;
            Bus<TalkFinished>.Raise(new TalkFinished(GetTalkId(), targetEnemyId,
                rewardKey, GetRewardWorldPosition()));
        }
        
        private void RequestReward(string rewardKey)
        {
            string finalRewardKey = string.IsNullOrEmpty(rewardKey) ? _currentRewardKey : rewardKey;
            if (string.IsNullOrEmpty(finalRewardKey))
                return;
            
            if (!_requestedRewardKeys.Add(finalRewardKey))
                return;
            
            if (_rewardGiver == null)
            {
                Debug.LogWarning($"Run event reward has no TalkRewardGiver. RewardKey: {finalRewardKey}", this);
                return;
            }
            
            _rewardGiver.GiveReward(GetTalkId(), string.Empty, finalRewardKey, GetRewardWorldPosition());
        }
        
        private void ChangeRewardKey(string rewardKey)
        {
            if (!string.IsNullOrEmpty(rewardKey))
                _currentRewardKey = rewardKey;
        }
        
        private void ChangeRunEventBattleAction(RunEventBattleAction action)
        {
            _runEventBattleAction = action;
        }
        
        private void SuppressStageReward(bool suppress)
        {
            if (suppress)
                _suppressStageReward = true;
        }
        
        private string GetTalkId()
        {
            return _data != null ? _data.TalkId : string.Empty;
        }
        
        private Vector3 GetRewardWorldPosition()
        {
            if (_entity != null)
                return _entity.transform.position;
            
            if (_system != null)
                return _system.transform.position;
            
            return transform.position;
        }
        
        private Talk_Controller ResolveTalkController()
        {
            if (_entity != null)
            {
                Talk_Controller controllerInEntity = _entity.GetComponentInChildren<Talk_Controller>(true);
                if (controllerInEntity != null)
                    return controllerInEntity;
            }
            
            if (fallbackTalkController != null)
                return fallbackTalkController;
            
            return FindAnyObjectByType<Talk_Controller>();
        }
        
        private void EnsureEntityActive()
        {
            if (_entity == null)
                return;
            
            if (!_entity.activeSelf)
                _entity.SetActive(true);
            
            RunTalkEntity runTalkEntity = _entity.GetComponentInChildren<RunTalkEntity>(true);
            if (runTalkEntity != null && !runTalkEntity.gameObject.activeSelf)
                runTalkEntity.gameObject.SetActive(true);
        }
        
        private void ApplyVisualOverrides(RunEventSO data)
        {
            if (_entity == null || data == null)
                return;
            
            Transform visual = _entity.transform.Find("Visual");
            
            SpriteRenderer spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);
            
            if (data.sprite != null && spriteRenderer != null)
                spriteRenderer.sprite = data.sprite;
            
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            
            if (data.animatorController == null || animator == null)
                return;
            
            animator.runtimeAnimatorController = data.animatorController;
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }
        
        private void Complete()
        {
            if (_completed)
                return;
            
            _completed = true;
            _onComplete?.Invoke();
            _onComplete = null;
        }
        
        private void Cleanup()
        {
            if (_entity != null)
                Destroy(_entity);
            
            if (_system != null)
                Destroy(_system);
            
            _entity = null;
            _system = null;
            _data = null;
            _rewardGiver = null;
            _currentStage = null;
            
            _requestedRewardKeys.Clear();
            _currentRewardKey = string.Empty;
            _suppressStageReward = false;
            _runEventBattleAction = RunEventBattleAction.Default;
            _completed = false;
        }
        
    }
    
}
