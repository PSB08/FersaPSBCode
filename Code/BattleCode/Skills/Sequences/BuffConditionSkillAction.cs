using System;
using System.Threading.Tasks;
using CIW.Code;
using Work.YIS.Code.Buffs;
using YIS.Code.Modules;
using YIS.Code.Skills.Sequences;
using Random = UnityEngine.Random;

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
        
        private ISkillAction[] _onTrueActions;
        private ISkillAction _onFalseAction;
        
        private Func<BuffType, ISkillAction[]> _onTrueDynamicActions;

        public BuffConditionSkillAction(Entity target, BuffConditionType conditionType, 
            BuffType specificBuffType, ISkillAction onTrueAction, ISkillAction onFalseAction = null)
        {
            _target = target;
            _conditionType = conditionType;
            _specificBuffType = specificBuffType;
            _onTrueActions = onTrueAction != null ? new ISkillAction[] { onTrueAction } : null;
            _onFalseAction = onFalseAction;
        }

        public BuffConditionSkillAction(Entity target, BuffConditionType conditionType, 
            Func<BuffType, ISkillAction[]> onTrueDynamicActions)
        {
            _target = target;
            _conditionType = conditionType;
            _onTrueDynamicActions = onTrueDynamicActions;
        }

        public async Task ExecuteAsync()
        {
            if (_target == null) return;

            BuffModule buffModule = _target.GetModule<BuffModule>();
            bool isConditionMet = false;
            BuffType foundBuffType = (BuffType)0;

            if (buffModule != null)
            {
                var activeBuffs = buffModule.GetRawActiveBuffs();
                
                if (_conditionType == BuffConditionType.HasAnyBuff && activeBuffs.Count > 0)
                {
                    isConditionMet = true;
                    int randomIndex = Random.Range(0, activeBuffs.Count);
                    foundBuffType = (BuffType)activeBuffs[randomIndex].BuffKey;
                }
                else if (_conditionType == BuffConditionType.HasNoBuff && activeBuffs.Count == 0)
                {
                    isConditionMet = true;
                }
                else if (_conditionType == BuffConditionType.HasSpecificBuff)
                {
                    foreach (var buff in activeBuffs)
                    {
                        if (buff.BuffKey == (int)_specificBuffType)
                        {
                            isConditionMet = true;
                            foundBuffType = _specificBuffType;
                            break;
                        }
                    }
                }
            }

            if (isConditionMet)
            {
                if (_onTrueActions != null)
                {
                    foreach (var action in _onTrueActions)
                    {
                        if (action != null) await action.ExecuteAsync();
                    }
                }

                if (_onTrueDynamicActions != null)
                {
                    var dynamicActions = _onTrueDynamicActions(foundBuffType);
                    foreach (var action in dynamicActions)
                    {
                        if (action != null) await action.ExecuteAsync();
                    }
                }
            }
            else
            {
                if (_onFalseAction != null)
                {
                    await _onFalseAction.ExecuteAsync();
                }
            }
        }
        
    }
}