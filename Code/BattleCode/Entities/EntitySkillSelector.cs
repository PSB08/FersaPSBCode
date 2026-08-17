using System;
using System.Collections.Generic;
using UnityEngine;
using YIS.Code.Defines;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Entities
{
    public class EntitySkillSelector
    {
        private readonly List<int> _validIndices = new();
        private readonly List<int> _availableIndices = new();
        private readonly List<int> _temporaryIndices = new();
        
        private SkillDataSO[] _skills;
        private bool _useAvailableIndices;
        
        public void SetSkills(SkillDataSO[] skills)
        {
            _skills = skills;
            _validIndices.Clear();
            ClearAvailableIndices();
            
            if (_skills == null)
                return;
            
            for (int i = 0; i < _skills.Length; i++)
            {
                if (_skills[i] != null)
                    _validIndices.Add(i);
            }
        }
        
        public SkillDataSO GetSkill(int index)
        {
            if (index < 0 || _skills == null || index >= _skills.Length)
                return null;
            
            return _skills[index];
        }
        
        public bool TryGetSkill(int index, out SkillDataSO skillData)
        {
            skillData = GetSkill(index);
            return skillData != null;
        }
        
        public bool CanSelect(int index)
        {
            if (GetSkill(index) == null)
                return false;
            
            return !_useAvailableIndices || _availableIndices.Contains(index);
        }
        
        public void SetAvailableIndices(IReadOnlyList<int> availableIndices)
        {
            _availableIndices.Clear();
            
            if (availableIndices == null)
            {
                _useAvailableIndices = false;
                return;
            }
            
            for (int i = 0; i < availableIndices.Count; i++)
            {
                int index = availableIndices[i];
                if (GetSkill(index) != null && !_availableIndices.Contains(index))
                    _availableIndices.Add(index);
            }
            
            _useAvailableIndices = true;
        }
        
        public void ClearAvailableIndices()
        {
            _availableIndices.Clear();
            _useAvailableIndices = false;
        }
        
        public int PickRandom(IReadOnlyList<int> excludedIndices = null)
        {
            return PickRandomFiltered(null, null, excludedIndices);
        }
        
        public int PickRandomFiltered(Func<SkillDataSO, bool> predicate,
            Func<int, bool> canUse = null, IReadOnlyList<int> excludedIndices = null)
        {
            IReadOnlyList<int> candidates = GetCandidates();
            if (candidates.Count == 0)
                return -1;
            
            _temporaryIndices.Clear();
            
            for (int i = 0; i < candidates.Count; i++)
            {
                int index = candidates[i];
                if (IsExcluded(index, excludedIndices) || (canUse != null && !canUse(index)))
                    continue;
                
                SkillDataSO skillData = GetSkill(index);
                if (skillData == null || (predicate != null && !predicate(skillData)))
                    continue;
                
                _temporaryIndices.Add(index);
            }
            
            return _temporaryIndices.Count > 0 ? 
                _temporaryIndices[UnityEngine.Random.Range(0, _temporaryIndices.Count)] : -1;
        }
        
        public int PickHighestDamage(Func<SkillDataSO, bool> predicate, 
            Func<int, bool> canUse = null, IReadOnlyList<int> excludedIndices = null)
        {
            IReadOnlyList<int> candidates = GetCandidates();
            int bestIndex = -1;
            float bestDamage = float.NegativeInfinity;
            
            for (int i = 0; i < candidates.Count; i++)
            {
                int index = candidates[i];
                if (IsExcluded(index, excludedIndices) || (canUse != null && !canUse(index)))
                {
                    continue;
                }
                
                SkillDataSO skillData = GetSkill(index);
                if (skillData == null || (predicate != null && !predicate(skillData)))
                    continue;
                
                float damage = Mathf.Max(0f, skillData.damage);
                if (damage > bestDamage ||
                    (Mathf.Approximately(damage, bestDamage) && UnityEngine.Random.value < 0.5f))
                {
                    bestDamage = damage;
                    bestIndex = index;
                }
            }
            
            return bestIndex;
        }
        
        public int PickHighestDamageThenGrade(Func<SkillDataSO, bool> predicate,
            Func<int, bool> canUse = null, IReadOnlyList<int> excludedIndices = null)
        {
            IReadOnlyList<int> candidates = GetCandidates();
            int bestIndex = -1;
            float bestDamage = float.NegativeInfinity;
            int bestGrade = -1;
            int tieCount = 0;
            
            for (int i = 0; i < candidates.Count; i++)
            {
                int index = candidates[i];
                if (IsExcluded(index, excludedIndices) || (canUse != null && !canUse(index)))
                    continue;
                
                SkillDataSO skillData = GetSkill(index);
                if (skillData == null || (predicate != null && !predicate(skillData)))
                    continue;
                
                float damage = Mathf.Max(0f, skillData.damage);
                int grade = GetGradePriority(skillData.grade);
                bool sameDamage = bestIndex >= 0 && Mathf.Approximately(damage, bestDamage);
                bool isBetter = bestIndex < 0 || (!sameDamage && damage > bestDamage) || (sameDamage && grade > bestGrade);
                
                if (isBetter)
                {
                    bestIndex = index;
                    bestDamage = damage;
                    bestGrade = grade;
                    tieCount = 1;
                    continue;
                }
                
                if (!sameDamage || grade != bestGrade)
                    continue;
                
                tieCount++;
                if (UnityEngine.Random.Range(0, tieCount) == 0)
                    bestIndex = index;
            }
            
            return bestIndex;
        }
        
        private IReadOnlyList<int> GetCandidates()
        {
            return _useAvailableIndices ? _availableIndices : _validIndices;
        }
        
        private static bool IsExcluded(int index, IReadOnlyList<int> excludedIndices)
        {
            if (excludedIndices == null)
                return false;
            
            for (int i = 0; i < excludedIndices.Count; i++)
            {
                if (excludedIndices[i] == index)
                    return true;
            }
            
            return false;
        }
        
        private static int GetGradePriority(Grade grade)
        {
            return grade >= Grade.Common && grade <= Grade.Legendary
                ? (int)grade
                : -1;
        }
        
    }
}