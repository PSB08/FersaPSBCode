using System;
using System.Threading.Tasks;
using CIW.Code;
using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Skills;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using YIS.Code.Skills;
using Action = Unity.Behavior.Action;
using YIS.Code.CoreSystem.Skills;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ExecuteSkillAlly", story: "execute skill command on [AllyAttack] using [SkillIndex] and [Target]", category: "Action", id: "aed57cdab51ef87601e2f68a5e5a4936")]
    public partial class ExecuteSkillAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> AllyAttack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        [SerializeReference] public BlackboardVariable<Transform> Target;
        [SerializeReference] public BlackboardVariable<bool> AttackResult;
        
        private Task<BtSkillUseResult> _useTask;
        
        protected override Status OnStart()
        {
            SetResult(false);
            _useTask = null;
            
            AllyAttack attack = AllyAttack?.Value;
            if (attack == null)
                return Skip(null, BtSkillUseResult.Failed, "AllyAttack is missing.");
            
            if (attack.SkillExecutor == null)
                return Skip(attack, BtSkillUseResult.Failed, "Skill executor is missing.");
            
            SkillDataSO skillData = attack.SkillExecutor.PlannedSo;
            if (skillData == null)
                return Skip(attack, BtSkillUseResult.Failed, "Planned SkillDataSO is missing.");
            
            if (!attack.TryBuildSkillTargets(skillData, out Entity directTarget,
                    out SkillTargetCandidates candidates, out Transform facingTarget, out string reason))
            {
                ClearTarget();
                return Skip(attack, BtSkillUseResult.NoTarget, reason);
            }
            
            if (Target != null)
                Target.Value = facingTarget;
            
            if (SkillIndex != null)
                SkillIndex.Value = attack.PlannedIndex;
            
            attack.LogAiDebug($"스킬 실행 시작 : {attack.BuildPlannedDebugText()}");
            _useTask = attack.ExecutePlannedSkillAsync(directTarget, candidates);
            return FinishTask(attack);
        }
        
        protected override Status OnUpdate()
        {
            return FinishTask(AllyAttack?.Value);
        }
        
        private Status FinishTask(AllyAttack attack)
        {
            if (_useTask == null)
                return Status.Success;
            
            if (!_useTask.IsCompleted)
                return Status.Running;
            
            if (_useTask.IsCanceled || _useTask.IsFaulted)
            {
                if (_useTask.Exception != null)
                    Debug.LogException(_useTask.Exception, attack);
                
                attack?.SkillExecutor?.SetLastUseResult(BtSkillUseResult.Failed);
                SetResult(false);
                return Status.Success;
            }
            
            BtSkillUseResult result = _useTask.Result;
            bool succeeded = result == BtSkillUseResult.Success;
            
            SetResult(succeeded);
            attack?.LogAiDebug($"스킬 실행 완료 : 결과 = {result}, 계획 = {attack.BuildPlannedDebugText()}");
            return Status.Success;
        }
        
        private void ClearTarget()
        {
            if (Target != null)
                Target.Value = null;
        }
        
        private void SetResult(bool value)
        {
            if (AttackResult != null)
                AttackResult.Value = value;
        }
        
        private Status Skip(AllyAttack attack, BtSkillUseResult result, string reason)
        {
            attack?.SkillExecutor?.SetLastUseResult(result);
            
            SetResult(false);
            return Status.Success;
        }
        
    }
}
