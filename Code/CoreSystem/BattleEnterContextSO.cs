using System;
using UnityEngine;
using Work.CSH.Code.Battle;

namespace Work.PSB.Code.CoreSystem
{
    public enum BattleEnterBy { Player, Enemy, Npc }
    
    [CreateAssetMenu(fileName = "BattleEnterContextSO", menuName = "SO/BattleEnterContext", order = 150)]
    public class BattleEnterContextSO : ScriptableObject
    {
        public bool HasRequest;
        public BattleEnterBy EnterBy;

        public void Set(BattleEnterBy by)
        {
            HasRequest = true;
            EnterBy = by;
        }

        public bool TryConsume(out BattleEnterBy by)
        {

            if (!HasRequest)
            {
                by = default;
                return false;
            }

            by = EnterBy;
            HasRequest = false;
            return true;
        }
    }
}