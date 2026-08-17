using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Enemies.PhaseBreak;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.UIs;
using PSW.Code.EventBus;
using Work.YIS.Code.Buffs;
using YIS.Code.Combat;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Players
{
    public static class SkillTargetingUtil
    {
        public static List<Entity> GetTargetsByRange(IReadOnlyList<Entity> enemies, int centerIndex, 
            int range, bool logBlockedTarget = false)
        {
            var result = new List<Entity>();
            if (enemies == null || enemies.Count == 0) return result;

            if (range <= 0) range = 1;
            if (centerIndex < 0 || centerIndex >= enemies.Count) return result;

            int maxCount = enemies.Count;
            int targetCount = range > maxCount ? maxCount : range;

            int checkedCount = 0;
            int indexOffset = 0;

            while (checkedCount < maxCount && result.Count < targetCount)
            {
                int idx = (centerIndex + indexOffset) % enemies.Count;
                var e = enemies[idx];

                if (CanBeRangeTarget(e, logBlockedTarget))
                {
                    result.Add(e);
                }

                indexOffset++;
                checkedCount++;
            }

            return result;
        }

        public static bool TryGetTauntTargetIndex<T>(IReadOnlyList<T> enemies, out int targetIndex)
            where T : Entity
        {
            targetIndex = -1;

            if (enemies == null || enemies.Count == 0)
                return false;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];

                if (!CanBeDirectTarget(enemy))
                    continue;

                var buffModule = enemy.GetModule<BuffModule>();
                if (buffModule == null)
                    continue;

                if (!buffModule.HasBuff(BuffType.AGGRAVATION_BUFF))
                    continue;

                targetIndex = i;
                return true;
            }

            return false;
        }
        
        public static bool CanBeDirectTarget(Entity entity, bool logBlockedTarget = false)
        {
            if (entity == null || entity.IsDead)
                return false;

            if (TryGetPhaseBreakController(entity, out EnemyPhaseBreakController phaseBreakController) &&
                phaseBreakController.BlocksDirectTarget)
            {
                if (logBlockedTarget)
                    RaisePhaseBreakTargetBlockLog(entity, true);

                return false;
            }

            var buffModule = entity.GetModule<BuffModule>();

            if (buffModule != null && buffModule.TryGetDirectTargetBlockBuff(out var blockBuff))
            {
                if (logBlockedTarget)
                {
                    RaiseTargetBlockLog(entity, blockBuff, true);
                }

                return false;
            }

            return true;
        }

        public static bool CanBeRangeTarget(Entity entity, bool logBlockedTarget = false)
        {
            if (entity == null || entity.IsDead)
                return false;

            if (TryGetPhaseBreakController(entity, out EnemyPhaseBreakController phaseBreakController) &&
                phaseBreakController.BlocksRangeTarget)
            {
                if (logBlockedTarget)
                    RaisePhaseBreakTargetBlockLog(entity, false);

                return false;
            }

            var buffModule = entity.GetModule<BuffModule>();

            if (buffModule != null && buffModule.TryGetRangeTargetBlockBuff(out var blockBuff))
            {
                if (logBlockedTarget)
                {
                    RaiseTargetBlockLog(entity, blockBuff, false);
                }

                return false;
            }

            return true;
        }

        private static void RaiseTargetBlockLog(Entity entity, BuffVisualSO blockBuff, bool isDirectTarget)
        {
            if (entity == null) return;

            SystemLogOwner owner = entity is BattlePlayer
                ? SystemLogOwner.Player
                : SystemLogOwner.Enemy;

            string targetName = SystemLogNameResolver.GetTargetName(entity, owner, "플레이어");
            string buffName = blockBuff != null ? blockBuff.buffName : "특수 효과";

            string message = isDirectTarget
                ? $"{targetName}은/는 {buffName} 효과로 대상 선택이 불가능합니다."
                : $"{targetName}은/는 {buffName} 효과로 공격을 받지 않았습니다.";

            Bus<EvadeEvent>.Raise(new EvadeEvent(blockBuff, entity));
            Bus<SystemLogEvent>.Raise(new SystemLogEvent(message, owner));

            DamageData evadeData = new DamageData(0f, Elemental.Normal)
            {
                Info = "회피!"
            };

            Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(entity.transform.position, evadeData));
        }

        private static bool TryGetPhaseBreakController(Entity entity, out EnemyPhaseBreakController controller)
        {
            controller = null;

            BattleEnemy battleEnemy = entity as BattleEnemy;
            if (battleEnemy == null)
                return false;

            controller = battleEnemy.GetComponent<EnemyPhaseBreakController>();
            return controller != null;
        }

        private static void RaisePhaseBreakTargetBlockLog(Entity entity, bool isDirectTarget)
        {
            if (entity == null) return;

            SystemLogOwner owner = entity is BattlePlayer
                ? SystemLogOwner.Player
                : SystemLogOwner.Enemy;

            string targetName = SystemLogNameResolver.GetTargetName(entity, owner, "플레이어");
            string message = isDirectTarget
                ? $"{targetName}은/는 페이즈 전환 준비 중이라 대상 선택이 불가능합니다."
                : $"{targetName}은/는 페이즈 전환 준비 중이라 공격을 받지 않았습니다.";

            DamageData evadeData = new DamageData(0f, Elemental.Normal)
            {
                Info = "스턴!"
            };

            Bus<EvadeEvent>.Raise(new EvadeEvent(null, entity));
            Bus<SystemLogEvent>.Raise(new SystemLogEvent(message, owner));
            Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(entity.transform.position, evadeData));
        }
        
    }
}
