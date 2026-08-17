using System;
using PSB.Code.BattleCode.Enemies.PhaseBreak;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    //BT의 Stun 노드에서 EnemyPhaseBreakController에 있는 스턴 피드백 재생만 맡기는 액션
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlayPhaseBreakStunFeedback", story: "play phase break stun feedback for [Self]", category: "Action", id: "0a4a7f2f36f743eca2e754f7eb0f3e50")]
    public partial class PlayPhaseBreakStunFeedbackAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleEnemy> Self;

        protected override Status OnStart()
        {
            //현재 적에서 EnemyPhaseBreakController 찾기
            EnemyPhaseBreakController controller = GetController();
            if (controller == null)
            {
                return Status.Failure;
            }

            //실제 Stun 피드백 재생은 컨트롤러에 맡기기
            controller.PlayStunFromBT();
            return Status.Success;
        }

        private EnemyPhaseBreakController GetController()
        {
            return Self?.Value != null ? Self.Value.GetComponent<EnemyPhaseBreakController>() : null;
        }
    }

    //BT의 Wake 노드에서 페이즈 적용, HP UI 회복, 마무리 피드백이 끝날 때까지 기다리는 액션
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ResolvePhaseBreakWakeFeedback", story: "resolve phase break wake feedback for [Self]", category: "Action", id: "40b78d02f0c148f1b5f3e629f2c5f4d9")]
    public partial class ResolvePhaseBreakWakeFeedbackAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleEnemy> Self;

        //Running 중 매 프레임 같은 컨트롤러를 확인하기 위해 저장
        private EnemyPhaseBreakController _ctrl;

        protected override Status OnStart()
        {
            _ctrl = GetController();
            if (_ctrl == null)
            {
                return Status.Failure;
            }
            
            _ctrl.StartWakeFromBT(); //컨트롤러의 Wake 처리 코루틴을 시작
            return _ctrl.WakeDone ? DoneStatus() : Status.Running; //이미 끝난 경우 즉시 결과를 반환하고, 아니면 Running으로 기다리기
        }

        protected override Status OnUpdate()
        {
            if (_ctrl == null)
            {
                //Running 중 컨트롤러가 사라졌으면 실패 처리
                return Status.Failure;
            }

            //컨트롤러가 완료될 때까지 Running을 유지
            return _ctrl.WakeDone ? DoneStatus() : Status.Running;
        }

        private Status DoneStatus()
        {
            return _ctrl.WakeOk ? Status.Success : Status.Failure;
        }

        private EnemyPhaseBreakController GetController()
        {
            return Self?.Value != null ? Self.Value.GetComponent<EnemyPhaseBreakController>() : null;
        }
    }
    
}
