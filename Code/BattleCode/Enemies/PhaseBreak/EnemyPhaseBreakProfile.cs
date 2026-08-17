using System;
using UnityEngine;
using YIS.Code.Feedbacks;

namespace PSB.Code.BattleCode.Enemies.PhaseBreak
{
    [CreateAssetMenu(fileName = "EnemyPhaseBreakProfile", menuName = "SO/Enemy/Phase Break Profile", order = 130)]
    public class EnemyPhaseBreakProfile : ScriptableObject
    {
        //페이즈 브레이크 설정은 연출, 타겟 차단, 타이밍만, STUN/WAKE 애니메이션은 BT 노드에서 처리
        [Header("State")]
        [SerializeField] private bool isEnabled = true; //이 Profile을 실제 PhaseBreak에 사용할지 켜고 끄는 값
        
        [SerializeField] private bool blockDirectTarget = true; //스턴 중인 적을 단일 대상 선택에서 제외할지 결정
        [SerializeField] private bool blockRangeTarget = true; //스턴 중인 적을 범위 피해 대상에서도 제외할지 결정
        
        //Wake 때 새 페이즈 HP UI가 차오르는 애니메이션 완료까지 기다릴지 결정
        [SerializeField] private bool waitForHpFill = true;

        [Header("Timings")]
        //Wake 애니메이션, 연출이 최소로 유지될 시간
        [SerializeField, Min(0f)] private float wakeDuration = 0.25f;
        //HP 회복 후 행동 시작 전에 마무리 피드백을 보여줄 시간
        [SerializeField, Min(0f)] private float finishDuration = 0.2f;

        [Header("Feedbacks")]
        [SerializeField] private EnemyPhaseBreakFeedbackEntry[] stunFeedbacks;
        [SerializeField] private EnemyPhaseBreakFeedbackEntry[] wakeFeedbacks;
        [SerializeField] private EnemyPhaseBreakFeedbackEntry[] finishFeedbacks;

        public bool IsEnabled => isEnabled;
        public bool BlockDirectTarget => blockDirectTarget;
        public bool BlockRangeTarget => blockRangeTarget;
        public bool WaitForHpFill => waitForHpFill;
        public float WakeDuration => wakeDuration;
        public float FinishDuration => finishDuration;
        public EnemyPhaseBreakFeedbackEntry[] StunFeedbacks => stunFeedbacks;
        public EnemyPhaseBreakFeedbackEntry[] WakeFeedbacks => wakeFeedbacks;
        public EnemyPhaseBreakFeedbackEntry[] FinishFeedbacks => finishFeedbacks;
    }

    [Serializable]
    public class EnemyPhaseBreakFeedbackEntry
    {
        [SerializeField] private FeedbackPlayer feedbackPrefab; //실제로 생성하고 재생할 FeedbackPlayer 프리팹
        [SerializeField] private bool parentToEnemy = true; //true면 피드백을 적 하위에 붙여서 적 이동을 따라가게
        
        public FeedbackPlayer FeedbackPrefab => feedbackPrefab; //컨트롤러가 피드백 프리팹을 읽기 위한 값
        public bool ParentToEnemy => parentToEnemy; //컨트롤러가 부모 지정 여부를 읽기 위한 값
    }
    
}
