using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.FieldCode.BTs;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForAnimationEnd", story: "wait for animation end in [Combat]", category: "Action", id: "2826c0f5da5c37c4a3b1702a6a383448")]
    public partial class WaitForAnimationEndAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<CombatAnimationContext> Combat;
        [SerializeField, Min(0f)] private float timeout = 2f;

        private float _elapsed;

        protected override Status OnStart()
        {
            if (Combat?.Value == null)
                return Status.Failure;

            //애니메이션 End 이벤트가 누락돼도 BT가 계속 멈춰 있지 않게 경과 시간을 초기화
            _elapsed = 0f;

            return Combat.Value.EndTriggered ? Status.Success : Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Combat?.Value == null)
                return Status.Failure;

            if (Combat.Value.EndTriggered)
                return Status.Success;

            if (timeout > 0f)
            {
                _elapsed += Time.deltaTime;
                if (_elapsed >= timeout)
                {
                    //End 이벤트가 안 들어와도 Return, Idle 등 다음 노드로 넘어가게 해서 턴 정지 막음
                    Debug.LogWarning("[WaitForAnimationEndAction] Animation end event timeout. Force continuing BT flow.");
                    return Status.Success;
                }
            }

            return Status.Running;
        }
        
    }
}

