using System.Collections.Generic;
using CIW.Code;
using UnityEngine;
using Work.YIS.Code.Buffs;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Actions
{
    [CreateAssetMenu(fileName = "EnemyApplyBuffAction", menuName = "SO/Enemy/Mechanics/Actions/ApplyBuff", order = 144)]
    public class EnemyApplyBuffActionSO : EnemyMechanicActionSO
    {
        [SerializeField] private BuffType buffType;
        [SerializeField] private float value = 1f;
        [SerializeField, Min(1)] private int duration = 999;
        [SerializeField] private bool removeOnDetach = true;
        
        public override EnemyMechanicActionRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
            if (duration <= 0)
                report.AddError($"{ownerName}'s {name} duration must be greater than 0.");
            
            if (Mathf.Approximately(value, 0f))
                report.AddWarning($"{ownerName}'s {name} value is zero.");
        }
        
        private sealed class Runtime : EnemyMechanicActionRuntime
        {
            private readonly EnemyApplyBuffActionSO _so;
            private readonly HashSet<Entity> _appliedTargets = new();
            
            public Runtime(EnemyMechanicContext context, EnemyApplyBuffActionSO so) : base(context)
            {
                _so = so;
            }
            
            public override void Execute(EnemyMechanicExecutionContext executionContext,
                IReadOnlyList<Entity> targets)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    Entity target = targets[i];
                    if (target == null || target.IsDead) continue;
                    
                    BuffModule buffModule = ResolveBuffModule(target);
                    if (buffModule == null)
                    {
                        Debug.LogWarning($"[EnemyApplyBuffAction] {target.name} has no BuffModule.", target);
                        continue;
                    }
                    
                    buffModule.BuffApply(_so.buffType, _so.value, _so.duration);
                    _appliedTargets.Add(target);
                }
            }
            
            protected override void OnDetach()
            {
                if (_so.removeOnDetach)
                {
                    foreach (Entity target in _appliedTargets)
                    {
                        BuffModule buffModule = ResolveBuffModule(target);
                        buffModule?.ForceClearBuff(_so.buffType);
                    }
                }
                
                _appliedTargets.Clear();
            }
            
            private static BuffModule ResolveBuffModule(Entity target)
            {
                if (target == null) return null;
                return target.GetModule<BuffModule>();
            }
        }
        
    }
}
