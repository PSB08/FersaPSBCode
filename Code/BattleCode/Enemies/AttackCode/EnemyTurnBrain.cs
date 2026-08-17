using System;
using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSB.Code.BattleCode.Enemies.Mechanics;
using PSB.Code.BattleCode.Enemies.Mechanics.Intents;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Players;
using PSB.Code.BattleCode.UIs;
using PSW.Code.EventBus;
using UnityEngine;
using Work.YIS.Code.Skills;
using YIS.Code.CoreSystem.Skills;
using YIS.Code.Modules;
using YIS.Code.Skills;
using Object = UnityEngine.Object;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public enum EnemyPlanStep
    {
        Defense,
        Draw,
        CostRecovery,
        Pattern,
        BestAction
    }
    
    public sealed class EnemyAiSettings
    {
        public float DangerHealthRatio = 0.5f;
        public float DefenseUseChance = 0.5f;
    }
    
    public class EnemyTurnBrain
    {
        private readonly EnemySkillHand _hand = new();
        private readonly EnemyTurnPlanner _planner = new();
        private readonly List<SkillDataSO> _playerSkills = new();
        private readonly List<int> _turnExcludedSkillIndices = new();
        
        private BattleEnemy _enemy;
        private EnemyAttack _attack;
        private EnemySkillExecutor _skills;
        private EnemyAiSettings _settings;
        private Action<string> _log;
        private BattleSkillSystem _battleSkillSystem;
        private SkillCostModule _costModule;
        
        private bool _skip;
        private bool _prepared;
        private bool _useDefenseThisBattle;
        private bool _defenseDecisionMade;
        private bool _defenseConsidered;
        private bool _mechanicIntentUsed;
        
        public bool HasSkip => _skip;
        public bool HasPlan => (_skills != null && _skills.HasPlanned) || _skip;
        public int PlannedIndex => _skills != null ? _skills.PlannedIndex : -1;
        public int CurrentCost => _costModule != null ? _costModule.CurrentCost : 0;
        public int MaxCost => GetMaxCost();
        
        public void Bind(BattleEnemy enemy, EnemyAttack attack, EnemySkillExecutor skills,
            BattleSkillSystem battleSkillSystem, EnemyAiSettings settings, Action<string> log)
        {
            _enemy = enemy;
            _attack = attack;
            _skills = skills;
            _battleSkillSystem = battleSkillSystem;
            _settings = settings ?? new EnemyAiSettings();
            _log = log;
            _costModule = enemy != null ? enemy.GetModule<SkillCostModule>() : null;
        }
        
        public void BeginBattle(SkillDataSO[] skills)
        {
            DecideDefenseForBattle();
            SetSkills(skills);
        }
        
        public void SetSkills(SkillDataSO[] skills)
        {
            if (!_defenseDecisionMade)
                DecideDefenseForBattle();
            
            _hand.Reset(skills, _useDefenseThisBattle);
            
            ResetTurn();
            
            if (_enemy != null)
            {
                Bus<EnemyIntentClearedEvent>.Raise(new EnemyIntentClearedEvent(_enemy));
                Bus<EnemySkillsChangedEvent>.Raise(new EnemySkillsChangedEvent(_enemy, skills));
            }
        }
        
        public void SetPlayerSkills(IReadOnlyList<SkillDataSO> skills)
        {
            _playerSkills.Clear();
            
            if (skills == null)
                return;
            
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null)
                    _playerSkills.Add(skills[i]);
            }
        }
        
        public bool Prepare()
        {
            if (_enemy == null || _enemy.IsDead) return false;
            if (_skills == null) return false;
            if (_enemy.enemySO == null)
            {
                Log("EnemySO가 없어 턴을 준비할 수 없습니다.");
                return false;
            }
            
            if (_costModule == null)
            {
                Log("SkillCostModule이 없어 턴을 준비할 수 없습니다.");
                return false;
            }
            
            if (_skills.HasPlanned || _skip)
                return true;
            
            if (!_prepared)
                StartTurn();
            else
                RefreshUsableSkills();
            
            Log($"행동 계획 시작 : 코스트 = {CurrentCost}/{GetMaxCost()}, 손패 = [{BuildHandText()}]");
            return true;
        }
        
        public bool PlanNext(bool allowSkip)
        {
            if (HasPlan) return true;
            
            if (!Prepare()) return false;
            
            if (PlanStep(EnemyPlanStep.Defense)) return true;
            if (PlanStep(EnemyPlanStep.Pattern)) return true;
            if (PlanStep(EnemyPlanStep.BestAction)) return true;
            
            Log($"행동 계획 실패 : 사용 가능 스킬 없음, 코스트 = {CurrentCost}, 손패 = [{BuildHandText()}]");
            return allowSkip && PlanSkip();
        }
        
        public bool PlanStep(EnemyPlanStep step)
        {
            if (!CanPlan()) return false;
            
            switch (step)
            {
                case EnemyPlanStep.Defense:
                    if (_defenseConsidered)
                        return false;
                    _defenseConsidered = true;
                    return PlanSkill(_planner.SelectDefenseSkill(_skills, _useDefenseThisBattle, IsDangerous()), "수비");
                case EnemyPlanStep.Draw:
                    return PlanSkill(_planner.SelectDrawSkill(_skills, CanDrawMore()), "드로우");
                case EnemyPlanStep.CostRecovery:
                    return PlanSkill(_planner.SelectCostRecoverySkill(_skills, CurrentCost < GetMaxCost()), "코스트 회복");
                case EnemyPlanStep.Pattern:
                    return PlanPattern();
                case EnemyPlanStep.BestAction:
                    return PlanSkill(_planner.SelectBestActionSkill(
                        _skills, _turnExcludedSkillIndices), "최강 행동");
                default:
                    return false;
            }
        }
        
        public bool PlanSkip()
        {
            _skip = true;
            _skills?.ClearPlan();
            
            Log("행동 스킵 계획 : 사용할 수 있는 스킬/코스트/손패가 없습니다.");
            
            if (_enemy != null)
                Bus<EnemyIntentClearedEvent>.Raise(new EnemyIntentClearedEvent(_enemy));
            
            return true;
        }
        
        public bool TrySelectTarget(PlayerManager playerManager, BattleAllyManager allyManager,
            out Transform target, out string reason)
        {
            target = null;
            reason = string.Empty;
            
            if (!EnemyTargetSelector.TrySelectTarget(playerManager, allyManager, CalcCurrentDamage(), out Entity selected, out reason))
                return false;
            
            target = selected != null ? selected.transform : null;
            return target != null;
        }
        
        public void CompleteAction()
        {
            if (_skills == null || !_skills.HasPlanned || _skip)
                return;
            
            int index = _skills.PlannedIndex;
            string skillText = BuildSkillText(index);
            int beforeCost = CurrentCost;
            
            _hand.Consume(index);
            RefreshUsableSkills();
            
            Log($"행동 완료 : {skillText}, 코스트 {beforeCost}->{CurrentCost}, 남은 손패 = [{BuildHandText()}]");
        }
        
        public void ClearPlan()
        {
            _skip = false;
            _skills?.ClearPlan();
            
            if (_enemy != null)
                Bus<EnemyIntentClearedEvent>.Raise(new EnemyIntentClearedEvent(_enemy));
        }
        
        public void EndTurn()
        {
            _hand.DiscardHand();
            _battleSkillSystem?.ResetChain(_enemy);
            Log($"턴 종료 정리 : 남은 코스트 = {CurrentCost}, 손패 = [{BuildHandText()}]");
            ResetTurn();
        }
        
        public string BuildPlannedText()
        {
            if (_skills == null || !_skills.HasPlanned)
                return "없음";
            
            return BuildSkillText(_skills.PlannedIndex, _skills.PlannedSo);
        }
        
        public int GetSkillCost(int skillIndex)
        {
            if (_skills != null &&
                _skills.TryGetSkillDataByIndex(skillIndex, out SkillDataSO skillData) &&
                skillData != null)
            {
                return Mathf.Max(0, skillData.cost);
            }
            
            return -1;
        }
        
        public void WarnIfSkillLimitExceeded(SkillDataSO[] skills, Object context)
        {
            if (_enemy == null || _enemy.enemySO == null)
            {
                Debug.LogWarning("[Enemy AI] EnemySO가 없어 보유 스킬 수를 검증할 수 없습니다.", context);
                return;
            }
            
            if (!_enemy.enemySO.IsOverOwnedSkillLimit(skills, out int count, out int max))
                return;
            
            string enemyName = _enemy.enemySO.name;
            
            Debug.LogWarning($"[Enemy AI] {enemyName} : 보유 스킬 수가 제한을 넘었습니다. 현재 = {count}, 최대 = {max}", context);
        }
        
        private void StartTurn()
        {
            _prepared = true;
            
            int maxCost = GetMaxCost();
            _costModule.SetMaxCost(maxCost);
            _costModule.AddCost(maxCost);
            
            _hand.DrawToLimit(GetMaxHand());
            
            RefreshUsableSkills();
            
            Log($"턴 시작 : 코스트 = {CurrentCost}/{GetMaxCost()}, 손패 최대 = {GetMaxHand()}, " +
                $"손패 = [{BuildHandText()}], 전투 방어 사용 = {_useDefenseThisBattle}");
        }
        
        private void ResetTurn()
        {
            _skip = false;
            _prepared = false;
            _defenseConsidered = false;
            _mechanicIntentUsed = false;
            _turnExcludedSkillIndices.Clear();
            
            _hand.ClearAffordable();
            _skills?.ClearAvailableSkillIndices();
            _skills?.ClearPlan();
        }
        
        private bool CanPlan()
        {
            return _enemy != null && _skills != null && !_skip && !_skills.HasPlanned;
        }
        
        private bool PlanPattern()
        {
            return PlanMechanicIntent();
        }
        
        private bool PlanMechanicIntent()
        {
            if (_mechanicIntentUsed)
                return false;
            
            EnemyMechanicController controller =
                _enemy != null ? _enemy.MechanicController : null;
            
            if (controller == null || !controller.IsInitialized)
                return false;
            
            _mechanicIntentUsed = true;
            return controller.TryResolveIntent(TryAcceptMechanicProposal);
        }
        
        private EnemyIntentAcceptance TryAcceptMechanicProposal(EnemyIntentProposal proposal)
        {
            bool accepted;
            string source = proposal.Source != null
                ? proposal.Source.DisplayName
                : "기믹";

            AddExcludedSkillIndices(proposal.ExcludedSkills);
            
            switch (proposal.Decision)
            {
                case EnemyIntentDecision.SpecificSkill:
                    accepted = PlanSkill(FindSkillIndex(proposal.Skill), source);
                    break;
                
                case EnemyIntentDecision.BestSkill:
                    int selectedIndex = _planner.SelectBestActionSkill(
                        _skills, _turnExcludedSkillIndices);
                    
                    accepted = PlanSkill(selectedIndex, source);
                    break;
                
                case EnemyIntentDecision.Skip:
                    accepted = PlanSkip();
                    break;
                
                default:
                    accepted = false;
                    break;
            }
            
            if (accepted)
            {
                RaiseMechanicLog(proposal);
                return EnemyIntentAcceptance.Accepted;
            }
            
            if (proposal.Fallback == EnemyIntentFallback.Skip && PlanSkip())
            {
                RaiseMechanicLog(proposal);
                return EnemyIntentAcceptance.FallbackAccepted;
            }
            
            return EnemyIntentAcceptance.Rejected;
        }
        
        private int FindSkillIndex(SkillDataSO targetSkill)
        {
            if (targetSkill == null || _attack == null || _attack.CurrentSkills == null)
                return -1;
            
            SkillDataSO[] skills = _attack.CurrentSkills;
            
            for (int i = 0; i < skills.Length; i++)
            {
                if (ReferenceEquals(skills[i], targetSkill))
                    return i;
            }
            
            return -1;
        }
        
        private void AddExcludedSkillIndices(IReadOnlyList<SkillDataSO> excludedSkills)
        {
            if (excludedSkills == null)
                return;
            
            for (int i = 0; i < excludedSkills.Count; i++)
            {
                int index = FindSkillIndex(excludedSkills[i]);
                
                if (index >= 0 && !_turnExcludedSkillIndices.Contains(index))
                    _turnExcludedSkillIndices.Add(index);
            }
        }
        
        private void RaiseMechanicLog(EnemyIntentProposal proposal)
        {
            if (!proposal.HasLog || _enemy == null)
                return;
            
            string enemyName = SystemLogNameResolver.GetTargetName(
                _enemy, SystemLogOwner.Enemy);
            
            Bus<SystemLogEvent>.Raise(new SystemLogEvent(
                $"{enemyName}이(가) {proposal.LogMessage}", SystemLogOwner.Enemy));
        }
        
        private bool PlanSkill(int index, string source)
        {
            if (index < 0)
                return false;
            
            if (!_skills.CanSelectSkillIndex(index) || !CanPaySkillCost(index))
            {
                Log($"{source} 후보 제외 : 번호 = {index}, 코스트 = {CurrentCost}, 손패 = [{BuildHandText()}]");
                return false;
            }
            
            if (!_skills.PlanByIndex(index, out SkillEnum id, out SkillDataSO skill))
            {
                Log($"{source} 계획 실패 : 번호 = {index}");
                return false;
            }
            
            _skip = false;
            
            Log($"{source} 선택 : {BuildSkillText(index, skill)}, 코스트 = {CurrentCost}/{GetMaxCost()}");
            RaiseIntent(index, id, skill);
            return true;
        }
        
        private void RaiseIntent(int index, SkillEnum id, SkillDataSO skill)
        {
            if (_enemy != null)
                Bus<EnemyIntentPlannedEvent>.Raise(new EnemyIntentPlannedEvent(_enemy, index, id, skill));
        }
        
        private void DecideDefenseForBattle()
        {
            _useDefenseThisBattle = _skills != null && _skills.HasDefenseSkill() &&
                                    UnityEngine.Random.value < Mathf.Clamp01(_settings.DefenseUseChance);
            _defenseDecisionMade = true;
        }
        
        private bool IsDangerous()
        {
            float damage = CalcIncomingDamage();
            if (damage <= 0f || _enemy == null)
                return false;
            
            EntityHealth health = _enemy.GetModule<EntityHealth>();
            if (health == null)
                return false;
            
            return damage >= health.CurrentHealth || damage >= health.MaxHealth * GetDangerRatio();
        }
        
        private float CalcIncomingDamage()
        {
            return _attack != null ? _attack.EstimateIncomingDamage(_playerSkills) : 0f;
        }
        
        private float CalcCurrentDamage()
        {
            if (_skills != null && _skills.HasPlanned)
                return Mathf.Max(0f, _skills.PlannedSo != null ? _skills.PlannedSo.damage : 0f);
            
            float best = 0f;
            IReadOnlyList<int> hand = _hand.AffordableHandSkillIndices;
            
            foreach (var s in hand)
            {
                if (_skills != null && _skills.TryGetSkillDataByIndex(s, out SkillDataSO skill) && skill != null)
                    best = Mathf.Max(best, skill.damage);
            }
            
            return Mathf.Max(0f, best);
        }
        
        private bool CanDrawMore()
        {
            return _hand.CanGainMoreSkills(GetMaxHand());
        }
        
        private void RefreshUsableSkills()
        {
            _hand.RefreshAffordable(CanPaySkillCost);
            _skills?.SetAvailableSkillIndices(_hand.AffordableHandSkillIndices);
        }
        
        private bool CanPaySkillCost(int skillIndex)
        {
            int cost = GetSkillCost(skillIndex);
            return cost >= 0 && _costModule != null && _costModule.CanPay(cost);
        }
        
        private int GetMaxCost()
        {
            return _enemy != null && _enemy.enemySO != null ? Mathf.Max(0, _enemy.enemySO.maxCost) : 0;
        }
        
        private int GetMaxHand()
        {
            return _enemy != null && _enemy.enemySO != null ? Mathf.Max(0, _enemy.enemySO.maxHandSkillCount) : 0;
        }
        
        private float GetDangerRatio()
        {
            return Mathf.Clamp01(_settings.DangerHealthRatio);
        }
        
        private string BuildHandText()
        {
            return _hand.BuildHandDebugText(BuildSkillText);
        }
        
        private string BuildSkillText(int index)
        {
            SkillDataSO skill = null;
            _skills?.TryGetSkillDataByIndex(index, out skill);
            return BuildSkillText(index, skill);
        }
        
        private string BuildSkillText(int index, SkillDataSO skill)
        {
            if (skill == null)
                return $"번호 = {index}, 비어 있음";
            
            return $"번호 = {index}, {skill.skillName}, 등급 = {skill.grade}, " +
                   $"피해량 = {skill.damage}, 코스트 = {GetSkillCost(index)}";
        }
        
        private void Log(string message)
        {
            _log?.Invoke(message);
        }
        
    }
}
