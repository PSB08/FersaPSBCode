using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Players;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Targets
{
    [CreateAssetMenu(fileName = "EnemyAllAlliesTargetSelector", menuName = "SO/Enemy/Mechanics/Targets/AllAllies", order = 134)]
    public class EnemyAllAlliesTargetSelectorSO : EnemyMechanicTargetSelectorSO
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

                if (target is not BattleAlly || target.IsDead || !target.gameObject.activeInHierarchy)
                    continue;

                targets.Add(target);
            }
        }
    }
}
