using System.Collections.Generic;
using PSB.Code.BattleCode.Skills.Interfaces;
using CIW.Code;
using Code.Scripts.Entities;
using UnityEngine;
using Work.YIS.Code.Buffs;
using YIS.Code.UI;
using YIS.Code.Defines;
using YIS.Code.Modules;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;
using YIS.Code.Skills.Interfaces;
using PSB.Code.BattleCode.Skills;

namespace PSB.Code.BattleCode.Players
{
    public class PreviewDamageCalculator : MonoBehaviour, IModule
    {
        private Entity _owner;
        private EntityDamageCalcModule _dmgCalcModule;
        private BuffModule _buffModule;
        
        public delegate void BuffProcessor(ref float skillDamage, 
            ref float simulatedAtkBuff, ref float finalMultiplier, float buffValue);
        
        private readonly Dictionary<int, BuffProcessor> _buffProcessors = new();

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Entity;
            if (_owner != null)
            {
                _dmgCalcModule = _owner.GetModule<EntityDamageCalcModule>();
                _buffModule = _owner.GetModule<BuffModule>();

                Debug.Assert(_dmgCalcModule != null, $"[PreviewDamageCalculator] {_owner.name}에 EntityDamageCalcModule이 없습니다.");
            }
            InitBuffProcessors();
        }

        private void InitBuffProcessors()
        {
            BuffProcessor addAtkBuff = (ref float _, ref float atkBuff, 
                ref float _, float value) => atkBuff += value;
            
            _buffProcessors[(int)BuffType.ATTACK_BUFF] = addAtkBuff;
            _buffProcessors[(int)BuffType.VITALITY_BUFF] = addAtkBuff;
        }

        public (float curDmg, float accDmg) CalculatePreviewForTarget(Entity target, 
            SkillDataSO[] skills, bool[] setIndex, int previewSlotIndex, bool[] isHitBySlot = null)
        {
            if (skills == null || setIndex == null || target == null) 
                return (0f, 0f);

            float curDmg = 0;
            float accDmg = 0;

            var slotVirtualBuffs = new Dictionary<int, float>[skills.Length];
            var slotVirtualEnchants = new Elemental[skills.Length];
            var activeRealBuffs = new Dictionary<int, float>();

            bool hasBuffModule = _buffModule != null;
            Elemental baseEnchant = Elemental.None;

            if (hasBuffModule)
            {
                foreach(var buffInfo in _buffModule.GetRawActiveBuffs())
                    activeRealBuffs[buffInfo.BuffKey] = buffInfo.Value;
                
                _buffModule.TryGetElementalOverrideOrImmediately(out baseEnchant);
            }

            var comps = new BaseSkill[skills.Length];
            for (int i = 0; i < skills.Length; i++)
            {
                slotVirtualBuffs[i] = new Dictionary<int, float>();
                slotVirtualEnchants[i] = baseEnchant; 

                if (skills[i] != null && setIndex[i])
                    comps[i] = GetSkillComponent(skills[i]);
            }

            for (int i = 0; i < skills.Length; i++)
            {
                if (comps[i] == null) continue;

                bool isBuff = comps[i] is IBuffOrDeBuffSkill;
                bool isEnchantProvider = comps[i] is IEnchantProvider;
                if (!isBuff && !isEnchantProvider) continue;

                bool isChainedLeft = CheckIsChained(i, true, skills, comps);
                bool isChainedRight = CheckIsChained(i, false, skills, comps);

                bool isChained = isChainedRight || isChainedLeft;

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

                if (isBuff)
                {
                    var actions = comps[i].SimulateSkill(isChained, _owner, new List<Entity> { target });
                    if (actions != null)
                    {
                        foreach (var action in actions)
                        {
                            if (action is BuffSkillAction ba)
                            {
                                if (isChainedLeft) AddBuffToSlot(slotVirtualBuffs, i - 1, (int)ba.BuffType, ba.Value);
                                if (isChainedRight) AddBuffToSlot(slotVirtualBuffs, i + 1, (int)ba.BuffType, ba.Value);
                                AddBuffToSlot(slotVirtualBuffs, i, (int)ba.BuffType, ba.Value);
                            }
                        }
                    }
                }
            }

            var targetStat = target.GetModule<EntityStat>();
            for (int i = 0; i < skills.Length; i++)
            {
                if (comps[i] == null) continue;

                bool isChainedLeft = CheckIsChained(i, true, skills, comps);
                bool isChainedRight = CheckIsChained(i, false, skills, comps);
                
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

                float simulatedAtkBuff = 0f;
                float finalMultiplier = 1f;

                foreach (var kvp in slotVirtualBuffs[i])
                {
                    float alreadyActiveValue = activeRealBuffs.GetValueOrDefault(kvp.Key, 0f);
                    float effectiveValue = Mathf.Max(0, kvp.Value - alreadyActiveValue);

                    if (_buffProcessors.TryGetValue(kvp.Key, out var processor))
                        processor?.Invoke(ref baseDamage, ref simulatedAtkBuff, ref finalMultiplier, effectiveValue);
                }

                int finalDamage = _dmgCalcModule != null && targetStat != null
                    ? _dmgCalcModule.DamageCalc(new DamageData { Damage = baseDamage, ElementalType = slotVirtualEnchants[i] }, 
                        targetStat, simulatedAtkBuff) 
                    : Mathf.RoundToInt(baseDamage);

                finalDamage = Mathf.RoundToInt(finalDamage * finalMultiplier);

                if (i == previewSlotIndex) curDmg += finalDamage;
                else accDmg += finalDamage;
            }
            return (curDmg, accDmg);
        }

        private bool CheckIsChained(int index, bool checkLeft, SkillDataSO[] skills, BaseSkill[] comps)
        {
            if (checkLeft)
            {
                return index > 0 && comps[index - 1] != null && 
                       skills[index].CanChainCheck(comps[index - 1]) == true && 
                       skills[index].checkSkillType != CheckType.Next;
            }
            else
            {
                return index < skills.Length - 1 && comps[index + 1] != null && 
                       skills[index + 1].CanChainCheck(comps[index]) == true && 
                       skills[index].checkSkillType != CheckType.Previous;
            }
        }

        private void AddBuffToSlot(Dictionary<int, float>[] slotVirtualBuffs, int slotIdx, int buffType, float value)
        {
            slotVirtualBuffs[slotIdx][buffType] = Mathf.Max(slotVirtualBuffs[slotIdx].GetValueOrDefault(buffType, 0f), value);
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