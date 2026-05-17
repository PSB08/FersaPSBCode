using UnityEngine;
using Work.PSB.Code.CoreSystem.UpgradeSystem;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Phases
{
    public enum PhaseActionType
    {
        ChangeSkills,
        ApplyBuffs,
        SpawnEnemies
    }

    [System.Serializable]
    public class EnemyPhaseData
    {
        [Range(0.01f, 1f)]
        public float hpThresholdPercent = 0.5f;

        public PhaseActionType actionType;

        public SkillDataSO[] phaseSkills;

        public MilestoneEffectSO[] phaseBuffs;
        
        public EnemySO[] phaseEnemies;
    }
    
}