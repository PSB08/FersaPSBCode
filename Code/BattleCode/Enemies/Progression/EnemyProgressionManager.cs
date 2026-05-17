using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public class EnemyProgressionManager : MonoBehaviour
    {
        //강화 내용을 쫙 뿌리는 코드 
        [SerializeField] private int defaultStage = 0;

        public static int CurrentStage { get; private set; } = 0;

        private void Awake()
        {
            CurrentStage = EnemyProgressionService.LoadStage(defaultStage);
            Bus<EnemyProgressionStageChanged>.Raise(new EnemyProgressionStageChanged(CurrentStage));
        }

        //Test
        private void Update()
        {
            #if UNITY_EDITOR
            if (Keyboard.current.lKey.wasPressedThisFrame)
                IncreaseStage(1);  //upgrade
            if (Keyboard.current.rKey.wasPressedThisFrame)
                ResetStage();  //reset
            #endif
        }

        public static void SetStage(int stage, bool save = true)
        {
            stage = Mathf.Max(0, stage);
            if (CurrentStage == stage) return;

            CurrentStage = stage;

            if (save)
                EnemyProgressionService.SaveStage(CurrentStage);

            Bus<EnemyProgressionStageChanged>.Raise(new EnemyProgressionStageChanged(CurrentStage));
        }

        //EnemyProgressionManager.IncreaseStage(1);
        public static void IncreaseStage(int delta = 1, bool save = true)
        {
            Debug.Log($"<color=aqua>[EnemyProgressData]증가 : {CurrentStage + delta}</color>");
            SetStage(CurrentStage + delta, save);
        }
        
        public static void ResetStage()
        {
            Debug.Log("<color=orange>[EnemyProgressData]삭제</color>");
            SetStage(0, save: true);
        }
        
    }
}