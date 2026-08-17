using System;
using PSW.Code.Talk;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Work.PSB.Code.RunSystem
{
    public class RunTalkEntity : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Talk_Controller talkController;
        [SerializeField] private bool ignoreClicksThroughUI = true;
        
        private TalkDataListSO _talkData;
        private Action _onComplete;
        private Action<string> _onRewardRequested;
        private Action<string> _onChangeAction;
        private Action<bool> _onSuppressStageRewardRequested;
        private Action<RunEventBattleAction> _onRunEventBattleActionRequested;
        
        private bool _isTalking;
        private bool _completed;
        private int _lastClickFrame = -1;
        
        public static RunTalkEntity BindTo(GameObject root, TalkDataListSO talkData, Talk_Controller controller,
            Action onComplete, Action<string> onRewardRequested = null, Action<string> onChangeAction = null,
            Action<bool> onSuppressStageRewardRequested = null,
            Action<RunEventBattleAction> onRunEventBattleActionRequested = null)
        {
            if (root == null)
                return null;
            
            RunTalkEntity entity = root.GetComponentInChildren<RunTalkEntity>(true);
            if (entity == null)
                entity = root.AddComponent<RunTalkEntity>();
            
            entity.Bind(talkData, controller, onComplete, onRewardRequested, onChangeAction,
                onSuppressStageRewardRequested, onRunEventBattleActionRequested);
            entity.InstallClickProxies(root);
            
            return entity;
        }
        
        public void Bind(TalkDataListSO talkData, Talk_Controller controller, Action onComplete,
            Action<string> onRewardRequested = null, Action<string> onChangeAction = null,
            Action<bool> onSuppressStageRewardRequested = null,
            Action<RunEventBattleAction> onRunEventBattleActionRequested = null)
        {
            UnsubscribeTalkController();
            
            _talkData = talkData;
            talkController = controller;
            _onComplete = onComplete;
            _onRewardRequested = onRewardRequested;
            _onChangeAction = onChangeAction;
            _onSuppressStageRewardRequested = onSuppressStageRewardRequested;
            _onRunEventBattleActionRequested = onRunEventBattleActionRequested;
            _isTalking = false;
            _completed = false;
            _lastClickFrame = -1;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            TryStartTalk(false);
        }
        
        private void OnMouseDown()
        {
            TryStartTalk(true);
        }
        
        private void TryStartTalk(bool checkUiBlock)
        {
            if (_lastClickFrame == Time.frameCount)
                return;
            
            if (_completed || _isTalking)
                return;
            
            if (checkUiBlock && ignoreClicksThroughUI && EventSystem.current != null 
                && EventSystem.current.IsPointerOverGameObject())
                return;
            
            _lastClickFrame = Time.frameCount;
            
            if (_talkData == null)
            {
                Complete();
                return;
            }
            
            if (talkController == null)
                talkController = FindAnyObjectByType<Talk_Controller>(FindObjectsInactive.Include);
            
            if (talkController == null)
            {
                Debug.LogWarning("RunTalkEntity could not find Talk_Controller.", this);
                Complete();
                return;
            }
            
            _isTalking = true;
            
            talkController.OnTalkClosed -= HandleTalkClosed;
            talkController.OnTalkClosed += HandleTalkClosed;
            
            talkController.OnRewardRequested -= HandleRewardRequested;
            talkController.OnRewardRequested += HandleRewardRequested;
            talkController.OnChangeAction -= HandleChangeAction;
            talkController.OnChangeAction += HandleChangeAction;
            
            talkController.OnSuppressDefaultRewardRequested -= HandleSuppressDefaultRewardRequested;
            talkController.OnSuppressDefaultRewardRequested += HandleSuppressDefaultRewardRequested;
            talkController.OnRunEventBattleActionRequested -= HandleRunEventBattleActionRequested;
            talkController.OnRunEventBattleActionRequested += HandleRunEventBattleActionRequested;
            talkController.gameObject.SetActive(true);
            talkController.StartTalk(_talkData, true);
        }
        
        private void HandleRewardRequested(string rewardKey)
        {
            _onRewardRequested?.Invoke(rewardKey);
        }
        
        private void HandleChangeAction(string newValue)
        {
            _onChangeAction?.Invoke(newValue);
        }
        
        private void HandleSuppressDefaultRewardRequested(bool suppress)
        {
            _onSuppressStageRewardRequested?.Invoke(suppress);
        }
        
        private void HandleRunEventBattleActionRequested(RunEventBattleAction action)
        {
            _onRunEventBattleActionRequested?.Invoke(action);
        }
        
        private void HandleTalkClosed(TalkCloseReason reason)
        {
            if (!_isTalking)
                return;
            
            if (reason == TalkCloseReason.Cancel)
            {
                _onRunEventBattleActionRequested?.Invoke(RunEventBattleAction.Default);
                _isTalking = false;
                UnsubscribeTalkController();
                return;
            }
            
            Complete();
        }
        
        private void Complete()
        {
            if (_completed)
                return;
            
            _completed = true;
            _isTalking = false;
            UnsubscribeTalkController();
            
            Action onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke();
        }
        
        private void InstallClickProxies(GameObject root)
        {
            bool hasCollider = false;
            
            Collider2D[] colliders2D = root.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders2D.Length; i++)
            {
                InstallClickProxy(colliders2D[i].gameObject);
                hasCollider = true;
            }
            
            Collider[] colliders3D = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders3D.Length; i++)
            {
                InstallClickProxy(colliders3D[i].gameObject);
                hasCollider = true;
            }
            
            if (!hasCollider)
                Debug.LogWarning("RunTalkEntity needs a Collider2D or Collider to receive mouse clicks.", this);
        }
        
        private void InstallClickProxy(GameObject host)
        {
            if (host == null || host == gameObject)
                return;

            ClickProxy proxy = host.GetComponent<ClickProxy>();
            if (proxy == null)
                proxy = host.AddComponent<ClickProxy>();

            proxy.Bind(this);
        }
        
        private void OnDestroy()
        {
            UnsubscribeTalkController();
        }
        
        private void UnsubscribeTalkController()
        {
            if (talkController == null)
                return;

            talkController.OnTalkClosed -= HandleTalkClosed;
            talkController.OnRewardRequested -= HandleRewardRequested;
            talkController.OnChangeAction -= HandleChangeAction;
            talkController.OnSuppressDefaultRewardRequested -= HandleSuppressDefaultRewardRequested;
            talkController.OnRunEventBattleActionRequested -= HandleRunEventBattleActionRequested;
        }
        
        private sealed class ClickProxy : MonoBehaviour, IPointerClickHandler
        {
            private RunTalkEntity _target;
            
            public void Bind(RunTalkEntity target)
            {
                _target = target;
            }
            
            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button != PointerEventData.InputButton.Left)
                    return;

                _target?.TryStartTalk(false);
            }
            
            private void OnMouseDown()
            {
                _target?.TryStartTalk(true);
            }
        }
        
    }
    
}
