namespace PSB.Code.BattleCode.Enemies.Mechanics.Intents
{
    public interface IEnemyIntentContributor
    {
        public bool TryProposeIntent(EnemyIntentRequest request, out EnemyIntentProposal proposal);
    }
}
