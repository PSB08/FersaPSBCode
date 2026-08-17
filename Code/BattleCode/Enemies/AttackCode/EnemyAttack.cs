using System.Collections.Generic;
using System.Threading.Tasks;
using CIW.Code;
using DG.Tweening;
using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Players;
using PSB.Code.BattleCode.Skills;
using PSB_Lib.Dependencies;
using PSB_Lib.StatSystem;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.CoreSystem.Skills;
using YIS.Code.Defines;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public class EnemyAttack : MonoBehaviour, IModule
    {
        [Header("Attack Dash")]
        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private Ease dashEase = Ease.OutQuad;
        
        [Header("Skill")]
        [SerializeField] private StatSO procChanceStat;
        
        [Header("Enemy AI")]
        [SerializeField, Range(0f, 1f)] private float defenseDangerHealthRatio = 0.5f;
        [SerializeField, Range(0f, 1f)] private float defenseUseChance = 0.5f;
        
        [Inject] private PlayerManager _playerManager;
        [Inject] private BattleAllyManager _allyManager;
        [Inject] private BattleEnemyManager _enemyManager;
        [Inject] private BattleSkillUseService _skillUseService;
        [Inject] private BattleSkillSystem _battleSkillSystem;
        
        private readonly EnemyTurnBrain _brain = new();
        
        private BattleEnemy _enemy;
        private EnemyAttackMover _mover;
        private EnemySkillExecutor _skillExecutor;
        private bool _hasCachedTurnStartPosition;
        private bool _hasPerformedTurnApproach;
        private bool _beginBattleSkillsPending;
        
        public Entity BattleEnemy => _enemy;
        public EnemyAttackMover Mover => _mover;
        public EnemySkillExecutor SkillExecutor => _skillExecutor;
        public bool IsMelee => _enemy != null && _enemy.enemySO != null && !_enemy.enemySO.isRanged;
        public bool HasPlanned => _brain.HasPlan;
        public bool HasPlannedSkip => _brain.HasSkip;
        public int PlannedIndex => _brain.PlannedIndex;
        public int CurrentCost => _brain.CurrentCost;
        public int MaxCost => _brain.MaxCost;
        public bool HasCachedTurnStartPosition => _hasCachedTurnStartPosition;
        public bool HasPerformedTurnApproach => _hasPerformedTurnApproach;
        public bool HasAttackSkill => CountAssignedSkills(CurrentSkills) > 0;
        public SkillDataSO[] CurrentSkills { get; private set; }
        
        private void OnEnable()
        {
            Bus<OnSetupSkillUIEvent>.OnEvent += HandlePlayerSkillSetup;
        }
        
        public void Initialize(ModuleOwner owner)
        {
            _enemy = owner as BattleEnemy;
            
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);
            
            Transform moveRoot = _enemy != null ? _enemy.transform : transform;
            _mover = new EnemyAttackMover(moveRoot, dashDuration, dashEase);
            
            if (_enemy != null)
                _skillExecutor = new EnemySkillExecutor(_enemy, procChanceStat, _skillUseService);
            
            _brain.Bind(_enemy, this, _skillExecutor, _battleSkillSystem, BuildSettings(), LogAiDebug);
            
            if (CurrentSkills != null)
                ApplySkills(CurrentSkills, _beginBattleSkillsPending);
        }
        
        public void BeginBattleSkills(SkillDataSO[] skills)
        {
            CurrentSkills = skills;
            _beginBattleSkillsPending = true;
            
            if (_skillExecutor == null)
                return;
            
            ApplySkills(skills, true);
        }
        
        public void SetAttackSkills(SkillDataSO[] skills)
        {
            CurrentSkills = skills;
            _beginBattleSkillsPending = false;
            
            if (_skillExecutor == null)
                return;
            
            ApplySkills(skills, false);
        }
        
        private void ApplySkills(SkillDataSO[] skills, bool beginBattle)
        {
            _skillExecutor.SetAttackSkills(skills);
            
            if (beginBattle)
                _brain.BeginBattle(skills);
            else
                _brain.SetSkills(skills);
            
            _enemy?.NotifyMechanicSkillsChanged(skills, beginBattle);
            _brain.WarnIfSkillLimitExceeded(skills, this);
            _hasCachedTurnStartPosition = false;
            _hasPerformedTurnApproach = false;
            _beginBattleSkillsPending = false;
        }
        
        private void OnDisable()
        {
            Bus<OnSetupSkillUIEvent>.OnEvent -= HandlePlayerSkillSetup;
            _mover?.Kill();
            _hasPerformedTurnApproach = false;
        }
        
        private void HandlePlayerSkillSetup(OnSetupSkillUIEvent evt)
        {
            _brain.SetPlayerSkills(evt.SelectedSkills);
        }
        
        public void NotifyHit()
        {
            _enemy?.NotifyMechanicHitObserved();
        }
        
        public bool PlanNextIntent()
        {
            if (!HasSelectableTargetForPlan())
                return PlanSkipIntent();
            
            return _brain.PlanNext(true);
        }
        
        public bool TryPlanNextActionInCurrentTurn()
        {
            if (!HasSelectableTargetForPlan())
                return false;
            
            return _brain.PlanNext(false) && !_brain.HasSkip && _skillExecutor != null && _skillExecutor.HasPlanned;
        }
        
        public bool PrepareTurnForPlanning()
        {
            return _brain.Prepare();
        }
        
        public bool TryPlanDefenseIntent()
        {
            return _brain.PlanStep(EnemyPlanStep.Defense);
        }
        
        public bool TryPlanDrawIntent()
        {
            return _brain.PlanStep(EnemyPlanStep.Draw);
        }
        
        public bool TryPlanCostRecoveryIntent()
        {
            return _brain.PlanStep(EnemyPlanStep.CostRecovery);
        }
        
        public bool TryPlanPatternIntent()
        {
            return _brain.PlanStep(EnemyPlanStep.Pattern);
        }
        
        public bool TryPlanBestActionIntent()
        {
            return _brain.PlanStep(EnemyPlanStep.BestAction);
        }
        
        public bool PlanSkipIntent()
        {
            return _brain.PlanSkip();
        }
        
        public bool TrySelectTargetTransform(out Transform target, out string reason)
        {
            ResolveTargetManagers();
            return _brain.TrySelectTarget(_playerManager, _allyManager, out target, out reason);
        }
        
        public bool TryBuildSkillTargets(SkillDataSO skillData, out Entity directTarget, 
            out SkillTargetCandidates candidates, out Transform facingTarget, out string reason)
        {
            directTarget = null;
            facingTarget = null;
            reason = string.Empty;
            
            ResolveTargetManagers();
            List<Entity> targets = new();
            
            IReadOnlyList<Entity> partyTargets = _playerManager != null ? _playerManager.PartyTargets : null;
            
            if (partyTargets != null)
            {
                for (int i = 0; i < partyTargets.Count; i++)
                    AddTargetCandidate(targets, partyTargets[i]);
            }
            
            if (_playerManager != null)
                AddTargetCandidate(targets, _playerManager.BattlePlayer);
            
            IReadOnlyList<BattleAlly> allies = _allyManager != null ? _allyManager.GetAllies() : null;
            
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                    AddTargetCandidate(targets, allies[i]);
            }
            
            candidates = SkillTargetCandidates.Many(targets.ToArray());
            
            if (skillData != null && skillData.targetType == TargetType.None)
            {
                TrySelectTargetTransform(out facingTarget, out _);
                return true;
            }
            
            if (!TrySelectTargetTransform(out facingTarget, out reason))
                return false;
            
            directTarget = facingTarget != null ? facingTarget.GetComponentInParent<Entity>() : null;
            
            return directTarget != null;
        }
        
        public async Task<BtSkillUseResult> ExecutePlannedSkillAsync(Entity directTarget, SkillTargetCandidates candidates)
        {
            if (_skillExecutor == null)
                return BtSkillUseResult.Failed;
            
            _enemyManager?.ResetTurnDoneTimeout(_enemy);
            
            try
            {
                return await _skillExecutor.ExecutePlannedSkillAsync(directTarget, candidates, true);
            }
            finally
            {
                _enemyManager?.ResetTurnDoneTimeout(_enemy);
            }
        }
        
        public float EstimateIncomingDamage(IReadOnlyList<SkillDataSO> playerSkills)
        {
            float best = FindHighestDamage(playerSkills);
            ResolveTargetManagers();
            
            IReadOnlyList<BattleAlly> allies = _allyManager != null ? _allyManager.GetAllies() : null;
            
            if (allies == null)
                return best;
            
            for (int i = 0; i < allies.Count; i++)
            {
                BattleAlly ally = allies[i];
                AllyAttack allyAttack = ally != null ? ally.GetModule<AllyAttack>() : null;
                
                if (allyAttack != null)
                    best = Mathf.Max(best, allyAttack.EstimatePotentialDamage());
            }
            
            return Mathf.Max(0f, best);
        }
        
        public float EstimatePotentialDamage()
        {
            if (_skillExecutor != null && _skillExecutor.HasPlanned)
                return Mathf.Max(0f, _skillExecutor.PlannedSo != null ? _skillExecutor.PlannedSo.damage : 0f);
            
            return FindHighestDamage(CurrentSkills);
        }
        
        public void CompletePlannedIntent()
        {
            _brain.CompleteAction();
        }
        
        public void EndEnemyTurnPlanning()
        {
            _hasCachedTurnStartPosition = false;
            _hasPerformedTurnApproach = false;
            _brain.EndTurn();
        }
        
        public void ClearCachedTurnStartPosition()
        {
            _hasCachedTurnStartPosition = false;
            _hasPerformedTurnApproach = false;
        }
        
        public void PrepareForExternalReposition()
        {
            _hasCachedTurnStartPosition = false;
            _hasPerformedTurnApproach = false;
            _mover?.Kill();
        }
        
        public void MarkTurnStartPositionCached()
        {
            _hasCachedTurnStartPosition = true;
        }
        
        public void MarkTurnApproachPerformed()
        {
            _hasPerformedTurnApproach = true;
        }
        
        public void ClearIntent()
        {
            _brain.ClearPlan();
        }
        
        public int GetSkillCost(int skillIndex)
        {
            return _brain.GetSkillCost(skillIndex);
        }
        
        public string BuildPlannedDebugText()
        {
            return _brain.BuildPlannedText();
        }
        
        public void LogAiDebug(string message)
        {
        }
        
        private EnemyAiSettings BuildSettings()
        {
            return new EnemyAiSettings
            {
                DangerHealthRatio = defenseDangerHealthRatio,
                DefenseUseChance = defenseUseChance
            };
        }
        
        private static void AddTargetCandidate(List<Entity> targets, Entity target)
        {
            if (target == null || target.IsDead || !target.gameObject.activeInHierarchy || targets.Contains(target))
            {
                return;
            }
            
            targets.Add(target);
        }
        
        private static float FindHighestDamage(IReadOnlyList<SkillDataSO> skills)
        {
            float best = 0f;
            if (skills == null)
                return best;
            
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDataSO skill = skills[i];
                if (skill != null)
                    best = Mathf.Max(best, skill.damage);
            }
            
            return Mathf.Max(0f, best);
        }
        
        private static int CountAssignedSkills(SkillDataSO[] skills)
        {
            if (skills == null) return 0;
            
            int count = 0;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null)
                    count++;
            }
            
            return count;
        }
        
        private bool HasSelectableTargetForPlan()
        {
            if (TrySelectTargetTransform(out _, out string reason))
                return true;
            
            LogAiDebug($"공격 가능한 대상 없음 : {reason}");
            return false;
        }
        
        private void ResolveTargetManagers()
        {
            if (_playerManager == null)
                _playerManager = FindAnyObjectByType<PlayerManager>();
            
            if (_allyManager == null)
                _allyManager = FindAnyObjectByType<BattleAllyManager>();
        }
        
    }
}
