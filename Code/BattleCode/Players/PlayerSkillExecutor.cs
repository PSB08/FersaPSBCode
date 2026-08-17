using CIW.Code;
using CIW.Code.Player.Combat.Adapters;
using Code.Scripts.Entities;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Skills;
using PSB_Lib.ObjectPool.RunTime;
using PSB_Lib.StatSystem;
using PSW.Code.EventBus;
using System;
using System.Collections.Generic;
using PSB.Code.BattleCode.Skills.Interfaces;
using UnityEngine;
using Work.YIS.Code.Skills;
using YIS.Code.Combat;
using YIS.Code.CoreSystem;
using YIS.Code.CoreSystem.Skills;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Skills;
using Random = UnityEngine.Random;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerSkillExecutor : ISkillExecutor, IDisposable
    {
        private readonly BattlePlayer _player;
        private readonly EntityStat _playerStat;
        
        private readonly PlayerSkillsCache _cache;
        private readonly BattleEnemyManager _enemyManager;
        private readonly PlayerTargetSelector _selector;
        private readonly BattleSkillUseServiceAdapter _skillUseServiceAdapter;

        private readonly StatSO _procChanceStat;
        
        private const float AttackScale = 0.5f;

        public PlayerSkillExecutor(PlayerSkillsCache cache, PoolManagerMono poolManager, 
            BattleEnemyManager enemyManager, PlayerTargetSelector selector,
            BattlePlayer player, StatSO procChanceStatDef, bool deferDamage = false,
            BattleSkillSystem battleSkillSystem = null
        )
        {
            _cache = cache;

            _enemyManager = enemyManager;
            _selector = selector;

            _player = player;
            _playerStat = player != null ? player.GetModule<EntityStat>() : null;
            _procChanceStat = procChanceStatDef;

            _skillUseServiceAdapter = new BattleSkillUseServiceAdapter(
                _player,
                battleSkillSystem,
                GetEnemyCandidates);
        }

        ~PlayerSkillExecutor()
        {
            Debug.Log("플레이어 실행자 소멸!");
        }

        public void Dispose()
        {
            _skillUseServiceAdapter?.Dispose();
        }

        public bool CanExecuteById(SkillEnum id, Transform target)
        {
            if (_cache == null) return false;

            return TryResolveSkillData(id, out SkillDataSO skillData) &&
                   _skillUseServiceAdapter != null &&
                   _skillUseServiceAdapter.CanUse(skillData, ResolveServiceDirectTarget(target), out _);
        }

        public bool ExecuteById(SkillEnum id, bool isChain, Transform target)
        {
            bool effectiveIsChain = ResolveChainFlag(isChain);

            if (TryResolveSkillData(id, out SkillDataSO skillData) &&
                TryExecuteSkillData(skillData, effectiveIsChain))
                return true;

            return false;
        }
        
        private bool RollProc()
        {
            float percent = 100f;

            if (_playerStat != null && _procChanceStat != null &&
                _playerStat.TryGetStat(_procChanceStat, out StatSO procStat) && procStat != null)
            {
                percent = Mathf.Clamp(procStat.Value, 0f, 100f);    
            }

            if (percent <= 0f) return false;
            if (percent >= 100f) return true;
            return Random.value * 100f < percent;
        }

        private bool TryResolveSkillData(SkillEnum id, out SkillDataSO skillData)
        {
            skillData = null;
            if (_cache == null)
                return false;

            return _cache.TryGetSkillData(id, out skillData) && skillData != null;
        }

        //체인이 아니면 prev 초기화 / 체인인데 prev가 없으면 경고
        private bool ResolveChainFlag(bool isChainFlag)
        {
            return isChainFlag;
        }

        private bool TryExecuteSkillData(SkillDataSO skillData, bool effectiveIsChain)
        {
            if (skillData == null || _skillUseServiceAdapter == null)
                return false;

            if (!TryGetTargets(skillData, out List<Entity> targets, out bool blocked) || targets == null)
                return false;

            if (blocked || targets.Count == 0)
            {
                return true;
            }

            if (!RollProc())
            {
                ShowMissText(targets[0]);
                return true;
            }

            if (_skillUseServiceAdapter.TrySubmit(
                    skillData,
                    targets[0],
                    effectiveIsChain,
                    _player,
                    out _))
            {
                return true;
            }

            return false;
        }

        //타겟 있는지 체크
        private bool HasValidTarget(out IReadOnlyList<Entity> enemies, out int centerIndex, out bool blocked)
        {
            enemies = null;
            centerIndex = -1;
            blocked = false;

            if (_enemyManager == null || _selector == null)
                return false;

            enemies = _enemyManager.GetEnemies();
            if (enemies == null || enemies.Count == 0)
                return false;

            centerIndex = _selector.GetCurrentTargetIndex();

            if (centerIndex < 0 || centerIndex >= enemies.Count)
            {
                blocked = TryShowEvadeTextFromBlockedEnemy(enemies);
                return false;
            }

            var center = enemies[centerIndex];
            if (center == null || center.IsDead)
                return false;

            if (!SkillTargetingUtil.CanBeDirectTarget(center, false))
            {
                SkillTargetingUtil.CanBeRangeTarget(center, true);
                blocked = true;
                return false;
            }

            return true;
        }

        //타겟 획득 타겟 리스트 생성
        private bool TryGetTargets(SkillDataSO skillData, out List<Entity> targets, out bool blocked)
        {
            targets = null;
            blocked = false;

            if (!HasValidTarget(out var enemies, out int centerIndex, out blocked))
            {
                if (blocked)
                {
                    targets = new List<Entity>();
                    return true;
                }

                return false;
            }

            int range = skillData != null ? skillData.range : 1;
            targets = SkillTargetingUtil.GetTargetsByRange(enemies, centerIndex, range, false);

            if (targets == null)
                targets = new List<Entity>();

            if (targets.Count == 0)
                blocked = true;

            return true;
        }
        
        //버프, 디버프에 따라 적을 찾을 수 있는가요
        private bool TryShowEvadeTextFromBlockedEnemy(IReadOnlyList<Entity> enemies)
        {
            if (enemies == null)
                return false;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];

                if (enemy == null || enemy.IsDead)
                    continue;

                if (!SkillTargetingUtil.CanBeRangeTarget(enemy, false))
                {
                    SkillTargetingUtil.CanBeRangeTarget(enemy, true);
                    return true;
                }

                if (!SkillTargetingUtil.CanBeDirectTarget(enemy, false))
                    return true;
            }

            return false;
        }
        
        private void ShowMissText(Entity target)
        {
            if (target == null || target.IsDead)
                return;

            Bus<EvadeEvent>.Raise(new EvadeEvent(null, target));
            Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(target.transform.position, new DamageData(0f, Elemental.Normal)));
        }
        
        private void ShowEvadeText(Entity target)
        {
            if (target == null || target.IsDead)
                return;

            DamageData evadeData = new DamageData(0f, Elemental.Normal)
            {
                Info = "회피!"
            };

            Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(target.transform.position, evadeData));
            Bus<EvadeEvent>.Raise(new EvadeEvent(null, target));
        }
        
        //pending에 쌓아둔 것들 다 실행해서 실제 데미지 적용
        public void FlushDeferredDamage()
        {
        }

        public void FlushDeferredDamage(int count)
        {
        }

        public bool TryGetSkill(SkillEnum id, out BaseSkill skill)
        {
            skill = null;
            return _cache != null &&
                   _cache.TryGetOrCreate(id, out skill) &&
                   skill != null;
        }

        private IReadOnlyList<Entity> GetEnemyCandidates()
        {
            return _enemyManager != null ? _enemyManager.GetEnemies() : null;
        }

        private Entity ResolveServiceDirectTarget(Transform target)
        {
            Entity targetEntity = target != null ? target.GetComponentInParent<Entity>() : null;
            if (targetEntity != null &&
                targetEntity != _player &&
                !targetEntity.IsDead)
            {
                return targetEntity;
            }

            BattleEnemy selectedEnemy = _selector != null ? _selector.GetCurrentTarget() : null;
            if (selectedEnemy != null && !selectedEnemy.IsDead)
                return selectedEnemy;

            return null;
        }
        
    }
}
