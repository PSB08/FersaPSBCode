using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Mechanics.ActionGates;
using PSB.Code.BattleCode.Enemies.Mechanics.Intents;
using PSB.Code.BattleCode.Enemies.Mechanics.Phases;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Players;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public sealed class EnemyMechanicContext
    {
        public BattleEnemy Enemy { get; }
        public EnemyAttack Attack { get; }
        public EntityHealth Health { get; }
        public BattleEnemyManager EnemyManager { get; }
        public PlayerManager PlayerManager { get; }
        public EnemyMechanicHost Host { get; }
        public EnemyBattleMechanicScope BattleScope { get; }
        public EnemyMechanicSignalHub Signals { get; }
        public EnemyIntentResolver Intents { get; }
        public EnemyPhaseTransitionPipeline PhaseTransitions { get; }
        public EnemyActionGatePipeline ActionGates { get; }
        
        public SkillDataSO[] CurrentSkills => Attack != null ? Attack.CurrentSkills : null;
        
        public EnemyMechanicContext(BattleEnemy enemy, EnemyAttack attack, EntityHealth health,
            BattleEnemyManager enemyManager, PlayerManager playerManager, EnemyMechanicHost host,
            EnemyBattleMechanicScope battleScope, EnemyMechanicSignalHub signals, EnemyIntentResolver intents,
            EnemyPhaseTransitionPipeline phaseTransitions, EnemyActionGatePipeline actionGates)
        {
            Enemy = enemy;
            Attack = attack;
            Health = health;
            EnemyManager = enemyManager;
            PlayerManager = playerManager;
            Host = host;
            BattleScope = battleScope;
            Signals = signals;
            Intents = intents;
            PhaseTransitions = phaseTransitions;
            ActionGates = actionGates;
        }
        
    }
}
