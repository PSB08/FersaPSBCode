using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public class SequenceSkillAction : ISkillAction
    {
        private readonly IReadOnlyList<ISkillAction> _actions;

        public SequenceSkillAction(IReadOnlyList<ISkillAction> actions)
        {
            _actions = actions;
        }

        public async Task ExecuteAsync()
        {
            if (_actions == null) return;

            foreach (ISkillAction action in _actions)
            {
                if (action == null) continue;

                await action.ExecuteAsync();
            }
        }
        
    }
}