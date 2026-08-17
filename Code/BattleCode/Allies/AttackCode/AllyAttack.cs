using System.Collections.Generic;
using System.Threading.Tasks;
using CIW.Code;
using DG.Tweening;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Skills;
using PSB_Lib.Dependencies;
using PSB_Lib.StatSystem;
using UnityEngine;
using Work.YIS.Code.Skills;
using YIS.Code.CoreSystem.Skills;
using YIS.Code.Defines;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Allies.AttackCode
{
    public sealed class AllyAttack : MonoBehaviour, IModule
    {
        [Header("공격 이동")]
        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private Ease dashEase = Ease.OutQuad;
        [SerializeField, Min(0f)] private float forwardDashDistance = 0.75f;
        
        [Header("스킬")]
        [SerializeField] private StatSO procChanceStat;
        
        [Header("동료 AI")]
        [SerializeField, Min(1)] private int maxActionsPerTurn = 10;
        [SerializeField, Range(0f, 1f)] private float defenseUseChance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float defenseDangerHealthRatio = 0.5f;
        
        [Inject] private BattleEnemyManager _enemyManager;
        [Inject] private BattleAllyManager _allyManager;
        [Inject] private BattleSkillUseService _skillUseService;
        [Inject] private BattleSkillSystem _battleSkillSystem;
        
        private readonly AllyTurnBrain _brain = new();
        
        private BattleAlly _ally;
        private AllyAttackMover _mover;
        private AllySkillExecutor _skillExecutor;
        private bool _hasCachedTurnStartPosition;
        private bool _hasPerformedTurnApproach;
        
        public Entity BattleAlly => _ally;
        public AllyAttackMover Mover => _mover;
        public AllySkillExecutor SkillExecutor => _skillExecutor;
        
        public bool IsMelee => true;
        public bool HasPlanned => _brain.HasPlan;
        public bool HasPlannedSkip => _brain.HasSkip;
        public int PlannedIndex => _brain.PlannedIndex;
        public bool HasAttackSkill => HasAnySkill(CurrentSkills);
        public float ForwardDashDistance => forwardDashDistance;
        public bool HasCachedTurnStartPosition => _hasCachedTurnStartPosition;
        public bool HasPerformedTurnApproach => _hasPerformedTurnApproach;
        
        public SkillDataSO[] CurrentSkills { get; private set; }
        
        public void Initialize(ModuleOwner owner)
        {
            _ally = owner as BattleAlly;
            
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);
            
            Transform moveRoot = _ally != null ? _ally.transform : transform;
            _mover = new AllyAttackMover(moveRoot, dashDuration, dashEase);
            
            if (_ally != null)
            {
                _skillExecutor = new AllySkillExecutor(_ally, procChanceStat, _skillUseService);
            }
            
            _brain.Bind(_ally, this, _skillExecutor, _battleSkillSystem, BuildSettings(), LogAiDebug);
            
            if (CurrentSkills != null)
                ApplySkills(CurrentSkills);
        }
        
        private void OnDisable()
        {
            _mover?.Kill();
            _brain.ResetTurn();
            _hasCachedTurnStartPosition = false;
            _hasPerformedTurnApproach = false;
        }
        
        public void SetAttackSkills(SkillDataSO[] skills)
        {
            CurrentSkills = skills;
            
            if (_skillExecutor != null)
                ApplySkills(skills);
        }
        
        public void NotifyHit()
        {
        }
        
        public bool PrepareTurnForPlanning()
        {
            return _brain.Prepare();
        }
        
        public bool TryPlanNextActionInCurrentTurn()
        {
            return _brain.PlanNext(false) && !_brain.HasSkip && _skillExecutor != null && _skillExecutor.HasPlanned;
        }
        
        public bool PlanNextIntent()
        {
            return _brain.PlanNext(true);
        }
        
        public bool TryPlanSkillIntent()
        {
            return _brain.PlanSkillIntent();
        }
        
        public bool TryPlanDefenseIntent()
        {
            return _brain.PlanDefenseIntent();
        }
        
        public bool TryPlanDrawIntent()
        {
            return false;
        }
        
        public bool TryPlanCostRecoveryIntent()
        {
            return false;
        }
        
        public bool TryPlanPatternIntent()
        {
            return TryPlanSkillIntent();
        }
        
        public bool TryPlanBestActionIntent()
        {
            return TryPlanSkillIntent();
        }
        
        public bool PlanSkipIntent()
        {
            return _brain.PlanSkip();
        }
        
        public bool TrySelectTargetTransform(out Transform target, out string reason)
        {
            target = null;
            reason = string.Empty;
            
            ResolveEnemyManager();
            
            float estimatedDamage = _brain.EstimateCurrentDamage(CurrentSkills);
            if (!AllyTargetSelector.TrySelectTarget(_enemyManager, estimatedDamage, out BattleEnemy selectedTarget,
                    out _, out reason))
            {
                if (string.IsNullOrEmpty(reason))
                    reason = "유효한 타겟이 없습니다.";
                
                return false;
            }
            
            target = selectedTarget != null ? selectedTarget.transform : null;
            if (target != null)
                return true;
            
            reason = "선택한 타겟의 Transform이 없습니다.";
            return false;
        }
        
        public bool TryBuildSkillTargets(SkillDataSO skillData, out Entity directTarget,
            out SkillTargetCandidates candidates, out Transform facingTarget, out string reason)
        {
            directTarget = null;
            facingTarget = null;
            reason = string.Empty;
            
            ResolveEnemyManager();
            List<Entity> targets = new();
            IReadOnlyList<BattleEnemy> enemies = _enemyManager != null ? _enemyManager.GetEnemies() : null;
            
            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    BattleEnemy enemy = enemies[i];
                    if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy || targets.Contains(enemy))
                    {
                        continue;
                    }
                    
                    targets.Add(enemy);
                }
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
            
            _allyManager?.ResetTurnDoneTimeout(_ally);
            
            try
            {
                return await _skillExecutor.ExecutePlannedSkillAsync(directTarget, candidates, false);
            }
            finally
            {
                _allyManager?.ResetTurnDoneTimeout(_ally);
            }
        }
        
        public float EstimateIncomingDamage()
        {
            ResolveEnemyManager();
            float best = 0f;
            IReadOnlyList<BattleEnemy> enemies = _enemyManager != null ? _enemyManager.GetEnemies() : null;
            
            if (enemies == null)
                return best;
            
            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemy enemy = enemies[i];
                EnemyAttack enemyAttack = enemy != null ? enemy.GetModule<EnemyAttack>() : null;
                
                if (enemyAttack != null)
                    best = Mathf.Max(best, enemyAttack.EstimatePotentialDamage());
            }
            
            return Mathf.Max(0f, best);
        }
        
        public float EstimatePotentialDamage()
        {
            return _brain.EstimateCurrentDamage(CurrentSkills);
        }
        
        public void MarkTurnStartPositionCached()
        {
            _hasCachedTurnStartPosition = true;
        }
        
        public void MarkTurnApproachPerformed()
        {
            _hasPerformedTurnApproach = true;
        }
        
        public void FaceTarget(Transform target)
        {
            if (_ally != null && _ally.animator != null && target != null)
                _ally.animator.FlipTowardsTarget(target);
        }
        
        public void CompletePlannedIntent()
        {
            _brain.CompleteAction();
        }
        
        public void ClearIntent()
        {
            _brain.ClearPlan();
        }
        
        public void EndAllyTurnPlanning()
        {
            _hasCachedTurnStartPosition = false;
            _hasPerformedTurnApproach = false;
            _brain.EndTurn();
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
        
        private void ApplySkills(SkillDataSO[] skills)
        {
            _skillExecutor?.SetAttackSkills(skills);
            _brain.SetSkills(skills);
        }
        
        private AllyAiSettings BuildSettings()
        {
            return new AllyAiSettings
            {
                MaxActionsPerTurn = maxActionsPerTurn,
                DefenseUseChance = defenseUseChance,
                DangerHealthRatio = defenseDangerHealthRatio
            };
        }
        
        private void ResolveEnemyManager()
        {
            if (_enemyManager == null)
                _enemyManager = FindAnyObjectByType<BattleEnemyManager>();
        }
        
        private static bool HasAnySkill(SkillDataSO[] skills)
        {
            return CountAssignedSkills(skills) > 0;
        }
        
        private static int CountAssignedSkills(SkillDataSO[] skills)
        {
            if (skills == null || skills.Length == 0)
                return 0;
            
            int count = 0;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null)
                    count++;
            }
            
            return count;
        }
        
    }
}
