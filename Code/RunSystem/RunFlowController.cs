using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using UnityEngine;
using UnityEngine.UI;
using Work.PSB.Code.CoreSystem;
using Work.PSB.Code.FieldCode;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.RunSystem
{
    public class RunFlowController : MonoBehaviour
    {
        [SerializeField] private RunMapSO fallbackRunMap;
        [SerializeField] private TransitionController transition;
        [SerializeField] private RunEventController eventController;
        [SerializeField] private RestPanelUI restPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button nextButton;
        
        private void Awake()
        {
            startButton.onClick.AddListener(OnClickStart);
            nextButton.onClick.AddListener(OnClickNext);
        }
        
        private void Start()
        {
            if (RunStateStore.Phase == RunPhase.None)
                RunStateStore.PrepareRun(fallbackRunMap);
            
            if (RunStateStore.IsReadyToStart)
            {
                ShowStartOnly();
                return;
            }
            
            if (RunStateStore.IsWaitingNext)
            {
                ShowNextOnly();
                return;
            }
            
            if (RunStateStore.IsRunning)
            {
                HideButtons();
                ExecuteCurrentNode();
                return;
            }
            
            HideButtons();
        }
        
        public void OnClickStart()
        {
            if (!RunStateStore.IsReadyToStart)
                return;
            
            RunStateStore.StartCurrentNode();
            HideButtons();

            if (ShouldReloadRunSceneBeforeExecute())
            {
                LoadRunScene(RunStateStore.Map);
                return;
            }
            
            ExecuteCurrentNode();
        }
        
        public void OnClickNext()
        {
            if (!RunStateStore.IsWaitingNext)
                return;
            
            RunMapSO map = RunStateStore.Map;
            RunAdvanceResult advanceResult = RunStateStore.AdvanceToNextNode(notify: false);
            
            if (advanceResult == RunAdvanceResult.NextChapter)
            {
                LoadRunScene(map);
                return;
            }
            
            if (advanceResult == RunAdvanceResult.Finished)
            {
                LoadTitleScene(map);
                return;
            }
            
            if (!RunStateStore.IsRunning)
            {
                HideButtons();
                return;
            }

            if (ShouldReloadRunSceneBeforeExecute())
            {
                LoadRunScene(map);
                return;
            }
            
            HideButtons();
            ExecuteCurrentNode();
        }

        private bool ShouldReloadRunSceneBeforeExecute()
        {
            if (!RunStateStore.IsRunning)
                return false;

            return RunStateStore.CurrentType == RunNodeType.Event ||
                   RunStateStore.CurrentType == RunNodeType.Rest;
        }
        
        private void ExecuteCurrentNode()
        {
            RunMapSO map = RunStateStore.Map != null ? RunStateStore.Map : fallbackRunMap;
            if (map == null)
                return;
            
            switch (RunStateStore.CurrentType)
            {
                case RunNodeType.Battle:
                    StartBattle(map.PickEncounter(RunStateStore.ChapterIndex, RunStateStore.NodeIndex), map);
                    break;
                case RunNodeType.Boss:
                    StartBattle(map.PickEncounter(RunStateStore.ChapterIndex, RunStateStore.NodeIndex), map);
                    break;
                case RunNodeType.Event:
                    StartEvent(map);
                    break;
                case RunNodeType.Rest:
                    if (restPanel != null)
                        restPanel.Open(RunStateStore.CurrentRestHealPercent(), MarkWaitingNext);
                    else
                        MarkWaitingNext();
                    break;
            }
        }
        
        private void StartEvent(RunMapSO map)
        {
            RunEventSO data = RunStateStore.CurrentEvent;
            
            if (data == null)
            {
                data = map.PickEvent(RunStateStore.ChapterIndex, RunStateStore.NodeIndex);
                RunStateStore.BeginEvent(data);
            }
            
            if (data == null)
            {
                MarkWaitingNext();
                return;
            }
            
            if (eventController == null)
            {
                HandleEventStageComplete(data, map);
                return;
            }
            
            eventController.Begin(data, data.GetTalkStage(RunStateStore.EventPhase),
                () => HandleEventStageComplete(data, map));
        }
        
        private void HandleEventStageComplete(RunEventSO data, RunMapSO map)
        {
            if (data.HasBattle && RunStateStore.EventPhase == RunEventPhase.BeforeBattle)
            {
                if (eventController != null && eventController.ShouldSkipBattle)
                {
                    CompleteCurrentEvent();
                    return;
                }
                
                StartEventBattle(data, map);
                return;
            }
            
            CompleteCurrentEvent();
        }
        
        private void CompleteCurrentEvent()
        {
            if (eventController != null)
                eventController.DisableEntityAfterFinished();
            
            RunStateStore.CompleteEvent();
            ShowNextOnly();
        }
        
        private void StartEventBattle(RunEventSO data, RunMapSO map)
        {
            if (data == null || data.battleEncounter == null)
            {
                RunStateStore.CompleteEvent();
                ShowNextOnly();
                return;
            }
            
            if (map == null || string.IsNullOrEmpty(map.battleSceneName))
            {
                Debug.LogWarning("Run event battle scene name is empty.", this);
                RunStateStore.CompleteEvent();
                ShowNextOnly();
                return;
            }
            
            BattlePresentationSO presentation = data.battlePresentation != null ? data.battlePresentation :
                map.PickPresentation(RunStateStore.ChapterIndex, RunStateStore.NodeIndex);
            
            RunEnemyProgressionSync.ApplyCurrentChapter();
            RunStateStore.EnterEventBattle();
            RunBattleContext.Set(RunNodeType.Event);
            BattleRuntimeData.Set(data.battleEncounter, presentation);
            
            if (transition != null)
                transition.Transition(map.battleSceneName);
            else
                SceneLoader.LoadScene(map.battleSceneName);
        }
        
        private void StartBattle(BattleEncounterSO encounter, RunMapSO map)
        {
            if (encounter == null)
            {
                Debug.LogWarning($"Run battle node has no encounter. nodeIndex={RunStateStore.NodeIndex}, nodeType={RunStateStore.CurrentType}", this);
                MarkWaitingNext();
                return;
            }
            
            if (map == null || string.IsNullOrEmpty(map.battleSceneName))
            {
                Debug.LogWarning("Run battle scene name is empty.", this);
                MarkWaitingNext();
                return;
            }
            
            RunEnemyProgressionSync.ApplyCurrentChapter();
            
            RunBattleContext.Set(RunStateStore.CurrentType);
            BattleRuntimeData.Set(encounter, map.PickPresentation(RunStateStore.ChapterIndex, RunStateStore.NodeIndex));
            
            if (transition != null)
                transition.Transition(map.battleSceneName);
            else
                SceneLoader.LoadScene(map.battleSceneName);
        }
        
        private void LoadTitleScene(RunMapSO map)
        {
            string titleSceneName = map != null && 
                                    !string.IsNullOrEmpty(map.titleSceneName) ? map.titleSceneName : "SW_Title";
            
            RunBattleContext.Clear();
            RunStateStore.Clear();
            HideButtons();
            
            if (transition != null)
                transition.Transition(titleSceneName);
            else
                SceneLoader.LoadScene(titleSceneName);
        }

        private void LoadRunScene(RunMapSO map)
        {
            string runSceneName = map != null && !string.IsNullOrEmpty(map.runSceneName)
                ? map.runSceneName
                : null;

            if (string.IsNullOrEmpty(runSceneName))
            {
                ShowStartOnly();
                return;
            }

            HideButtons();

            if (transition != null)
                transition.Transition(runSceneName);
            else
                SceneLoader.LoadScene(runSceneName);
        }
        
        private void MarkWaitingNext()
        {
            RunStateStore.WaitNext();
            ShowNextOnly();
        }
        
        private void ShowStartOnly()
        {
            if (startButton != null) startButton.gameObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
        }
        
        private void ShowNextOnly()
        {
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }
        
        private void HideButtons()
        {
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
        }
        
    }
    
}
