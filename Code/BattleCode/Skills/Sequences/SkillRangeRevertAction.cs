using System.Threading.Tasks;
using UnityEngine;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public class SkillRangeRevertAction : ISkillAction
    {
        private readonly SkillDataSO _skillData;
        private readonly int _originalRange;

        public SkillRangeRevertAction(SkillDataSO skillData, int originalRange)
        {
            _skillData = skillData;
            _originalRange = originalRange;
        }
        
        public async Task ExecuteAsync()
        {
            _skillData.range = _originalRange;
            await Task.CompletedTask;
        }
        
    }
}