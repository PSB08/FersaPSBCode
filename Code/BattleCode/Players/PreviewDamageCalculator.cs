using System.Collections.Generic;
using PSB.Code.BattleCode.Skills.Interfaces;
using CIW.Code;
using Code.Scripts.Entities;
using UnityEngine;
using YIS.Code.Combat;
using YIS.Code.Defines;
using YIS.Code.Modules;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;
using YIS.Code.Skills.Interfaces;

namespace PSB.Code.BattleCode.Players
{
    public class PreviewDamageCalculator : MonoBehaviour, IModule
    {
        private Entity _owner;
        private EntityDamageCalcModule _dmgCalcModule;
        private BuffModule _buffModule;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Entity;
            if (_owner != null)
            {
                _dmgCalcModule = _owner.GetModule<EntityDamageCalcModule>();
                _buffModule = _owner.GetModule<BuffModule>();

                Debug.Assert(_dmgCalcModule != null, $"[PreviewDamageCalculator] {_owner.name}에 EntityDamageCalcModule이 없습니다.");
            }
        }

        public (float curDmg, float accDmg) CalculatePreviewForTarget(Entity target, 
            SkillDataSO[] skills, bool[] setIndex, int previewSlotIndex, bool[] isHitBySlot = null)
        {
            if (skills == null || setIndex == null || target == null) 
                return (0f, 0f);

            float curDmg = 0;
            float accDmg = 0;

            var slotVirtualEnchants = new Elemental[skills.Length];
            Elemental baseEnchant = Elemental.None;

            if (_buffModule != null)
            {
                _buffModule.TryGetElementalOverrideOrImmediately(out baseEnchant);
            }

            var comps = new BaseSkill[skills.Length];
            for (int i = 0; i < skills.Length; i++)
            {
                slotVirtualEnchants[i] = baseEnchant; 

                if (skills[i] != null && setIndex[i])
                    comps[i] = GetSkillComponent(skills[i]);
            }

            int validSkillCount = 0;
            int chainedSkillCount = 0;

            bool[] isChainedLefts = new bool[skills.Length];
            bool[] isChainedRights = new bool[skills.Length];

            for (int i = 0; i < skills.Length; i++)
            {
                if (comps[i] != null)
                {
                    validSkillCount++;

                    isChainedLefts[i] = CheckIsChained(i, true, skills, comps);
                    isChainedRights[i] = CheckIsChained(i, false, skills, comps);

                    if (isChainedLefts[i] || isChainedRights[i]) chainedSkillCount++;
                }
            }

            bool isAllChained = (validSkillCount > 1) && (validSkillCount == chainedSkillCount);

            for (int i = 0; i < skills.Length; i++)
            {
                if (comps[i] == null) continue;

                bool isEnchantProvider = comps[i] is IEnchantProvider;
                if (!isEnchantProvider) continue;

                bool isChainedLeft = isChainedLefts[i];
                bool isChainedRight = isChainedRights[i];

                if (isEnchantProvider)
                {
                    Elemental provideElement = skills[i].elemental;

                    if (comps[i] is IEnchantable) 
                        slotVirtualEnchants[i] = provideElement;

                    if (isChainedLeft && comps[i - 1] is IEnchantable)
                    {
                        slotVirtualEnchants[i - 1] = provideElement;
                    }

                    if (isChainedRight && comps[i + 1] is IEnchantable)
                    {
                        slotVirtualEnchants[i + 1] = provideElement;
                    }
                }
            }   

            var targetStat = target.GetModule<EntityStat>();
            for (int i = 0; i < skills.Length; i++)
            {
                if (comps[i] == null) continue;

                bool isChainedLeft = isChainedLefts[i];
                bool isChainedRight = isChainedRights[i];
                bool isChained = isChainedRight || isChainedLeft;
                
                bool actuallyProvidedEnchant = false;
                
                if (comps[i] is IEnchantProvider)
                {
                    bool canEnchantLeft = isChainedLeft && comps[i - 1] is IEnchantable;
                    bool canEnchantRight = isChainedRight && comps[i + 1] is IEnchantable;
        
                    if (canEnchantLeft || canEnchantRight)
                    {
                        actuallyProvidedEnchant = true; 
                    }
                }

                bool isAttack = (comps[i] is IAttackSkill) || 
                                (comps[i] is IEnchantProvider && !actuallyProvidedEnchant) ||
                                (!isChained);
                
                if (!isAttack) continue;

                if (isHitBySlot != null && !isHitBySlot[i]) 
                {
                    continue; 
                }

                float baseDamage = 0f;
                var actions = comps[i].SimulateSkill(isChained, _owner, new List<Entity> { target });
                if (actions != null)
                {
                    foreach (var action in actions)
                        if (action is DamageSkillAction da) baseDamage += da.CurrentDamageData.Damage;
                }
                
                if (baseDamage <= 0 && skills[i].damage > 0) baseDamage = skills[i].damage;
                if (baseDamage <= 0) continue;

                float finalMultiplier = 1f;

                if (isAllChained)
                    finalMultiplier = EntityDamageCalcModule.ALL_CHAIN_BONUS_MULTIPLIER;

                float previewBaseDamage = baseDamage * finalMultiplier;

                int finalDamage = _dmgCalcModule != null && targetStat != null
                    ? _dmgCalcModule.DamageCalc(new DamageData { Damage = previewBaseDamage, ElementalType = slotVirtualEnchants[i] }, 
                        targetStat, 0f, isPreview: true) 
                    : Mathf.RoundToInt(previewBaseDamage);

                if (i == previewSlotIndex) curDmg += finalDamage;
                else accDmg += finalDamage;
            }
            return (curDmg, accDmg);
        }

        private bool CheckIsChained(int index, bool checkLeft, SkillDataSO[] skills, BaseSkill[] comps)
        {
            if (checkLeft)
            {
                if (index <= 0 || comps[index - 1] == null) return false;

                if (string.IsNullOrEmpty(skills[index].checkAttributeName)) 
                    return false;

                return skills[index].CanChainCheck(comps[index - 1]) == true && 
                       skills[index].checkSkillType != CheckType.Next;
            }
            else
            {
                if (index >= skills.Length - 1 || comps[index + 1] == null) return false;

                if (string.IsNullOrEmpty(skills[index + 1].checkAttributeName)) 
                    return false;

                return skills[index + 1].CanChainCheck(comps[index]) == true && 
                       skills[index].checkSkillType != CheckType.Previous;
            }
        }

        private BaseSkill GetSkillComponent(SkillDataSO skillData)
        {
            if (skillData == null || skillData.skillPrefab == null) return null;
            
            BaseSkill skillComp = skillData.skillPrefab.GetComponent<BaseSkill>();
            if (skillComp != null)
            {
                skillComp.SetData(skillData); 
                skillComp.Initialize();
            }
            return skillComp;
        }
        
    }
}