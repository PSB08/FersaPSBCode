using System.Collections.Generic;
using System.Threading.Tasks;
using CIW.Code;
using Work.YIS.Code.Buffs;
using YIS.Code.Modules;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public class MultiTargetBuffSkillAction : ISkillAction
    {
        private IReadOnlyList<Entity> _targets;
        private BuffType BuffType { get; set; }
        private float Value { get; set; }
        private int Duration { get; set; }

        public MultiTargetBuffSkillAction(IReadOnlyList<Entity> targets, BuffType buffType, float value, int duration)
        {
            _targets = targets;
            BuffType = buffType;
            Value = value;
            Duration = duration;
        }
        
        public async Task ExecuteAsync()
        {
            if (_targets == null || _targets.Count <= 0) return;

            foreach (var target in _targets)
            {
                if (target == null) continue;

                BuffModule buffModule = target.GetModule<BuffModule>();
                if (buffModule == null) continue;
                
                buffModule.BuffApply(BuffType, Value, Duration);
            }

            await Task.CompletedTask;
        }
        
    }
}