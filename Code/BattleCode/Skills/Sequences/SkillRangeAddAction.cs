using System.Threading.Tasks;
using UnityEngine;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public class SkillRangeAddAction : ISkillAction
    {
        private readonly SkillDataSO _targetSkill;
        private readonly int _changeRange;

        public SkillRangeAddAction(SkillDataSO targetSkill, int changeRange)
        {
            _targetSkill = targetSkill;
            _changeRange = changeRange;
        }
        
        public async Task ExecuteAsync()
        {
            _targetSkill.range = Mathf.Max(0, _targetSkill.range + _changeRange);
            await Task.CompletedTask;
        }
        
    }
}