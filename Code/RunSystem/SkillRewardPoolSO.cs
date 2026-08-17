using System;
using System.Collections.Generic;
using UnityEngine;
using YIS.Code.Skills;

namespace Work.PSB.Code.RunSystem
{
    [CreateAssetMenu(fileName = "SkillRewardPool", menuName = "SO/Run/SkillRewardPool")]
    public class SkillRewardPoolSO : ScriptableObject
    {
        public SkillDataSO[] skills;
        
        public List<SkillDataSO> Pick(int count, Predicate<SkillDataSO> filter = null)
        {
            List<SkillDataSO> source = new();
            List<SkillDataSO> result = new();
            
            if (skills == null)
                return result;
            
            foreach (SkillDataSO skill in skills)
            {
                if (skill != null && (filter == null || filter(skill)))
                    source.Add(skill);
            }
            
            while (result.Count < count && source.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, source.Count);
                result.Add(source[index]);
                source.RemoveAt(index);
            }
            
            return result;
        }
        
    }
}
