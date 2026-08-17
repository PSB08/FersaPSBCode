using System.Threading.Tasks;
using CIW.Code;
using Work.YIS.Code.Buffs;
using YIS.Code.Modules;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public class BuffRemoveSkillAction : ISkillAction
    {
        private Entity _target;
        private BuffType _buffTypeToRemove;
        private bool _removeAllStacks; 

        public BuffRemoveSkillAction(Entity target, BuffType buffTypeToRemove, bool removeAllStacks = true)
        {
            _target = target;
            _buffTypeToRemove = buffTypeToRemove;
            _removeAllStacks = removeAllStacks;
        }

        public async Task ExecuteAsync()
        {
            if (_target == null) return;

            BuffModule buffModule = _target.GetModule<BuffModule>();
            if (buffModule == null) return;

            if (_removeAllStacks)
            {
                buffModule.ForceClearBuff(_buffTypeToRemove);
            }
            else
            {
                var activeBuffs = buffModule.GetRawActiveBuffs();
                foreach (var buffInfo in activeBuffs)
                {
                    if (buffInfo.BuffKey == (int)_buffTypeToRemove)
                    {
                        buffModule.BuffRemover(buffInfo);
                        break;
                    }
                }
            }

            await Task.CompletedTask;
        }
        
    }
}