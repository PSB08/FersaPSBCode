using System;
using UnityEngine;
using PSB.Code.BattleCode.Entities;
using Work.YIS.Code.Skills;
using YIS.Code.CoreSystem.Skills;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Allies.AttackCode
{
    public sealed class AllyAiSettings
    {
        public int MaxActionsPerTurn = 10;
        public float DefenseUseChance = 0.5f;
        public float DangerHealthRatio = 0.5f;
    }
    
    public sealed class AllyTurnBrain
    {
        private readonly AllyTurnPlanner _planner = new();
        
        private BattleAlly _ally;
        private AllyAttack _attack;
        private AllySkillExecutor _skills;
        private AllyAiSettings _settings;
        private Action<string> _log;
        private BattleSkillSystem _battleSkillSystem;
        private SkillCostModule _costModule;
        
        private bool _skip;
        private bool _prepared;
        private bool _defenseAllowed;
        private bool _defenseConsidered;
        private int _completedActions;
        
        public bool HasSkip => _skip;
        public bool HasPlan => (_skills != null && _skills.HasPlanned) || _skip;
        public int PlannedIndex => _skills != null ? _skills.PlannedIndex : -1;
        public int CurrentCost => _costModule != null ? _costModule.CurrentCost : 0;
        public int MaxCost => GetMaxCost();
        
        public void Bind(BattleAlly ally, AllyAttack attack, AllySkillExecutor skills,
            BattleSkillSystem battleSkillSystem, AllyAiSettings settings, Action<string> log)
        {
            _ally = ally;
            _attack = attack;
            _skills = skills;
            _battleSkillSystem = battleSkillSystem;
            _settings = settings ?? new AllyAiSettings();
            _log = log;
            _costModule = ally != null ? ally.GetModule<SkillCostModule>() : null;
        }
        
        public void SetSkills(SkillDataSO[] skills)
        {
            ResetTurn();
        }
        
        public bool Prepare()
        {
            if (_ally == null || _ally.IsDead) return false;
            if (_skills == null) return false;
            if (_ally.allySO == null)
            {
                Log("AllySO가 없어 턴을 준비할 수 없습니다.");
                return false;
            }
            
            if (_costModule == null)
            {
                Log("SkillCostModule이 없어 턴을 준비할 수 없습니다.");
                return false;
            }
            
            if (_skills.HasPlanned || _skip) return true;
            
            if (!_prepared)
                StartTurn();
            else
                RefreshUsableSkills();
            
            Log($"행동 계획 시작 : 코스트 = {CurrentCost}/{GetMaxCost()}, 선택 기준 = 보유 스킬 전체");
            return true;
        }
        
        public bool PlanNext(bool allowSkip)
        {
            if (HasPlan) return true;
            
            if (!Prepare()) return false;
            
            if (PlanDefenseIntent()) return true;
            
            if (PlanSkillIntent()) return true;
            
            Log($"행동 계획 실패 : 사용 가능한 스킬이 없습니다. 코스트 = {CurrentCost}");
            return allowSkip && PlanSkip();
        }
        
        public bool PlanSkillIntent()
        {
            if (!CanPlan()) return false;
            
            int index = _planner.SelectRandomSkill(_skills, CanPaySkillCost);
            return PlanSkill(index, "무작위 스킬");
        }
        
        public bool PlanDefenseIntent()
        {
            if (_defenseConsidered || !CanPlan())
                return false;
            
            _defenseConsidered = true;
            
            if (!_defenseAllowed || !IsDangerous())
                return false;
            
            int index = _skills.PickBestDefenseSkillIndex();
            return PlanSkill(index, "수비");
        }
        
        public bool PlanSkip()
        {
            _skip = true;
            _skills?.ClearPlan();
            Log("선택 가능한 행동이 없어 스킵합니다.");
            return true;
        }
        
        public bool TryGetPlannedSkillIds(out SkillEnum[] ids, out bool[] chainFlags)
        {
            ids = null;
            chainFlags = null;
            
            if (_skip || _skills == null) return false;
            
            if (!_skills.TryGetPlannedSkillIds(out ids)) return false;
            
            chainFlags = new bool[ids.Length];
            return ids.Length > 0;
        }
        
        public void CompleteAction()
        {
            if (_skills == null || !_skills.HasPlanned || _skip) return;
            
            int index = _skills.PlannedIndex;
            string skillText = BuildSkillText(index, _skills.PlannedSo);
            int beforeCost = CurrentCost;
            
            _completedActions++;
            
            RefreshUsableSkills();
            Log($"행동 완료 : {skillText}, 코스트 {beforeCost}->{CurrentCost}");
        }
        
        public void ClearPlan()
        {
            _skip = false;
            _skills?.ClearPlan();
        }
        
        public void EndTurn()
        {
            _battleSkillSystem?.ResetChain(_ally);
            Log($"턴 정리 : 남은 코스트 = {CurrentCost}");
            ResetTurn();
        }
        
        public void ResetTurn()
        {
            _skip = false;
            _prepared = false;
            _defenseAllowed = false;
            _defenseConsidered = false;
            _completedActions = 0;
            
            _skills?.ClearAvailableSkillIndices();
            _skills?.ClearPlan();
        }
        
        public int GetSkillCost(int skillIndex)
        {
            if (_skills != null && _skills.TryGetSkillDataByIndex(skillIndex, out SkillDataSO skillData) && 
                skillData != null)
            {
                return Mathf.Max(0, skillData.cost);
            }
            
            return -1;
        }
        
        public float EstimateCurrentDamage(SkillDataSO[] skills)
        {
            if (_skills != null && _skills.HasPlanned)
                return Mathf.Max(0f, _skills.PlannedSo != null ? _skills.PlannedSo.damage : 0f);
            
            float bestDamage = 0f;
            if (skills == null)
                return bestDamage;
            
            for (int i = 0; i < skills.Length; i++)
            {
                SkillDataSO skill = skills[i];
                if (skill == null)
                    continue;
                
                if (_prepared && !CanPaySkillCost(i))
                    continue;
                
                bestDamage = Mathf.Max(bestDamage, skill.damage);
            }
            
            return Mathf.Max(0f, bestDamage);
        }
        
        public string BuildPlannedText()
        {
            if (_skills == null || !_skills.HasPlanned)
                return "없음";
            
            return BuildSkillText(_skills.PlannedIndex, _skills.PlannedSo);
        }
        
        private void StartTurn()
        {
            _prepared = true;
            int maxCost = GetMaxCost();
            _costModule.SetMaxCost(maxCost);
            _costModule.AddCost(maxCost);
            
            _defenseAllowed = _skills.HasDefenseSkill() &&
                              EstimateIncomingDamage() > 0f &&
                              UnityEngine.Random.value < Mathf.Clamp01(_settings.DefenseUseChance);
            
            RefreshUsableSkills();
            Log($"턴 시작 : 코스트 = {CurrentCost}/{GetMaxCost()}, 선택 방식 = 보유 스킬 전체 중 최고 피해");
        }
        
        private void RefreshUsableSkills()
        {
            _skills?.ClearAvailableSkillIndices();
        }
        
        private bool CanPlan()
        {
            return _ally != null && _skills != null && !_skip &&
                   _completedActions < Mathf.Max(1, _settings.MaxActionsPerTurn) && !_skills.HasPlanned;
        }
        
        private bool IsDangerous()
        {
            float damage = EstimateIncomingDamage();
            if (damage <= 0f || _ally == null)
                return false;
            
            EntityHealth health = _ally.GetModule<EntityHealth>();
            if (health == null)
                return false;
            
            return damage >= health.CurrentHealth ||
                   damage >= health.MaxHealth * Mathf.Clamp01(_settings.DangerHealthRatio);
        }
        
        private float EstimateIncomingDamage()
        {
            return _attack != null ? _attack.EstimateIncomingDamage() : 0f;
        }
        
        private bool PlanSkill(int index, string source)
        {
            if (index < 0)
                return false;
            
            if (!_skills.CanSelectSkillIndex(index) || !CanPaySkillCost(index))
            {
                Log($"{source} 후보 제외 : 번호 = {index}, 코스트 = {CurrentCost}");
                return false;
            }
            
            if (!_skills.PlanByIndex(index, out SkillEnum id, out SkillDataSO skill))
            {
                Log($"{source} 계획 실패 : 번호 = {index}");
                return false;
            }
            
            _skip = false;
            
            Log($"{source} 선택 : {BuildSkillText(index, skill)}, 코스트 = {CurrentCost}/{GetMaxCost()}");
            return true;
        }
        
        private bool CanPaySkillCost(int skillIndex)
        {
            int cost = GetSkillCost(skillIndex);
            return cost >= 0 && _costModule != null && _costModule.CanPay(cost);
        }
        
        private int GetMaxCost()
        {
            return _ally != null && _ally.allySO != null ? Mathf.Max(0, _ally.allySO.maxCost) : 0;
        }
        
        private string BuildSkillText(int skillIndex, SkillDataSO skillData)
        {
            if (skillData == null)
                return $"번호 = {skillIndex}, 비어 있음";

            return $"번호 = {skillIndex}, {skillData.skillName}, 등급 = {skillData.grade}, " +
                   $"피해량 = {skillData.damage}, 코스트 = {GetSkillCost(skillIndex)}";
        }
        
        private void Log(string message)
        {
            _log?.Invoke(message);
        }
    }
    
    public sealed class AllyTurnPlanner
    {
        public int SelectRandomSkill(AllySkillExecutor skillExecutor, Func<int, bool> canUse)
        {
            return skillExecutor != null ? skillExecutor.PickRandomUsableSkillIndex(canUse) : -1;
        }
    }
    
}
