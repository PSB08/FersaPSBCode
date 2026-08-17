using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using PSW.Code.Talk;
using UnityEngine;
using Work.PSB.Code.CoreSystem;
using Work.PSB.Code.FieldCode;

namespace Work.PSB.Code.RunSystem
{
    public enum RunEventBattleHealthEndMode
    {
        None,
        HealthOne,
        HealthPercent
    }
    
    [CreateAssetMenu(fileName = "RunEvent", menuName = "SO/Run/Event")]
    public class RunEventSO : ScriptableObject
    {
        [Header("Entity")]
        public GameObject entityPrefab;
        public GameObject systemPrefab;
        public Sprite sprite;
        public RuntimeAnimatorController animatorController;
        public bool disableEntityAfterFinished;
        
        [Header("Talk")]
        public TalkStage talkStage = new TalkStage();
        
        [Header("Event Battle")]
        public BattleEncounterSO battleEncounter;
        public BattlePresentationSO battlePresentation;
        public TalkStage victoryTalkStage = new TalkStage();
        public TalkStage failureTalkStage = new TalkStage();
        public bool rollbackBattleLootOnVictory = true;
        
        [Header("Event Battle Challenge")]
        public RunEventBattleChallengeMode battleChallengeMode;
        [Min(0)] public int battleChallengeTargetEnemyIndex;
        [Min(1)] public int battleChallengeTurnLimit = 3;
        [Min(1f)] public float battleChallengeRequiredDamage = 100f;
        public bool battleChallengeProtectTarget = true;
        [Min(0f)] public float battleChallengeEndDelay = 0.6f;
        
        [Header("Event Battle Health End")]
        public RunEventBattleHealthEndMode battleHealthEndMode;
        
        [Min(0)]
        public int battleHealthTargetEnemyIndex;
        
        [Range(0.01f, 1f)]
        public float battleHealthTargetPercent = 0.3f;
        
        [Header("Reward")]
        public string talkId;
        public TalkRewardGiver.RewardEntry[] rewardEntries;
        
        public string TalkId => string.IsNullOrEmpty(talkId) ? name : talkId;
        public bool HasBattle => battleEncounter != null;
        public bool HasBattleHealthEndCondition => HasBattle && battleHealthEndMode != RunEventBattleHealthEndMode.None;
        public bool HasBattleChallenge => HasBattle && battleChallengeMode != RunEventBattleChallengeMode.None;
        
        public TalkStage GetTalkStage()
        {
            return talkStage;
        }
        
        public TalkStage GetTalkStage(RunEventPhase phase)
        {
            if (phase == RunEventPhase.AfterVictory)
            {
                return RunStateStore.EventBattleResult == RunEventBattleResult.Failure
                    ? failureTalkStage : victoryTalkStage;
            }
            
            return talkStage;
        }
        
        public string GetStageRewardKey(TalkStage stage)
        {
            if (stage != null && !string.IsNullOrEmpty(stage.rewardKey))
                return stage.rewardKey;
            
            string firstRewardKey = ResolveFirstRewardKey();
            if (!string.IsNullOrEmpty(firstRewardKey))
                return firstRewardKey;
            
            if (rewardEntries == null || rewardEntries.Length == 0)
                return string.Empty;
            
            return BuildAutoRewardKey(0);
        }
        
        public bool CanStageRequestReward(TalkStage stage)
        {
            if (stage == null)
                return false;
            
            return stage.actionMode == InteractActionMode.TalkAndReward ||
                   stage.actionMode == InteractActionMode.RewardOnly ||
                   stage.actionMode == InteractActionMode.TalkAndBarter;
        }
        
        private string ResolveFirstRewardKey()
        {
            if (rewardEntries == null)
                return string.Empty;
            
            for (int i = 0; i < rewardEntries.Length; i++)
            {
                TalkRewardGiver.RewardEntry entry = rewardEntries[i];
                if (entry != null && !string.IsNullOrEmpty(entry.rewardKey))
                    return entry.rewardKey;
            }
            
            return string.Empty;
        }
        
        public TalkRewardGiver.RewardEntry[] CreateRuntimeRewardEntries(TalkStage stage = null)
        {
            if (rewardEntries == null || rewardEntries.Length == 0)
                return rewardEntries;
            
            TalkStage activeStage = stage != null ? stage : GetTalkStage();
            string stageRewardKey = GetStageRewardKey(activeStage);
            TalkRewardGiver.RewardEntry[] runtimeEntries = new TalkRewardGiver.RewardEntry[rewardEntries.Length];
            
            for (int i = 0; i < rewardEntries.Length; i++)
            {
                TalkRewardGiver.RewardEntry entry = rewardEntries[i];
                if (entry == null)
                    continue;
                
                runtimeEntries[i] = new TalkRewardGiver.RewardEntry
                {
                    rewardKey = string.IsNullOrEmpty(entry.rewardKey) ? stageRewardKey : entry.rewardKey,
                    rewardType = entry.rewardType,
                    dropTable = entry.dropTable,
                    skillDropTable = entry.skillDropTable,
                    
                    useItemDropper = entry.useItemDropper,
                    rewardObject = entry.rewardObject,
                    rewardRelic = entry.rewardRelic,
                    relicList = entry.relicList,
                    
                    healValue = entry.healValue,
                    healMode = entry.healMode,
                    uiType = entry.uiType,
                    
                    ally = entry.ally,
                    allyDatabase = entry.allyDatabase,
                    autoEquipAlly = entry.autoEquipAlly,
                    skipIfAlreadyOwned = entry.skipIfAlreadyOwned,
                    duplicateAllyHealPercent = entry.duplicateAllyHealPercent
                };
            }
            
            return runtimeEntries;
        }
        
        private string BuildAutoRewardKey(int rewardIndex)
        {
            int rewardNumber = Mathf.Max(0, rewardIndex) + 1;
            return $"{TalkId}_Reward_{rewardNumber}";
        }
        
    }
}
