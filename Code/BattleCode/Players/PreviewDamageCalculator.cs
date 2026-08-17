using System;
using System.Collections.Generic;
using System.Linq;
using CIW.Code;
using UnityEngine;
using YIS.Code.Combat;
using YIS.Code.Defines;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Players
{
    public class PreviewDamageCalculator : MonoBehaviour, IModule
    {
        private Entity _owner;
        private BuffModule _buffModule;
        
        private struct SlotChainState
        {
            public bool Left;
            public bool Right;
            public bool IsChained;
        }
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Entity;
            if (_owner != null)
            {
                _buffModule = _owner.GetModule<BuffModule>();
                
            }
        }
        
        public (float curDmg, float accDmg) CalculatePreviewForTarget(Entity target, SkillDataSO[] skills,
            bool[] setIndex, int previewSlotIndex, bool[] isHitBySlot = null)
        {
            if (skills == null || setIndex == null || target == null)
                return (0f, 0f);
            
            float curDmg = 0f;
            float accDmg = 0f;
            float damageModifier = _owner != null ? _owner.SkillDamageModifier : 0f;
            
            bool[] links = BuildLinks(skills, setIndex);
            SlotChainState[] chainStates = BuildSlotChainStates(skills, setIndex);
            
            bool isAllChained = IsAllChained(skills, setIndex, links);
            Elemental[] slotVirtualEnchants = BuildSlotVirtualEnchants(skills, chainStates);
            
            for (int i = 0; i < skills.Length; i++)
            {
                SkillDataSO skillData = skills[i];
                if (!IsSelectedSkill(skillData, setIndex, i))
                    continue;
                
                bool isChained = isAllChained || chainStates[i].IsChained;
                bool providedEnchant = IsEnchantSkill(skillData) &&
                                       (chainStates[i].Left || chainStates[i].Right);
                bool isAttack = IsAttackSkill(skillData) || 
                                IsEnchantSkill(skillData) && !providedEnchant || !isChained;
                
                if (!isAttack)
                    continue;
                
                if (isHitBySlot != null && i < isHitBySlot.Length && !isHitBySlot[i])
                    continue;
                
                float baseDamage = Mathf.Max(0f, skillData.damage);
                if (baseDamage <= 0f)
                    continue;
                
                float finalMultiplier = isAllChained
                    ? EntityDamageCalcModule.ALL_CHAIN_BONUS_MULTIPLIER : 1f;
                
                _ = slotVirtualEnchants[i];
                
                float modifiedDamage = Mathf.Max(0f,
                    baseDamage + damageModifier);
                
                int finalDamage = Mathf.Max(0,
                    Mathf.RoundToInt(modifiedDamage * finalMultiplier));
                
                if (i == previewSlotIndex)
                    curDmg += finalDamage;
                else
                    accDmg += finalDamage;
            }
            
            return (curDmg, accDmg);
        }
        
        private bool[] BuildLinks(SkillDataSO[] skills, bool[] setIndex)
        {
            bool[] links = skills.Length >= 2 ? new bool[skills.Length - 1] : Array.Empty<bool>();
            
            for (int i = 0; i < links.Length; i++)
            {
                if (i >= setIndex.Length || i + 1 >= setIndex.Length)
                    continue;
                
                if (!setIndex[i] || !setIndex[i + 1])
                    continue;
                
                SkillDataSO leftSO = skills[i];
                SkillDataSO rightSO = skills[i + 1];
                
                if (leftSO == null || rightSO == null)
                    continue;
                
                bool leftCanParticipate = leftSO.checkSkillType.HasFlag(CheckType.Previous) ||
                                          leftSO.checkSkillType.HasFlag(CheckType.Next);
                
                bool rightCanParticipate = rightSO.checkSkillType.HasFlag(CheckType.Previous) ||
                                           rightSO.checkSkillType.HasFlag(CheckType.Next);
                
                bool leftToRight = leftSO.checkSkillType.HasFlag(CheckType.Next) &&
                                   CanChain(leftSO, rightSO);
                
                bool rightToLeft = rightSO.checkSkillType.HasFlag(CheckType.Previous) &&
                                   CanChain(rightSO, leftSO);
                
                links[i] = leftCanParticipate && rightCanParticipate && (leftToRight || rightToLeft);
            }
            
            return links;
        }
        
        private SlotChainState[] BuildSlotChainStates(SkillDataSO[] skills, bool[] setIndex)
        {
            SlotChainState[] states = new SlotChainState[skills.Length];
            
            for (int i = 0; i < skills.Length; i++)
            {
                if (i >= setIndex.Length) continue;
                if (!setIndex[i]) continue;
                
                SkillDataSO currentSO = skills[i];
                if (currentSO == null)
                    continue;
                
                bool reqPrev = currentSO.checkSkillType.HasFlag(CheckType.Previous);
                bool reqNext = currentSO.checkSkillType.HasFlag(CheckType.Next);
                
                bool left = false;
                bool right = false;
                bool isChained = false;
                
                if (reqPrev && reqNext)
                {
                    if (i > 0 && i < skills.Length - 1)
                    {
                        SkillDataSO prevSO = skills[i - 1];
                        SkillDataSO nextSO = skills[i + 1];
                        
                        if (prevSO != null && nextSO != null)
                        {
                            left = CanChain(currentSO, prevSO);
                            right = CanChain(currentSO, nextSO);
                            
                            if (left && right)
                                isChained = true;
                        }
                    }
                }
                else if (reqNext)
                {
                    if (i < skills.Length - 1)
                    {
                        SkillDataSO nextSO = skills[i + 1];
                        
                        if (nextSO != null)
                        {
                            right = CanChain(currentSO, nextSO);
                            
                            if (right)
                                isChained = true;
                        }
                    }
                }
                else if (reqPrev)
                {
                    if (i > 0)
                    {
                        SkillDataSO prevSO = skills[i - 1];
                        
                        if (prevSO != null)
                        {
                            left = CanChain(currentSO, prevSO);
                            
                            if (left)
                                isChained = true;
                        }
                    }
                }
                
                states[i] = new SlotChainState
                {
                    Left = left,
                    Right = right,
                    IsChained = isChained
                };
            }
            
            return states;
        }
        
        private bool IsAllChained(SkillDataSO[] skills, bool[] setIndex, bool[] links)
        {
            if (skills.Length <= 1)
                return false;
            
            if (links.Length <= 0)
                return false;
            
            for (int i = 0; i < skills.Length; i++)
            {
                if (i >= setIndex.Length)
                    return false;
                
                if (!setIndex[i])
                    return false;
                
                if (skills[i] == null)
                    return false;
            }
            
            return links.All(link => link);
        }
        
        private Elemental[] BuildSlotVirtualEnchants(SkillDataSO[] skills, SlotChainState[] chainStates)
        {
            Elemental[] slotVirtualEnchants = new Elemental[skills.Length];
            
            Elemental baseEnchant = Elemental.None;
            
            if (_buffModule != null)
                _buffModule.TryGetElementalOverrideOrImmediately(out baseEnchant);
            
            for (int i = 0; i < slotVirtualEnchants.Length; i++)
                slotVirtualEnchants[i] = baseEnchant;
            
            for (int i = 0; i < skills.Length; i++)
            {
                SkillDataSO skillData = skills[i];
                if (!IsEnchantSkill(skillData))
                    continue;
                
                Elemental provideElement = skillData.elemental;
                
                slotVirtualEnchants[i] = provideElement;
                
                if (chainStates[i].Left && i > 0 && IsEnchantableSkill(skills[i - 1]))
                    slotVirtualEnchants[i - 1] = provideElement;
                
                if (chainStates[i].Right && i < skills.Length - 1 && IsEnchantableSkill(skills[i + 1]))
                    slotVirtualEnchants[i + 1] = provideElement;
            }
            
            return slotVirtualEnchants;
        }
        
        public List<Entity> GetActualSimulatedTargets(int index, SkillDataSO[] skills,
            bool[] setIndex, List<Entity> defaultTargets)
        {
            if (skills == null || setIndex == null || index < 0 || index >= skills.Length)
                return defaultTargets;
            
            if (index >= setIndex.Length)
                return defaultTargets;
            
            if (skills[index] == null || !setIndex[index])
                return defaultTargets;
            
            bool[] links = BuildLinks(skills, setIndex);
            SlotChainState[] chainStates = BuildSlotChainStates(skills, setIndex);
            bool isAllChained = IsAllChained(skills, setIndex, links);
            bool isChained = isAllChained || chainStates[index].IsChained;
            
            return ResolvePreviewTargets(skills[index], isChained, defaultTargets);
        }
        
        private static bool IsSelectedSkill(SkillDataSO skillData, bool[] setIndex, int index)
        {
            return skillData != null && index >= 0 &&
                   index < setIndex.Length && setIndex[index];
        }
        
        private static bool CanChain(SkillDataSO source, SkillDataSO other)
        {
            if (source == null || other == null)
                return false;
            
            SkillCategory category = source.SkillCategory;
            return category != SkillCategory.None && category 
                != SkillCategory.End && category == other.SkillCategory;
        }
        
        private static bool IsAttackSkill(SkillDataSO skillData)
        {
            return skillData != null &&
                   (skillData.SkillCategory == SkillCategory.Attack ||
                    skillData.damage > 0f);
        }
        
        private static bool IsEnchantSkill(SkillDataSO skillData)
        {
            return skillData != null &&
                   skillData.SkillCategory == SkillCategory.Special;
        }
        
        private static bool IsEnchantableSkill(SkillDataSO skillData)
        {
            return skillData != null &&
                   skillData.SkillCategory is SkillCategory.Attack or SkillCategory.Special;
        }
        
        private static List<Entity> ResolvePreviewTargets(SkillDataSO skillData, 
            bool isChained, List<Entity> defaultTargets)
        {
            if (skillData == null || defaultTargets == null || defaultTargets.Count == 0)
                return defaultTargets;
            
            if (skillData.TargetType == TargetType.All)
                return defaultTargets;
            
            if (skillData.TargetType == TargetType.None && !isChained)
                return defaultTargets;
            
            Entity firstTarget = defaultTargets[0];
            return firstTarget != null ? new List<Entity> { firstTarget } : defaultTargets;
        }
        
    }
}
