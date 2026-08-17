using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Players;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Targets
{
    [CreateAssetMenu(fileName = "EnemyPlayerTargetSelector", menuName = "SO/Enemy/Mechanics/Targets/Player", order = 133)]
    public class EnemyPlayerTargetSelectorSO : EnemyMechanicTargetSelectorSO
    {
        public override void CollectTargets(EnemyMechanicExecutionContext executionContext,
            List<Entity> targets)
        {
            PlayerManager manager = executionContext.Mechanics.PlayerManager;
            BattlePlayer player = manager != null ? manager.BattlePlayer : null;

            if (player != null && !player.IsDead && player.gameObject.activeInHierarchy)
                targets.Add(player);
        }
    }
}
