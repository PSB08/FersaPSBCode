using System;
using Code.Scripts.Enemies.BT;
using Code.Scripts.Entities;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ChangeClip", story: "change to new [Clip] in [Renderer]", category: "Action", id: "98d5b3d077bb375086da1c7436726cad")]
    public partial class ChangeClipAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<AnimParamSO> Clip;
        [SerializeReference] public BlackboardVariable<EntityRenderer> Renderer;

        protected override Status OnStart()
        {
            if (Renderer?.Value == null || Clip?.Value == null)
                return Status.Failure;

            //BT 상태 전환은 같은 클립이어도 Hit, End 이벤트를 다시 받을 수 있게 0초부터 재생
            Renderer.Value.RestartClip(Clip.Value);
            return Status.Success;
        }
        
    }
}

