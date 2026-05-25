using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.FieldCode.MapSaves;
using Work.PSW.Code.Title;

namespace Work.PSB.Code.CoreSystem.Tests
{
    public class TitleAllResetCode : MonoBehaviour
    {
        [SerializeField] private DataManager dataManager;
        [SerializeField] private TitleButton titleButton;

        public void AllResetEvent()
        {
            SceneSaveSystem.DeleteAllSaves();
            EnemyProgressionManager.ResetStage();
            Bus<ResetAllPrefEvent>.Raise(new ResetAllPrefEvent());
            PlayerPrefs.DeleteKey("Stage");
            PlayerPrefs.Save();
            titleButton?.SetSaveKey();
            dataManager.ResetData();
        }
        
    }
}