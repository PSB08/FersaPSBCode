using System.Collections;

namespace PSB.Code.BattleCode.Enemies.Mechanics.ActionGates
{
    public interface IEnemyActionGate
    {
        public int ActionGatePriority { get; }
        
        public bool ShouldResolveBeforeAction(EnemyActionGateContext context);
        public IEnumerator ResolveBeforeAction(EnemyActionGateContext context);
    }
}
