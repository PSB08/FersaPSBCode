using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Actions
{
    [CreateAssetMenu(fileName = "EnemyChangeSkillsAction", menuName = "SO/Enemy/Mechanics/Actions/ChangeSkills", order = 140)]
    public class EnemyChangeSkillsActionSO : EnemyMechanicActionSO
    {
        [SerializeField] private SkillDataSO[] skills;
        
        public override EnemyMechanicActionRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
            if (skills == null || skills.Length == 0)
                report.AddError($"{ownerName}에서 사용하는 {name}에 스킬이 없습니다.");
        }
        
        private sealed class Runtime : EnemyMechanicActionRuntime
        {
            private readonly EnemyChangeSkillsActionSO _so;
            
            public Runtime(EnemyMechanicContext context,
                EnemyChangeSkillsActionSO so) : base(context)
            {
                _so = so;
            }
            
            public override void Execute(EnemyMechanicExecutionContext executionContext,
                IReadOnlyList<Entity> targets)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    EnemyAttack attack = targets[i] != null
                        ? targets[i].GetModule<EnemyAttack>()
                        : null;
                    
                    attack?.SetAttackSkills(_so.skills);
                }
            }
        }
        
    }
}
