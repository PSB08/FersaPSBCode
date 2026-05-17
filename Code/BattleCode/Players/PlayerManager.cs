using PSB_Lib.Dependencies;
using UnityEngine;

namespace PSB.Code.BattleCode.Players
{
    [DefaultExecutionOrder(-6)]
    [Provide]
    public class PlayerManager : MonoBehaviour, IDependencyProvider
    {
        public BattlePlayer BattlePlayer { get; private set; }

        public void SetPlayer(BattlePlayer player)
        {
            BattlePlayer = player;
        }

        public void ClearPlayer(BattlePlayer player)
        {
            if (BattlePlayer == player)
                BattlePlayer = null;
        }
        
    }
}