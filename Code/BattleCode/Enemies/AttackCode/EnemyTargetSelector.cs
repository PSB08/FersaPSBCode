using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Players;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public static class EnemyTargetSelector
    {
        public static bool TrySelectTarget(PlayerManager playerManager, BattleAllyManager allyManager, float estimatedDamage,
            out Entity target, out string reason)
        {
            target = null;
            reason = string.Empty;
            
            List<Entity> candidates = BuildCandidates(playerManager, allyManager);
            if (candidates.Count == 0)
            {
                reason = "후보 대상이 없습니다.";
                return false;
            }
            
            if (SkillTargetingUtil.TryGetTauntTargetIndex(candidates, out int tauntIndex))
            {
                target = candidates[tauntIndex];
                reason = "도발 또는 강제 대상";
                return target != null;
            }
            
            Entity firstValid = null;
            Entity lethalTarget = null;
            Entity weakestTarget = null;
            
            float lowestLethalHealth = float.PositiveInfinity;
            float lowestHealthRatio = float.PositiveInfinity;
            float lowestCurrentHealth = float.PositiveInfinity;
            float weakestMaxHealth = 1f;
            
            for (int i = 0; i < candidates.Count; i++)
            {
                Entity candidate = candidates[i];
                if (!IsValidTarget(candidate))
                    continue;
                
                if (firstValid == null)
                    firstValid = candidate;
                
                EntityHealth health = candidate.GetModule<EntityHealth>();
                if (health == null)
                    continue;
                
                float currentHealth = Mathf.Max(0f, health.CurrentHealth);
                float maxHealth = Mathf.Max(1f, health.MaxHealth);
                float healthRatio = currentHealth / maxHealth;
                
                if (currentHealth < lowestCurrentHealth ||
                    (Mathf.Approximately(currentHealth, lowestCurrentHealth) && healthRatio < lowestHealthRatio))
                {
                    lowestHealthRatio = healthRatio;
                    lowestCurrentHealth = currentHealth;
                    weakestMaxHealth = maxHealth;
                    weakestTarget = candidate;
                }
                
                if (estimatedDamage <= 0f || estimatedDamage < currentHealth)
                    continue;
                
                if (currentHealth >= lowestLethalHealth)
                    continue;
                
                lowestLethalHealth = currentHealth;
                lethalTarget = candidate;
            }
            
            if (lethalTarget != null)
            {
                target = lethalTarget;
                reason = $"처치 가능 : 예상 피해 {Mathf.RoundToInt(estimatedDamage)} / 현재 체력 {Mathf.RoundToInt(lowestLethalHealth)}";
                return true;
            }
            
            if (weakestTarget != null)
            {
                target = weakestTarget;
                reason = $"현재 체력 낮음 : {Mathf.RoundToInt(lowestCurrentHealth)} / {Mathf.RoundToInt(weakestMaxHealth)}";
                return true;
            }
            
            if (firstValid == null)
            {
                return TrySelectBlockedFallback(candidates, out target, out reason);
            }
            
            target = firstValid;
            reason = "첫 번째 유효 대상";
            return target != null;
        }
        
        private static List<Entity> BuildCandidates(PlayerManager playerManager, BattleAllyManager allyManager)
        {
            List<Entity> result = new();
            
            IReadOnlyList<Entity> partyTargets = playerManager != null ? playerManager.PartyTargets : null;
            if (partyTargets != null)
            {
                for (int i = 0; i < partyTargets.Count; i++)
                    AddUnique(result, partyTargets[i]);
            }
            
            if (playerManager != null)
                AddUnique(result, playerManager.BattlePlayer);

            IReadOnlyList<BattleAlly> allies = allyManager != null ? allyManager.GetAllies() : null;
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                    AddUnique(result, allies[i]);
            }
            
            return result;
        }
        
        private static void AddUnique(List<Entity> targets, Entity target)
        {
            if (target == null || targets.Contains(target))
                return;
            
            targets.Add(target);
        }
        
        private static bool IsValidTarget(Entity target)
        {
            return IsAliveTarget(target) &&
                   SkillTargetingUtil.CanBeDirectTarget(target);
        }
        
        private static bool TrySelectBlockedFallback(List<Entity> candidates, out Entity target, out string reason)
        {
            target = null;
            reason = "직접 지정 가능한 대상이 없습니다.";
            
            float lowestCurrentHealth = float.PositiveInfinity;
            float lowestHealthRatio = float.PositiveInfinity;
            
            for (int i = 0; i < candidates.Count; i++)
            {
                Entity candidate = candidates[i];
                if (!IsAliveTarget(candidate))
                    continue;
                
                EntityHealth health = candidate.GetModule<EntityHealth>();
                if (health == null)
                {
                    if (target == null)
                        target = candidate;
                    
                    continue;
                }
                
                float currentHealth = Mathf.Max(0f, health.CurrentHealth);
                float maxHealth = Mathf.Max(1f, health.MaxHealth);
                float healthRatio = currentHealth / maxHealth;
                
                if (currentHealth >= lowestCurrentHealth &&
                    !Mathf.Approximately(currentHealth, lowestCurrentHealth))
                    continue;
                
                if (Mathf.Approximately(currentHealth, lowestCurrentHealth) && healthRatio >= lowestHealthRatio)
                    continue;
                
                lowestCurrentHealth = currentHealth;
                lowestHealthRatio = healthRatio;
                target = candidate;
            }
            
            if (target == null)
                return false;
            
            reason = "직접 지정 불가 상태지만 공격 시도";
            return true;
        }
        
        private static bool IsAliveTarget(Entity target)
        {
            return target != null &&
                   target.gameObject.activeInHierarchy &&
                   !target.IsDead;
        }
        
    }
}
