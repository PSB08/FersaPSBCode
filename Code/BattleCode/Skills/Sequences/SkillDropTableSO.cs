using System;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    [Serializable]
    public class SkillDropEntry
    {
        public SkillRewardSO reward;
        public float dropRate = 1f;
    }
    
    [CreateAssetMenu(fileName = "SkillDropTable", menuName = "SO/Item/SkillDropTable", order = 98)]
    public class SkillDropTableSO : ScriptableObject
    {
        public SkillDropEntry[] entries;
    }
    
}