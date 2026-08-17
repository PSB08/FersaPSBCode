using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Phases
{
    [Serializable]
    public class EnemyPhaseDefinition
    {
        public string phaseName;
        
        [Range(0.01f, 0.99f)]
        public float hpThresholdPercent = 0.5f;
        
        public EnemyMechanicActionSO[] actions;
    }
}
