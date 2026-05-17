using System.Threading.Tasks;
using CIW.Code;
using Work.YIS.Code.Buffs;
using YIS.Code.Modules;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public enum BuffConditionType
    {
        HasAnyBuff,
        HasSpecificBuff,
        HasNoBuff
    }
    
    public class BuffConditionSkillAction : ISkillAction
    {
        private Entity _target;
        private BuffConditionType _conditionType;
        private BuffType _specificBuffType;
        private ISkillAction _onTrueAction;
        private ISkillAction _onFalseAction;

        public BuffConditionSkillAction(Entity target, BuffConditionType conditionType, 
            BuffType specificBuffType, ISkillAction onTrueAction, ISkillAction onFalseAction = null)
        {
            _target = target;
            _conditionType = conditionType;
            _specificBuffType = specificBuffType;
            _onTrueAction = onTrueAction;
            _onFalseAction = onFalseAction;
        }

        public async Task ExecuteAsync()
        {
            if (_target == null) return;

            BuffModule buffModule = _target.GetModule<BuffModule>();
            bool isConditionMet = false;

            if (buffModule != null)
            {
                var activeBuffs = buffModule.GetRawActiveBuffs();
                
                if (_conditionType == BuffConditionType.HasAnyBuff)
                {
                    isConditionMet = activeBuffs.Count > 0;
                }
                else if (_conditionType == BuffConditionType.HasNoBuff)
                {
                    isConditionMet = activeBuffs.Count == 0;
                }
                else if (_conditionType == BuffConditionType.HasSpecificBuff)
                {
                    foreach (var buff in activeBuffs)
                    {
                        if (buff.BuffKey == (int)_specificBuffType)
                        {
                            isConditionMet = true;
                            break;
                        }
                    }
                }
            }

            if (isConditionMet && _onTrueAction != null)
            {
                await _onTrueAction.ExecuteAsync();
            }
            else if (!isConditionMet && _onFalseAction != null)
            {
                await _onFalseAction.ExecuteAsync();
            }
        }
        
    }
}