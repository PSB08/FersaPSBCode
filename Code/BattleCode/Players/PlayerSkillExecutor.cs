using CIW.Code;
using Code.Scripts.Entities;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Entities;
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
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;
using Random = UnityEngine.Random;

namespace PSB.Code.BattleCode.Players
{
    //데미지를 지금 당장 넣지 않고 나중에 넣기 위함 - 어떤 스킬을 누구에게 어떤 이펙트로 실행할지 저장하는 용도임
    public struct PendingHit
    {
        public readonly BaseSkill Skill;
        public readonly List<Entity> Targets;
        public readonly SkillDataSO VFX;

        public PendingHit(BaseSkill s, List<Entity> t, SkillDataSO v)
        {
            Skill = s;
            Targets = t;
            VFX = v;
        }
    }

    public class PlayerSkillExecutor : ISkillExecutor, IDisposable
    {
        private readonly BattleActionQueue _battleActionQueue;
        private readonly BattlePlayer _player;
        private readonly EntityStat _playerStat;
        
        private readonly PlayerSkillsCache _cache;
        private readonly BattleEnemyManager _enemyManager;
        private readonly PlayerTargetSelector _selector;
        private readonly EntitySkillEffectExecutor _effectExecutor;

        private readonly StatSO _procChanceStat;
        
        private const float AttackScale = 0.5f;

        private BaseSkill _prevExecutedSkill;

        private readonly bool _deferDamage;
        private readonly List<PendingHit> _pending = new();
        private IDisposable _disposableImplementation;

        public PlayerSkillExecutor(PlayerSkillsCache cache, PoolManagerMono poolManager, 
            BattleEnemyManager enemyManager, PlayerTargetSelector selector, BattleActionQueue battleActionQueue,
            BattlePlayer player,  StatSO procChanceStatDef, bool deferDamage = false
        )
        {
            _cache = cache;

            _enemyManager = enemyManager;
            _selector = selector;
            _battleActionQueue = battleActionQueue;

            _player = player;
            _playerStat = player != null ? player.GetModule<EntityStat>() : null;
            _procChanceStat = procChanceStatDef;

            _prevExecutedSkill = null;
            _deferDamage = deferDamage;
            
            _effectExecutor = new EntitySkillEffectExecutor(_cache, poolManager);

            Bus<OnPlayEffectEvent>.OnEvent += HandlePlayEffect;
        }

        ~PlayerSkillExecutor()
        {
            Debug.Log("플레이어 실행자 소멸!");
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

        public bool CanExecuteById(SkillEnum id, Transform _)
        {
            if (_cache == null) return false;
            return _cache.TryGetOrCreate(id, out BaseSkill _);
        }

        public bool ExecuteById(SkillEnum id, bool isChain, Transform _)
        {
            if (!TryPrepareSkill(id, _, out var skill))
                return false;

            if (!RollProc()) return false;

            bool effectiveIsChain = ResolveChainFlag(isChain);

            if (!effectiveIsChain)
            {
                if (!HasValidTarget())
                    return false;
            }

            if (!ApplyEnchantIfNeeded(effectiveIsChain, skill, out var enchantBackup,
                    out bool enchanted, out SkillDataSO vfxSourceData))
                return false;

            if (!TryGetTargets(skill, out var targets) || targets == null || targets.Count == 0)
                return effectiveIsChain;

            if (!TryExecuteSkill(skill, effectiveIsChain, enchanted, enchantBackup, targets))
                return false;

            _prevExecutedSkill = skill;

            if (skill.SkipDamageThisCast)
                return true;

            if (_deferDamage)
            {
                _pending.Add(new PendingHit(skill, new List<Entity>(targets), vfxSourceData));
                return true;
            }

            if (enchanted)
            {
                skill.ChangeElemental(enchantBackup);
            }
            return true;
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

        //스킬을 꺼내고 게임오브젝트를 활성화
        private bool TryPrepareSkill(SkillEnum id, Transform target, out BaseSkill skill)
        {
            skill = null;

            if (!CanExecuteById(id, target))
                return false;

            if (!_cache.TryGetOrCreate(id, out skill) || skill == null)
                return false;

            skill.gameObject.SetActive(true);
            return true;
        }

        //체인이 아니면 prev 초기화 / 체인인데 prev가 없으면 경고
        private bool ResolveChainFlag(bool isChainFlag)
        {
            if (!isChainFlag)
            {
                _prevExecutedSkill = null;
                return false;
            }

            if (_prevExecutedSkill == null)
                Debug.LogWarning("[Chain] isChain=true but prevExecutedSkill is null. Enchant will be skipped.");

            return true;
        }

        //체인으로 연결되면, 다음 스킬이 이전 스킬의 속성을 물려받는 곳
        private bool ApplyEnchantIfNeeded(bool effectiveIsChain, BaseSkill currentSkill,
            out Elemental backup, out bool enchanted, out SkillDataSO vfxSourceData)
        {
            backup = default;
            enchanted = false;
            vfxSourceData = null;

            if (!effectiveIsChain) return true;
            if (_prevExecutedSkill == null) return true;

            if (_prevExecutedSkill is IEnchantProvider provider &&
                currentSkill is IEnchantable receiver)
            {
                backup = currentSkill.CurrentElementalState.CurrentElemental;
                receiver.ApplyEnchant(currentSkill.CurrentElementalState.CurrentElemental);
                enchanted = true;
                
                vfxSourceData = _prevExecutedSkill.SkillData;
            }

            return true;
        }

        //조건 통과하면 실행 성공 실패하면 속성 복구
        private bool TryExecuteSkill(BaseSkill skill, bool effectiveIsChain, bool enchanted, Elemental backup, List<Entity> targets)
        {
            _battleActionQueue.EnBattleQueue(new UseSkillAction(_player, skill, effectiveIsChain, targets));
            //_battleActionQueue.EnBattleQueue(new UseSkillAction(_player, skill, effectiveIsChain, targets));

            if (enchanted)
                skill.ChangeElemental(backup);

            Debug.LogWarning("스킬 실행 실패: 쿨타임 상태");
            return true;
        }

        //타겟 있는지 체크
        private bool HasValidTarget()
        {
            if (_enemyManager == null || _selector == null) return false;

            var enemies = _enemyManager.GetEnemies();
            if (enemies == null || enemies.Count == 0) return false;

            int centerIndex = _selector.GetCurrentTargetIndex();
            if (centerIndex < 0 || centerIndex >= enemies.Count) return false;

            var center = enemies[centerIndex];
            if (center == null || center.IsDead) return false;

            return true;
        }

        //타겟 획득 타겟 리스트 생성
        private bool TryGetTargets(BaseSkill skill, out List<Entity> targets)
        {
            targets = null;

            if (_enemyManager == null || _selector == null)
                return false;

            var enemies = _enemyManager.GetEnemies();
            int centerIndex = _selector.GetCurrentTargetIndex();

            if (enemies == null || enemies.Count == 0 || centerIndex < 0 || centerIndex >= enemies.Count)
                return false;

            var centerTarget = enemies[centerIndex];
            if (centerTarget == null || centerTarget.IsDead)
                return false;

            int range = skill.SkillData != null ? skill.SkillData.range : 1;
            targets = SkillTargetingUtil.GetTargetsByRange(enemies, centerIndex, range);
            return targets != null;
        }
        
        //타겟들 순회하면서 죽음이면 스킵 - 캐스팅 - 데미지
        private void ApplyTargetsImmediate(BaseSkill skill, IReadOnlyList<Entity> targets)
        {
            foreach (var be in targets)
            {
                if (be == null || be.IsDead) continue;

                var enemy = be as NormalBattleEnemy;
                if (enemy == null || enemy.IsDead) continue;
                
                //이펙트 및 사운드 등등 실행라인
                _effectExecutor.PlayEffectForTarget(skill, enemy.transform, enemy, skill.SkillData);
            }
        }

        private float CalculateScaledDamage(BaseSkill skill)
        {
            float baseDmg = skill.GetFinalDamage();

            if (_playerStat != null &&
                _playerStat.TryGetStat("Attack", out StatSO atkStat) &&
                atkStat != null)
            {
                baseDmg += atkStat.Value * AttackScale;
            }

            return baseDmg;
        }

        public bool TryGetSkill(SkillEnum id, out BaseSkill skill)
        {
            skill = null;
            if (_cache == null) return false;
            if (!_cache.TryGetOrCreate(id, out var s) || s == null) return false;
            skill = s;
            return true;
        }

        //pending에 쌓아둔 것들 다 실행해서 실제 데미지 적용
        public void FlushDeferredDamage()
        {
            if (_pending.Count == 0) return;

            for (int i = 0; i < _pending.Count; i++)
            {
                var p = _pending[i];
                if (p.Skill == null || p.Targets == null || p.Targets.Count == 0) continue;

                ApplyTargetsImmediate(p.Skill, p.Targets);
            }

            _pending.Clear();
        }

        public void FlushDeferredDamage(int count)
        {
            if (_pending.Count == 0) return;
            if (count <= 0) return;

            int n = count > _pending.Count ? _pending.Count : count;

            for (int i = 0; i < n; i++)
            {
                var p = _pending[i];
                if (p.Skill == null || p.Targets == null || p.Targets.Count == 0) continue;

                ApplyTargetsImmediate(p.Skill, p.Targets);
            }

            _pending.RemoveRange(0, n);
        }
        
    }
}
