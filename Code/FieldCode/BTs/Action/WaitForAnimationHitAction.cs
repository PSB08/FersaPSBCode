using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.FieldCode.BTs;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForAnimationHit", story: "wait for animation hit in [Combat]", category: "Action", id: "3409cb9c0c2edcb8657564d92aa6eee3")]
    public partial class WaitForAnimationHitAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<CombatAnimationContext> Combat;
        [SerializeField, Min(0f)] private float timeout = 1.2f;

        private float _elapsed;

        protected override Status OnStart()
        {
            if (Combat?.Value == null)
                return Status.Failure;

            //애니메이션 이벤트가 누락돼도 BT가 영구 Running에 갇히지 않도록 경과 시간을 초기화
            _elapsed = 0f;

            return Combat.Value.HitTriggered ? Status.Success : Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Combat?.Value == null)
                return Status.Failure;

            if (Combat.Value.HitTriggered)
                return Status.Success;

            if (timeout > 0f)
            {
                _elapsed += Time.deltaTime;
                if (_elapsed >= timeout)
                {
                    //Hit 이벤트가 없던 클립이어도 다음 공격 처리 노드로 넘어가 턴 종료가 보장
                    Debug.LogWarning("[WaitForAnimationHitAction] Animation hit event timeout. Force continuing BT flow.");
                    return Status.Success;
                }
            }

            return Status.Running;
        }
        
    }
}

