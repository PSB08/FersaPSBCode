using UnityEngine;
using Work.PSB.Code.CoreSystem;

namespace Work.PSB.Code.RunSystem
{
    public class RunEntryButton : MonoBehaviour
    {
        [SerializeField] private RunMapSO runMap;
        [SerializeField] private TransitionController transition;
        [SerializeField] private string runSceneName = "RunScene";
        
        public void OnClickStartGame()
        {
            RunStateStore.PrepareRun(runMap);
            RunBattleContext.Clear();
            
            if (transition != null)
                transition.Transition(runSceneName);
        }
        
    }
}
