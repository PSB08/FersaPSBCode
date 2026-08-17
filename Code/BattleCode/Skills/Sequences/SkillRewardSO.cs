using System;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    [CreateAssetMenu(fileName = "SkillReward", menuName = "SO/Item/SkillRewardSO", order = 97)]
    public class SkillRewardSO : ScriptableObject
    {
        public SkillDataSO skill;
        public GameObject skillVisualPrefab;
    }
}