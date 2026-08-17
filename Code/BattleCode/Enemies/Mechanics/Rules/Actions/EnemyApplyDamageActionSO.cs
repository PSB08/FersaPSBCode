using CIW.Code;
using PSB.Code.BattleCode.Entities;
using System.Collections.Generic;
using UnityEngine;
using YIS.Code.Combat;
using YIS.Code.Defines;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Actions
{
    [CreateAssetMenu(fileName = "EnemyApplyDamageAction", menuName = "SO/Enemy/Mechanics/Actions/ApplyDamage", order = 142)]
    public class EnemyApplyDamageActionSO : EnemyMechanicActionSO
    {
        [SerializeField, Min(0f)] private float damage = 1f;
        [SerializeField] private Elemental elemental = Elemental.Normal;
        [SerializeField] private string info = "기믹 피해";
        
        public override EnemyMechanicActionRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        private sealed class Runtime : EnemyMechanicActionRuntime
        {
            private readonly EnemyApplyDamageActionSO _so;
            
            public Runtime(EnemyMechanicContext context, EnemyApplyDamageActionSO so) : base(context)
            {
                _so = so;
            }
            
            public override void Execute(EnemyMechanicExecutionContext executionContext, IReadOnlyList<Entity> targets)
            {
                foreach (Entity target in targets)
                {
                    if (target == null || target.IsDead || !target.gameObject.activeInHierarchy)
                        continue;
                    
                    EntityHealth health = target.GetComponentInChildren<EntityHealth>(true);
                    if (health == null || !health.IsInitialized)
                        continue;
                    
                    DamageData damageData = new DamageData(_so.damage, _so.elemental, _so.info);
                    health.ApplyFixedDamage(damageData);
                }
            }
        }
        
    }
}
