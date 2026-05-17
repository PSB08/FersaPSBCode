using System;
using PSB.Code.BattleCode.Feedbacks;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "DeadFeedbackPlayer", story: "[Feedback] play enemy", category: "Action", id: "789797ae4524b38aa43829c1ac039262")]
    public partial class DeadFeedbackPlayerAction : Action
    {
        [SerializeReference] public BlackboardVariable<DeadShaderFeedback> Feedback;

        private bool _isFinished;

        protected override Status OnStart()
        {
            _isFinished = false;

            if (Feedback == null || Feedback.Value == null)
            {
                return Status.Success;
            }

            Feedback.Value.PlayFeedback(() => 
            {
                _isFinished = true;
            });

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_isFinished)
            {
                return Status.Success;
            }
            
            return Status.Running;
        }
        
    }
}

