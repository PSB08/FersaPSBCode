using CIW.Code;
using DG.Tweening;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Commands;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Players;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Combat;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public class EnemyAttack : MonoBehaviour, IModule
    {
        [Header("Attack Dash")]
        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private Ease dashEase = Ease.OutQuad;

        [Header("Command")]
        [field: SerializeField] public SkillCommandSO skillCommand;
        [SerializeField] private StatSO procChanceStat;

        [Header("Skill Cache (Enemy)")]
        [SerializeField] private EnemySkillsCache skillsCache;

        [Inject] private PlayerManager _playerManager;
        [Inject] private PoolManagerMono _poolManager;
        [Inject] private BattleActionQueue _battleActionQueue;

        private BattleEnemy _battleEnemy;
        
        private EnemyAttackMover _mover;
        private EnemySkillExecutor _skillExecutor;

        public Entity BattleEnemy => _battleEnemy;
        public EnemyAttackMover Mover => _mover;
        public EnemySkillExecutor SkillExecutor => _skillExecutor;
        public bool IsMelee => !_battleEnemy.enemySO.isRanged;
        public bool HasPlanned => _skillExecutor != null && _skillExecutor.HasPlanned;
        public int PlannedIndex => _skillExecutor != null ? _skillExecutor.PlannedIndex : -1;
        
        public SkillDataSO[] CurrentSkills { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _battleEnemy = owner as BattleEnemy;

            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);

            Transform moveRoot = (_battleEnemy != null) ? _battleEnemy.transform : transform;

            _mover = new EnemyAttackMover(moveRoot, dashDuration, dashEase);

            if (skillsCache == null)
                skillsCache = owner.GetModule<EnemySkillsCache>();

            if (_battleEnemy != null)
                _skillExecutor = new EnemySkillExecutor(_battleEnemy, skillsCache, _poolManager, procChanceStat, _battleActionQueue);
        }

        public void SetAttackSkills(SkillDataSO[] skills)
        {
            CurrentSkills = skills;
            _skillExecutor.SetAttackSkills(skills);
        }

        private void OnDisable()
        {
            _mover.Kill();
            
            _skillExecutor?.DeactivateLast();
        }

        private void OnDestroy()
        {
            _skillExecutor.Dispose();
        }
        
        public bool PreActivateByIndex(int idx)
        {
            if (_skillExecutor == null) return false;
            if (!_skillExecutor.TryGetSkillIdByIndex(idx, out var id)) return false;

            if (skillsCache == null) return false;

            skillsCache.SetActive(id, true);
            _skillExecutor.MarkActivated(id);

            return true;
        }
        
        public bool PlanNextIntent()
        {
            if (_battleEnemy == null) return false;
            if (_skillExecutor == null) return false;
            if (skillsCache == null) return false;
            
            if (_skillExecutor.HasPlanned)
                return true;

            int idx = _skillExecutor.PickRandomSkillIndex();
            if (idx < 0) return false;

            if (!_skillExecutor.PlanByIndex(idx, out var id, out var so))
                return false;
            
            skillsCache.SetActive(id, true);
            _skillExecutor.MarkActivated(id);
            
            Bus<EnemyIntentPlannedEvent>.Raise(new EnemyIntentPlannedEvent(_battleEnemy, idx, id, so));
            return true;
        }

        public void ClearIntent()
        {
            if (_battleEnemy == null) return;

            _skillExecutor?.ClearPlan();
            Bus<EnemyIntentClearedEvent>.Raise(new EnemyIntentClearedEvent(_battleEnemy));
        }
        
    }
}