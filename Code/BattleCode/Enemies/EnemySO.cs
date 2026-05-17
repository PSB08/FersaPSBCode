using System.Collections.Generic;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Phases;
using PSB.Code.BattleCode.Enums;
using PSB.Code.BattleCode.Items;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies
{
    [CreateAssetMenu(fileName = "EnemySO", menuName = "SO/Enemy/EnemySO", order = 110)]
    public class EnemySO : ScriptableObject
    {
        [Header("Visual")]
        public Sprite icon;
        public RuntimeAnimatorController animController;
        
        [Header("Enemy inform")]
        public string enemyName;
        public EnemyGrade grade;
        public bool isRanged;
        
        [Header("Stat")]
        public StatListSO statListSO;
        [Header("Stat Overrides")]
        public StatOverride[] statOverrides;

        [Header("Progression")]
        public EnemyProgressionSO progressionSO;
        
        [Header("Drop")]
        public DropTableSO dropTable;
        
        [Header("Damage Scaling")]
        public string attackStatName = "Attack";
        public float attackStatScale = 0.5f;
        
        [Header("Attack Skills")]
        public SkillDataSO[] attackSkills;
        
        [Header("Phase System")]
        public EnemyPhaseData[] phases;
        
#if UNITY_EDITOR
        public void SyncStatOverrides()
        {
            Dictionary<StatSO, StatOverride> oldList = new();

            if (statOverrides != null)
            {
                foreach (StatOverride ov in statOverrides)
                {
                    if (ov == null || ov.Stat == null) continue;

                    if (!oldList.ContainsKey(ov.Stat))
                        oldList.Add(ov.Stat, ov);
                }
            }

            List<StatOverride> newList = new();

            foreach (StatSO stat in statListSO.statDataList)
            {
                if (stat == null) continue;

                if (oldList.TryGetValue(stat, out StatOverride overStat))
                {
                    newList.Add(overStat);
                }
                else
                {
                    newList.Add(new StatOverride(stat));
                }
            }

            statOverrides = newList.ToArray();
        }
#endif
        
    }
}