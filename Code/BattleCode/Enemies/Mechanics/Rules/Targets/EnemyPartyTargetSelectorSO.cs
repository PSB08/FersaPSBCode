using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Players;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Targets
{
    [CreateAssetMenu(fileName = "EnemyPartyTargetSelector", menuName = "SO/Enemy/Mechanics/Targets/Party", order = 135)]
    public class EnemyPartyTargetSelectorSO : EnemyMechanicTargetSelectorSO
    {
        public override void CollectTargets(EnemyMechanicExecutionContext executionContext,
            List<Entity> targets)
        {
            PlayerManager manager = executionContext.Mechanics.PlayerManager;
            IReadOnlyList<Entity> partyTargets = manager != null ? manager.PartyTargets : null;

            if (partyTargets == null)
                return;

            for (int i = 0; i < partyTargets.Count; i++)
            {
                Entity target = partyTargets[i];

                if (target == null || target.IsDead || !target.gameObject.activeInHierarchy)
                    continue;

                targets.Add(target);
            }
        }
    }
}
