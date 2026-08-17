using System.Collections.Generic;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    [CreateAssetMenu(fileName = "EnemyMechanicSet", menuName = "SO/Enemy/Mechanics/MechanicSet", order = 90)]
    public class EnemyMechanicSetSO : ScriptableObject
    {
        [SerializeField] private EnemyMechanicSO[] mechanics;
        
        public EnemyMechanicSO[] Mechanics => mechanics;
        public bool HasMechanics
        {
            get
            {
                if (mechanics == null)
                    return false;
                
                for (int i = 0; i < mechanics.Length; i++)
                {
                    if (mechanics[i] != null)
                        return true;
                }
                
                return false;
            }
        }
        
        public EnemyMechanicValidationReport ValidateFor(EnemySO enemySO)
        {
            EnemyMechanicValidationReport report = new EnemyMechanicValidationReport();
            HashSet<EnemyMechanicSO> registered = new HashSet<EnemyMechanicSO>();
            
            if (!HasMechanics)
            {
                report.AddError($"{name}의 Mechanics에 유효한 기믹이 없습니다.");
                return report;
            }
            
            for (int i = 0; i < mechanics.Length; i++)
            {
                EnemyMechanicSO mechanic = mechanics[i];
                
                if (mechanic == null)
                {
                    report.AddWarning($"{name}의 Mechanics[{i}]가 비어있습니다.");
                    continue;
                }
                
                if (!registered.Add(mechanic))
                    report.AddWarning($"{name}에 {mechanic.name} 기믹이 중복 연결되어 있습니다.");
                
                mechanic.Validate(enemySO, report);
            }
            
            return report;
        }
        
    }
}
