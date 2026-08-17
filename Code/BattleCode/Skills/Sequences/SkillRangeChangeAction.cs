using System.Threading.Tasks;
using UnityEngine;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public class SkillRangeChangeAction : ISkillAction
    {
        private readonly SkillDataSO _targetSkill;
        private readonly int _changeRange;

        public SkillRangeChangeAction(SkillDataSO targetSkill, int changeRange)
        {
            _targetSkill = targetSkill;
            _changeRange = changeRange;
        }
        
        public async Task ExecuteAsync()
        {
            _targetSkill.range = Mathf.Max(0, _changeRange);
            await Task.CompletedTask;
        }
        
    }
}