using System.Collections.Generic;
using CIW.Code;
using PSB_Lib.Dependencies;
using UnityEngine;

namespace PSB.Code.BattleCode.Players
{
    [DefaultExecutionOrder(-6)]
    [Provide]
    public class PlayerManager : MonoBehaviour, IDependencyProvider
    {
        private readonly List<Entity> _partyTargets = new();

        public BattlePlayer BattlePlayer { get; private set; }
        public IReadOnlyList<Entity> PartyTargets => _partyTargets;

        public void SetPlayer(BattlePlayer player)
        {
            if (BattlePlayer != null && BattlePlayer != player)
                UnregisterPartyTarget(BattlePlayer);

            BattlePlayer = player;
            RegisterPartyTarget(player);
        }

        public void ClearPlayer(BattlePlayer player)
        {
            UnregisterPartyTarget(player);

            if (BattlePlayer == player)
                BattlePlayer = null;
        }

        public void RegisterPartyTarget(Entity target)
        {
            if (target == null || _partyTargets.Contains(target))
                return;

            _partyTargets.Add(target);
        }

        public void UnregisterPartyTarget(Entity target)
        {
            if (target == null)
                return;

            _partyTargets.Remove(target);
        }
        
    }
}
