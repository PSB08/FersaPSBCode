using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIW.Code;
using Code.Scripts.Entities;
using PSB.Code.BattleCode.Skills;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Players;
using PSW.Code.EventBus;
using UnityEngine;
using Work.YIS.Code.Skills;
using YIS.Code.Combat;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Skills;
using YIS.Code.CoreSystem.Skills;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public sealed class EnemySkillExecutor : ISkillUseResultExecutor
    {
        private readonly BattleEnemy _battleEnemy;
        private readonly EntityStat _enemyStat;
        private readonly StatSO _procChanceStat;
        
        private readonly EntitySkillSelector _selector = new();
        private readonly EntitySkillPlan _plan = new();
        private readonly EntitySkillUseGateway _skillUseGateway;
        
        public BtSkillUseResult LastUseResult { get; private set; } = BtSkillUseResult.Failed;
        public int PlannedIndex => _plan.Index;
        public SkillDataSO PlannedSo => _plan.SkillData;
        public bool HasPlanned => _plan.HasPlan;
        
        public EnemySkillExecutor(BattleEnemy battleEnemy, StatSO procChanceStat, BattleSkillUseService skillUseService)
        {
            _battleEnemy = battleEnemy;
            _procChanceStat = procChanceStat;
            _enemyStat = battleEnemy != null ? battleEnemy.GetModule<EntityStat>() : null;
            _skillUseGateway = new EntitySkillUseGateway(skillUseService);
        }
        
        public void SetAttackSkills(SkillDataSO[] skills)
        {
            _selector.SetSkills(skills);
            _plan.Clear();
        }
        
        public int PickRandomSkillIndex()
        {
            return _selector.PickRandom();
        }
        
        public int PickHighestDamageSkillIndexExcept(IReadOnlyList<int> excludedIndices)
        {
            return _selector.PickHighestDamageThenGrade(null, null, excludedIndices);
        }
        
        public bool CanSelectSkillIndex(int index)
        {
            return _selector.CanSelect(index);
        }
        
        public void SetAvailableSkillIndices(IReadOnlyList<int> availableIndices)
        {
            _selector.SetAvailableIndices(availableIndices);
        }
        
        public void ClearAvailableSkillIndices()
        {
            _selector.ClearAvailableIndices();
        }
        
        public bool TryGetSkillDataByIndex(int index, out SkillDataSO skillData)
        {
            return _selector.TryGetSkill(index, out skillData);
        }
        
        public bool IsDefenseSkillIndex(int index)
        {
            return IsDefenseSkill(_selector.GetSkill(index));
        }
        
        public bool PlanByIndex(int index, out SkillEnum id, out SkillDataSO skillData)
        {
            id = default;
            skillData = null;
            
            if (!_selector.CanSelect(index) || 
                !_selector.TryGetSkill(index, out skillData) || !_plan.TrySet(index, skillData))
            {
                return false;
            }
            
            id = _plan.Id;
            return true;
        }
        
        public bool TryGetPlannedSkillIds(out SkillEnum[] ids)
        {
            return _plan.TryGetSkillIds(out ids);
        }
        
        public void ClearPlan()
        {
            _plan.Clear();
        }
        
        public async Task<BtSkillUseResult> ExecutePlannedSkillAsync(Entity directTarget, 
            SkillTargetCandidates candidates, bool canActivateChain)
        {
            LastUseResult = BtSkillUseResult.Failed;
            
            SkillDataSO skillData = _plan.SkillData;
            if (_battleEnemy == null || _battleEnemy.IsDead || skillData == null || !_skillUseGateway.IsAvailable)
            {
                return LastUseResult;
            }
            
            if (skillData.targetType != TargetType.None && candidates.IsEmpty)
            {
                LastUseResult = BtSkillUseResult.NoTarget;
                return LastUseResult;
            }
            
            if (!CanStartTargetedSkill(skillData, directTarget, candidates))
            {
                LastUseResult = BtSkillUseResult.BlockedAfterTry;
                return LastUseResult;
            }
            
            _battleEnemy.NotifyMechanicSkillStarted(skillData);
            
            try
            {
                SkillUseResult result = await _skillUseGateway.UseAsync(_battleEnemy, skillData, 
                    directTarget, candidates, canActivateChain);
                
                LastUseResult = EntitySkillUseGateway.ToBtResult(result);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, _battleEnemy);
                LastUseResult = BtSkillUseResult.Failed;
            }
            finally
            {
                _battleEnemy.NotifyMechanicSkillFinished(skillData, LastUseResult);
            }
            
            return LastUseResult;
        }
        
        public void SetLastUseResult(BtSkillUseResult result)
        {
            LastUseResult = result;
        }
        
        public bool HasDefenseSkill()
        {
            return PickBestDefenseSkillIndex() >= 0;
        }
        
        public bool HasSpecialSkill()
        {
            return PickBestSpecialSkillIndex() >= 0;
        }
        
        public int PickBestDefenseSkillIndex()
        {
            return _selector.PickHighestDamageThenGrade(IsDefenseSkill);
        }
        
        public int PickBestSpecialSkillIndex()
        {
            return _selector.PickHighestDamageThenGrade(IsSpecialSkill);
        }
        
        public int PickBestActionSkillIndex()
        {
            return PickBestActionSkillIndexExcept(null);
        }
        
        public int PickBestActionSkillIndexExcept(IReadOnlyList<int> excludedIndices)
        {
            return _selector.PickHighestDamageThenGrade(skillData => !IsDefenseSkill(skillData), null, excludedIndices);
        }
        
        public int PickBestDrawSkillIndex()
        {
            return PickBestSpecialSkillIndex();
        }
        
        public int PickBestCostRecoverySkillIndex()
        {
            return PickBestSpecialSkillIndex();
        }
        
        private static bool IsAttackSkill(SkillDataSO skillData)
        {
            return skillData != null && skillData.skillCategory == SkillCategory.Attack;
        }
        
        private static bool IsDefenseSkill(SkillDataSO skillData)
        {
            return skillData != null && skillData.skillCategory == SkillCategory.Defense;
        }
        
        private static bool IsSpecialSkill(SkillDataSO skillData)
        {
            return skillData != null && skillData.skillCategory == SkillCategory.Special;
        }
        
        private static bool CanStartTargetedSkill(SkillDataSO skillData, Entity directTarget, 
            SkillTargetCandidates candidates)
        {
            if (skillData == null || skillData.targetType == TargetType.None)
                return true;
            
            return skillData.targetType switch
            {
                TargetType.Direct => ContainsCandidate(candidates, directTarget) && CanBeActiveDirectTarget(directTarget, true),
                TargetType.Random or TargetType.All => HasRangeTargetCandidate(candidates), 
                _ => false
            };
        }
        
        private static bool ContainsCandidate(SkillTargetCandidates candidates, Entity target)
        {
            if (target == null)
                return false;
            
            if (candidates.Single == target)
                return true;
            
            if (candidates.Multiple == null)
                return false;
            
            foreach (Entity candidate in candidates.Multiple)
            {
                if (candidate == target)
                    return true;
            }
            
            return false;
        }
        
        private static bool HasRangeTargetCandidate(SkillTargetCandidates candidates)
        {
            if (CanBeActiveRangeTarget(candidates.Single))
                return true;
            
            if (candidates.Multiple == null)
                return false;
            
            foreach (Entity candidate in candidates.Multiple)
            {
                if (CanBeActiveRangeTarget(candidate))
                    return true;
            }
            
            return false;
        }
        
        private static bool CanBeActiveDirectTarget(Entity target, bool logBlockedTarget)
        {
            return target != null &&
                   target.gameObject.activeInHierarchy &&
                   SkillTargetingUtil.CanBeDirectTarget(target, logBlockedTarget);
        }
        
        private static bool CanBeActiveRangeTarget(Entity target)
        {
            return target != null &&
                   target.gameObject.activeInHierarchy &&
                   SkillTargetingUtil.CanBeRangeTarget(target, false);
        }
        
        private static void RaiseEvade(Entity target)
        {
            if (target == null)
                return;
            
            DamageData evadeData = new(0f, Elemental.Normal)
            {
                Info = "회피!"
            };
            
            Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(target.transform.position, evadeData));
            Bus<EvadeEvent>.Raise(new EvadeEvent(null, target));
        }
        
    }
}
