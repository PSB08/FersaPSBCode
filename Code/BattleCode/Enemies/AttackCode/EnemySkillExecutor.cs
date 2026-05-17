using System;
using System.Collections.Generic;
using CIW.Code;
using Code.Scripts.Entities;
using PSB_Lib.ObjectPool.RunTime;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Players;
using PSW.Code.EventBus;
using UnityEngine;
using Work.YIS.Code.Skills;
using YIS.Code.UI;
using YIS.Code.CoreSystem;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;
using Random = UnityEngine.Random;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public class EnemySkillExecutor : ISkillExecutor, IDisposable
    {
        private readonly BattleEnemy _battleEnemy;
        private readonly EnemySkillsCache _cache;
        private readonly EntitySkillEffectExecutor _effectExecutor;
        private readonly BattleActionQueue _battleActionQueue;
        
        private readonly EntityStat _enemyStat;
        private readonly StatSO _procChanceStat;

        private SkillDataSO[] _attackSkills;

        private readonly List<int> _validIdx = new();
        private readonly Dictionary<SkillEnum, SkillDataSO> _soById = new();
        
        public SkillEnum LastActivatedId { get; private set; }
        public bool HasLastActivated { get; private set; }
        
        public SkillEnum PlannedId { get; private set; }
        public int PlannedIndex { get; private set; } = -1;
        public SkillDataSO PlannedSo { get; private set; }
        public bool HasPlanned { get; private set; }

        public EnemySkillExecutor(BattleEnemy battleEnemy, EnemySkillsCache cache, 
            PoolManagerMono poolManager, StatSO procChanceStat, BattleActionQueue battleActionQueue)
        {
            _battleEnemy = battleEnemy;
            _cache = cache;
            _procChanceStat = procChanceStat;
            _enemyStat = battleEnemy != null ? battleEnemy.GetModule<EntityStat>() : null;
            _battleActionQueue = battleActionQueue;

            HasLastActivated = false;
            LastActivatedId = default;
            _effectExecutor = new EntitySkillEffectExecutor(_cache, poolManager);

            Bus<OnPlayEffectEvent>.OnEvent += HandlePlayEffect;
        }
        ~EnemySkillExecutor()
        {
            Debug.Log("적 실행자 소멸!");
            Bus<OnPlayEffectEvent>.OnEvent -= HandlePlayEffect;
        }
        
        public void Dispose()
        {
            Bus<OnPlayEffectEvent>.OnEvent -= HandlePlayEffect;
        }

        private void HandlePlayEffect(OnPlayEffectEvent evt)
        {
            ApplyTargetsImmediate(evt.Skill, evt.Targets);
        }

        private void ApplyTargetsImmediate(BaseSkill skill, IReadOnlyList<Entity> targets)
        {
            foreach (var target in targets)
            {
                if (target == null || target.IsDead) continue;

                var player = target as BattlePlayer;
                if (player == null) continue;
                
                _effectExecutor.PlayEffectForTarget(skill, player.transform, target, skill.SkillData);
            }
        }

        public void SetAttackSkills(SkillDataSO[] skills)
        {
            _attackSkills = skills;

            _validIdx.Clear();
            _soById.Clear();

            if (_attackSkills != null)
            {
                for (int i = 0; i < _attackSkills.Length; i++)
                {
                    var so = _attackSkills[i];
                    if (so == null) continue;

                    _validIdx.Add(i);
                    _soById[(SkillEnum)so.index] = so;
                }
            }
            
            _cache?.Prewarm(_attackSkills);
            
            HasLastActivated = false;
            LastActivatedId = default;
            ClearPlan();
        }

        public int PickRandomSkillIndex()
        {
            if (_validIdx.Count == 0) return -1;
            return _validIdx[Random.Range(0, _validIdx.Count)];
        }

        public bool TryGetSkillIdByIndex(int idx, out SkillEnum id)
        {
            id = default;

            if (_attackSkills == null) return false;
            if (idx < 0 || idx >= _attackSkills.Length) return false;

            var so = _attackSkills[idx];
            if (so == null) return false;

            id = (SkillEnum)so.index;
            return true;
        }
        
        public void DeactivateLast()
        {
            if (!HasLastActivated) return;
            Deactivate(LastActivatedId);
            HasLastActivated = false;
            LastActivatedId = default;
        }

        public void Deactivate(SkillEnum id)
        {
            if (_cache == null) return;
            _cache.SetActive(id, false);
        }
        
        public bool CanExecuteById(SkillEnum id, Transform target)
        {
            if (_battleEnemy == null) return false;
            if (_cache == null) return false;
            if (target == null) return false;

            if (!_soById.TryGetValue(id, out var so) || so == null) return false;

            return _cache.TryGetOrCreate(id, so, out _);
        }

        public bool ExecuteById(SkillEnum id, bool isChain, Transform target)
        {
            if (!CanExecuteById(id, target)) return false;

            if (!_soById.TryGetValue(id, out var so) || so == null) return false;
            if (!_cache.TryGetOrCreate(id, so, out var skill) || skill == null) return false;

            if (!RollProc())
            {
                Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(target.position, 0, Elemental.Normal ));
                return false;
            }
            
            var battlePlayer = target.GetComponentInParent<BattlePlayer>();
            if (battlePlayer == null) return false;

            List<Entity> targetEntity = new List<Entity>();
            targetEntity.Add(battlePlayer);
            
            _battleActionQueue.EnBattleQueue(new UseSkillAction(_battleEnemy, skill, isChain, targetEntity));
            
            //사운드 등등 실행라인
            _battleActionQueue.BattleAction();
            
            return true;
        }

        private float ApplyEnemyStatScaling(float baseDmg)
        {
            if (_battleEnemy == null || _battleEnemy.enemySO == null) return baseDmg;

            EntityStat statComp = _battleEnemy.GetModule<EntityStat>();
            if (statComp == null) return baseDmg;

            string atkName = _battleEnemy.enemySO.attackStatName;
            float scale = _battleEnemy.enemySO.attackStatScale;

            if (string.IsNullOrEmpty(atkName) || Mathf.Approximately(scale, 0f))
                return baseDmg;

            if (statComp.TryGetStat(atkName, out StatSO atkStat))
                return baseDmg + atkStat.Value * scale;

            return baseDmg;
        }
        
        public void MarkActivated(SkillEnum id)
        {
            HasLastActivated = true;
            LastActivatedId = id;
        }
        
        public bool PlanByIndex(int idx, out SkillEnum id, out SkillDataSO so)
        {
            id = default;
            so = null;

            if (!TryGetSkillIdByIndex(idx, out id)) return false;
            if (!_soById.TryGetValue(id, out so) || so == null) return false;

            HasPlanned = true;
            PlannedId = id;
            PlannedIndex = idx;
            PlannedSo = so;
            return true;
        }
        
        private bool RollProc()
        {
            float percent = 100f;

            if (_enemyStat != null && _procChanceStat != null &&
                _enemyStat.TryGetStat(_procChanceStat, out StatSO procStat) && procStat != null)
            {
                percent = Mathf.Clamp(procStat.Value, 0f, 100f);
            }

            if (percent <= 0f) return false;
            if (percent >= 100f) return true;
            return Random.value * 100f < percent;
        }

        public void ClearPlan()
        {
            HasPlanned = false;
            PlannedId = default;
            PlannedIndex = -1;
            PlannedSo = null;
        }
        
    }
}
