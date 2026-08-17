using System.Collections.Generic;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Players;
using UnityEngine;

namespace PSB.Code.BattleCode.Allies.AttackCode
{
    public static class AllyTargetSelector
    {
        public static bool TrySelectTarget(BattleEnemyManager enemyManager, float estimatedDamage,
            out BattleEnemy target, out int targetIndex, out string reason)
        {
            target = null;
            targetIndex = -1;
            reason = string.Empty;

            IReadOnlyList<BattleEnemy> candidates = enemyManager != null ? enemyManager.GetEnemies() : null;
            if (candidates == null || candidates.Count == 0)
            {
                reason = "사용 가능한 적이 없습니다.";
                return false;
            }

            if (SkillTargetingUtil.TryGetTauntTargetIndex(candidates, out int tauntIndex))
            {
                target = candidates[tauntIndex];
                targetIndex = tauntIndex;
                reason = "도발 또는 강제 대상";
                return target != null;
            }

            BattleEnemy firstValid = null;
            int firstValidIndex = -1;
            BattleEnemy firstBlocked = null;
            int firstBlockedIndex = -1;
            BattleEnemy lethalTarget = null;
            int lethalIndex = -1;
            BattleEnemy weakestTarget = null;
            int weakestIndex = -1;
            float lowestLethalHealth = float.PositiveInfinity;
            float lowestHealthRatio = float.PositiveInfinity;
            float lowestCurrentHealth = float.PositiveInfinity;
            float weakestMaxHealth = 1f;

            for (int i = 0; i < candidates.Count; i++)
            {
                BattleEnemy candidate = candidates[i];
                if (!IsAliveTarget(candidate))
                    continue;

                if (!SkillTargetingUtil.CanBeDirectTarget(candidate))
                {
                    if (firstBlocked == null)
                    {
                        firstBlocked = candidate;
                        firstBlockedIndex = i;
                    }

                    continue;
                }

                if (firstValid == null)
                {
                    firstValid = candidate;
                    firstValidIndex = i;
                }

                EntityHealth health = candidate.GetModule<EntityHealth>();
                if (health == null)
                    continue;

                float currentHealth = Mathf.Max(0f, health.CurrentHealth);
                float maxHealth = Mathf.Max(1f, health.MaxHealth);
                float healthRatio = currentHealth / maxHealth;

                if (estimatedDamage > 0f && estimatedDamage >= currentHealth && currentHealth < lowestLethalHealth)
                {
                    lowestLethalHealth = currentHealth;
                    lethalTarget = candidate;
                    lethalIndex = i;
                }

                if (currentHealth < lowestCurrentHealth ||
                    (Mathf.Approximately(currentHealth, lowestCurrentHealth) && healthRatio < lowestHealthRatio))
                {
                    lowestHealthRatio = healthRatio;
                    lowestCurrentHealth = currentHealth;
                    weakestMaxHealth = maxHealth;
                    weakestTarget = candidate;
                    weakestIndex = i;
                }
            }

            if (lethalTarget != null)
            {
                target = lethalTarget;
                targetIndex = lethalIndex;
                reason = $"처치 가능 : 예상 피해 {Mathf.RoundToInt(estimatedDamage)} / 현재 체력 {Mathf.RoundToInt(lowestLethalHealth)}";
                return true;
            }

            if (weakestTarget != null)
            {
                target = weakestTarget;
                targetIndex = weakestIndex;
                reason = $"현재 체력 낮음 : {Mathf.RoundToInt(lowestCurrentHealth)} / {Mathf.RoundToInt(weakestMaxHealth)}";
                return true;
            }

            if (firstValid != null)
            {
                target = firstValid;
                targetIndex = firstValidIndex;
                reason = "첫 번째 유효 대상";
                return true;
            }

            if (firstBlocked != null)
            {
                target = firstBlocked;
                targetIndex = firstBlockedIndex;
                reason = "직접 지정 불가 상태지만 공격 시도";
                return true;
            }

            reason = "직접 지정 가능한 대상이 없습니다.";
            return false;
        }

        private static bool IsAliveTarget(BattleEnemy target)
        {
            return target != null &&
                   !target.IsDead;
        }
    }
}
