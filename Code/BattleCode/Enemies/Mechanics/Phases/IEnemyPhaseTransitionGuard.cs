using System;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Phases
{
    public interface IEnemyPhaseTransitionGuard
    {
        public int PhaseTransitionPriority { get; }
        
        public bool TryHoldTransition(EnemyPhaseTransitionRequest request, Action continueTransition);
    }
}
