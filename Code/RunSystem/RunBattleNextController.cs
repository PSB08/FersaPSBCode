using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using UnityEngine;
using UnityEngine.UI;
using Work.PSB.Code.CoreSystem;
using Work.PSB.Code.FieldCode;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.RunSystem
{
    public class RunBattleNextController : MonoBehaviour
    {
        [SerializeField] private Button nextButton;
        [SerializeField] private TransitionController transition;
        
        private void Awake()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(OnClickNext);
                nextButton.onClick.AddListener(OnClickNext);
                nextButton.gameObject.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            if (nextButton != null)
                nextButton.onClick.RemoveListener(OnClickNext);
        }
        
        public void ShowNext()
        {
            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
        }
        
        public void HideNext()
        {
            if (nextButton != null)
                nextButton.gameObject.SetActive(false);
        }
        
        public void OnClickNext()
        {
            if (!RunStateStore.IsWaitingNext)
                return;
            
            RunMapSO map = RunStateStore.Map;
            HideNext();
            RunAdvanceResult advanceResult = RunStateStore.AdvanceToNextNode(notify: false);
            
            if (advanceResult == RunAdvanceResult.Finished)
            {
                GoToTitleScene(map);
                return;
            }
            
            if (advanceResult == RunAdvanceResult.NextChapter)
            {
                GoToRunScene(map);
                return;
            }
            
            if (!RunStateStore.IsRunning)
                return;
            
            ExecuteCurrentNode();
        }
        
        private void ExecuteCurrentNode()
        {
            RunMapSO map = RunStateStore.Map;
            if (map == null)
            {
                Debug.LogWarning("Run map is empty. Cannot advance from battle next button.", this);
                return;
            }
            
            switch (RunStateStore.CurrentType)
            {
                case RunNodeType.Battle:
                    StartBattle(map.PickEncounter(RunStateStore.ChapterIndex, RunStateStore.NodeIndex), map);
                    break;
                case RunNodeType.Boss:
                    StartBattle(map.PickEncounter(RunStateStore.ChapterIndex, RunStateStore.NodeIndex), map);
                    break;
                case RunNodeType.Event:
                case RunNodeType.Rest:
                    GoToRunScene(map);
                    break;
            }
        }
        
        private void StartBattle(BattleEncounterSO encounter, RunMapSO map)
        {
            if (encounter == null)
            {
                Debug.LogWarning($"Run battle node has no encounter. nodeIndex={RunStateStore.NodeIndex}, nodeType={RunStateStore.CurrentType}", this);
                RunStateStore.WaitNext();
                ShowNext();
                return;
            }
            
            if (string.IsNullOrEmpty(map.battleSceneName))
            {
                Debug.LogWarning("Run battle scene name is empty.", this);
                RunStateStore.WaitNext();
                ShowNext();
                return;
            }
            
            RunBattleContext.Set(RunStateStore.CurrentType);
            BattleRuntimeData.Set(encounter, map.PickPresentation(RunStateStore.ChapterIndex, RunStateStore.NodeIndex));
            LoadScene(map.battleSceneName);
        }
        
        private void GoToRunScene(RunMapSO map)
        {
            RunBattleContext.Clear();
            
            if (map == null || string.IsNullOrEmpty(map.runSceneName))
            {
                Debug.LogWarning("Run scene name is empty.", this);
                RunStateStore.WaitNext();
                ShowNext();
                return;
            }
            
            LoadScene(map.runSceneName);
        }
        
        private void GoToTitleScene(RunMapSO map)
        {
            RunBattleContext.Clear();
            RunStateStore.Clear();
            LoadScene(map.titleSceneName);
        }
        
        private void LoadScene(string sceneName)
        {
            if (transition != null)
            {
                transition.Transition(sceneName);
                return;
            }
            
            SceneLoader.LoadScene(sceneName);
        }
        
    }
}
