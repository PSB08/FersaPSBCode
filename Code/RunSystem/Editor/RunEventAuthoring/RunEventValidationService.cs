#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using PSW.Code.Talk;
using Work.PSB.Code.CoreSystem;

namespace Work.PSB.Code.RunSystem.Editor
{
    public enum RunEventValidationSeverity
    {
        Info, //작동에는 문제없지만 확인할 내용
        Warning, //설정 누락 등 확인이 필요한 내용
        Error //정상 작동을 막을 수 있는 잘못된 설정
    } //검사 결과의 심각도
	
    //검사 결과 한 건의 내용과 문제가 발생한 위치를 저장
    public class RunEventValidationIssue
    {
        public RunEventValidationSeverity Severity { get; } //오류, 경고, 안내 구분
        public string Message { get; } //검사 결과에 표시할 설명
        public UnityEngine.Object Target { get; } //실제 문제가 있는 SO
        public RunEventSO OwnerEvent { get; } //문제 SO가 연결된 기준 RunEvent
		
        //검사 결과의 심각도, 설명, 문제 대상과 원본 이벤트를 저장
        public RunEventValidationIssue(RunEventValidationSeverity severity, string message,
            UnityEngine.Object target, RunEventSO ownerEvent)
        {
            Severity = severity;
            Message = message;
            Target = target;
            OwnerEvent = ownerEvent;
        }
    }
	
    //RunEvent와 연결된 Talk, Choice, Action, Reward 설정을 검사
    public class RunEventValidationService
    {
        private readonly RunEventAssetRepository _repository; //전체 Action 에셋 검색에 사용
		
        //검사에 필요한 에셋 Repository를 저장
        public RunEventValidationService(RunEventAssetRepository repository)
        {
            _repository = repository;
        }
		
        //모든 RunEvent의 내부 설정과 전체 ID 중복을 검사
        public IReadOnlyList<RunEventValidationIssue> ValidateAll(IReadOnlyList<RunEventSO> events)
        {
            List<RunEventValidationIssue> issues = new List<RunEventValidationIssue>();
            IReadOnlyList<RunEventSO> validEvents = events ?? Array.Empty<RunEventSO>();
			
            for (int i = 0; i < validEvents.Count; i++)
                issues.AddRange(ValidateEvent(validEvents[i]));
			
            AddDuplicateTalkIdIssues(validEvents, issues);
            AddDuplicateActionIdIssues(issues);
            return issues;
        }
		
        //RunEvent 하나의 기본 정보, 전투, 보상, 전체 대화 연결을 검사
        public IReadOnlyList<RunEventValidationIssue> ValidateEvent(RunEventSO eventData)
        {
            List<RunEventValidationIssue> issues = new List<RunEventValidationIssue>();
            if (eventData == null)
                return issues;
			
            HashSet<string> rewardKeys = CollectRewardKeys(eventData);
            ValidateEventIdentity(eventData, issues);
            ValidateTalkStage(eventData, eventData.talkStage, "시작 대화", rewardKeys, true, issues);
            ValidateBattle(eventData, rewardKeys, issues);
            ValidateRewards(eventData, issues);
			
            //visitedTalks는 중복 검사 방지, activeTalks는 현재 경로의 순환 검사에 사용
            HashSet<TalkDataListSO> visitedTalks = new HashSet<TalkDataListSO>();
            HashSet<TalkDataListSO> activeTalks = new HashSet<TalkDataListSO>();
            ValidateTalkGraph(eventData, eventData.talkStage?.talkData,
                rewardKeys, visitedTalks, activeTalks, issues);
            ValidateTalkGraph(eventData, eventData.victoryTalkStage?.talkData,
                rewardKeys, visitedTalks, activeTalks, issues);
            ValidateTalkGraph(eventData, eventData.failureTalkStage?.talkData,
                rewardKeys, visitedTalks, activeTalks, issues);
            return issues;
        }
		
        //Talk ID, NPC Prefab, 목록 Sprite 같은 이벤트 기본 정보를 검사
        private static void ValidateEventIdentity(RunEventSO eventData,
            ICollection<RunEventValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(eventData.TalkId))
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "Talk ID가 비어 있습니다.", eventData, eventData);
            }
			
            if (eventData.entityPrefab == null)
            {
                AddIssue(issues, RunEventValidationSeverity.Warning,
                    "이벤트 NPC 프리팹이 연결되지 않았습니다.", eventData, eventData);
            }
			
            if (eventData.sprite == null)
            {
                AddIssue(issues, RunEventValidationSeverity.Info,
                    "이벤트 목록에 표시할 스프라이트가 없습니다.", eventData, eventData);
            }
        }
		
        //Encounter와 Challenge, Health End, 전투 후 대화 설정을 검사
        private static void ValidateBattle(RunEventSO eventData, HashSet<string> rewardKeys,
            ICollection<RunEventValidationIssue> issues)
        {
            if (!eventData.HasBattle)
            {
                if (eventData.battleChallengeMode != RunEventBattleChallengeMode.None ||
                    eventData.battleHealthEndMode != RunEventBattleHealthEndMode.None)
                {
                    AddIssue(issues, RunEventValidationSeverity.Error,
                        "전투 조건이 설정됐지만 Battle Encounter가 없습니다.", eventData, eventData);
                }
				
                return;
            }
			
            if (eventData.HasBattleChallenge && eventData.HasBattleHealthEndCondition)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "Battle Challenge와 Health End 조건은 동시에 사용할 수 없습니다.", eventData, eventData);
            }
			
            int enemyCount = eventData.battleEncounter.enemies != null
                ? eventData.battleEncounter.enemies.Length : 0;
			
            if (enemyCount == 0)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "Battle Encounter에 적이 없습니다.", eventData.battleEncounter, eventData);
            }
            else if (eventData.HasBattleChallenge &&
                     eventData.battleChallengeTargetEnemyIndex >= enemyCount)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "Challenge 대상 적 인덱스가 Encounter 범위를 벗어났습니다.", eventData, eventData);
            }
            else if (eventData.HasBattleHealthEndCondition &&
                     eventData.battleHealthTargetEnemyIndex >= enemyCount)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "Health End 대상 적 인덱스가 Encounter 범위를 벗어났습니다.", eventData, eventData);
            }
			
            ValidateTalkStage(eventData, eventData.victoryTalkStage,
                "전투 성공 대화", rewardKeys, false, issues);
			
            if (eventData.HasBattleChallenge)
            {
                ValidateTalkStage(eventData, eventData.failureTalkStage,
                    "전투 실패 대화", rewardKeys, false, issues);
            }
        }
		
        //TalkStage 존재 여부와 TalkData, Reward Key 연결을 검사
        private static void ValidateTalkStage(RunEventSO eventData, TalkStage stage, string stageName,
            HashSet<string> rewardKeys, bool isRequired, ICollection<RunEventValidationIssue> issues)
        {
            if (stage == null)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    $"{stageName} 설정이 없습니다.", eventData, eventData);
                return;
            }
			
            bool requiresTalk = stage.actionMode != InteractActionMode.RewardOnly;
            if ((isRequired || requiresTalk) && stage.talkData == null)
            {
                AddIssue(issues, RunEventValidationSeverity.Warning,
                    $"{stageName}에 TalkData가 연결되지 않았습니다.", eventData, eventData);
            }
			
            if (!string.IsNullOrWhiteSpace(stage.rewardKey) && !rewardKeys.Contains(stage.rewardKey))
            {
                AddIssue(issues, RunEventValidationSeverity.Warning,
                    $"{stageName}의 Reward Key '{stage.rewardKey}'와 일치하는 보상이 없습니다.",
                    eventData, eventData);
            }
        }
		
        //보상 종류마다 반드시 필요한 DropTable, Skill, Relic 등의 참조를 검사
        private static void ValidateRewards(RunEventSO eventData,
            ICollection<RunEventValidationIssue> issues)
        {
            if (eventData.rewardEntries == null)
                return;
			
            for (int i = 0; i < eventData.rewardEntries.Length; i++)
            {
                TalkRewardGiver.RewardEntry reward = eventData.rewardEntries[i];
                if (reward == null)
                {
                    AddIssue(issues, RunEventValidationSeverity.Warning,
                        $"Reward Element {i}가 비어 있습니다.", eventData, eventData);
                    continue;
                }
				
                switch (reward.rewardType)
                {
                    case TalkRewardGiver.RewardType.DropTable:
                        RequireReference(reward.dropTable, "DropTable", i, eventData, issues);
                        break;
                    case TalkRewardGiver.RewardType.GiveSkill:
                    case TalkRewardGiver.RewardType.GiveRandomSkill:
                        RequireReference(reward.skillDropTable, "SkillDropTable", i, eventData, issues);
                        break;
                    case TalkRewardGiver.RewardType.DropTableAndSkill:
                        RequireReference(reward.dropTable, "DropTable", i, eventData, issues);
                        RequireReference(reward.skillDropTable, "SkillDropTable", i, eventData, issues);
                        break;
                    case TalkRewardGiver.RewardType.ActiveObject:
                        RequireReference(reward.rewardObject, "Reward Object", i, eventData, issues);
                        break;
                    case TalkRewardGiver.RewardType.GiveRelic:
                        RequireReference(reward.rewardRelic, "Relic", i, eventData, issues);
                        break;
                    case TalkRewardGiver.RewardType.GiveRandomRelic:
                        RequireReference(reward.relicList, "Relic Database", i, eventData, issues);
                        break;
                    case TalkRewardGiver.RewardType.GiveAlly:
                        RequireReference(reward.ally, "Ally", i, eventData, issues);
                        break;
                    case TalkRewardGiver.RewardType.GiveRandomAlly:
                        RequireReference(reward.allyDatabase, "Ally Database", i, eventData, issues);
                        break;
                }
            }
        }
		
        //TalkData에서 Choice와 다음 TalkData를 따라가며 대화 그래프 전체를 검사
        private static void ValidateTalkGraph(RunEventSO eventData, TalkDataListSO talkData,
            HashSet<string> rewardKeys, HashSet<TalkDataListSO> visitedTalks,
            HashSet<TalkDataListSO> activeTalks, ICollection<RunEventValidationIssue> issues)
        {
            if (talkData == null)
                return;
			
            if (activeTalks.Contains(talkData))
            {
                //현재 검사 경로에 같은 TalkData가 다시 나오면 순환 대화
                AddIssue(issues, RunEventValidationSeverity.Warning,
                    $"순환 대화가 감지되었습니다: {talkData.name}", talkData, eventData);
                return;
            }
			
            //이미 검사한 공유 TalkData는 다시 검사하지 않음
            if (!visitedTalks.Add(talkData))
                return;
			
            activeTalks.Add(talkData);
			
            if (talkData.talkDataList == null || talkData.talkDataList.Count == 0)
            {
                AddIssue(issues, RunEventValidationSeverity.Warning,
                    "대사 목록이 비어 있습니다.", talkData, eventData);
            }
            else
            {
                for (int lineIndex = 0; lineIndex < talkData.talkDataList.Count; lineIndex++)
                {
                    TalkChoiceDataSO choiceData = talkData.talkDataList[lineIndex].choiceData;
                    ValidateChoiceData(eventData, choiceData, rewardKeys,
                        visitedTalks, activeTalks, issues);
                }
            }
			
            activeTalks.Remove(talkData);
        }
		
        //선택지 목록, 랜덤 대화, Reward Key, Action과 다음 대화를 검사
        private static void ValidateChoiceData(RunEventSO eventData, TalkChoiceDataSO choiceData,
            HashSet<string> rewardKeys, HashSet<TalkDataListSO> visitedTalks,
            HashSet<TalkDataListSO> activeTalks, ICollection<RunEventValidationIssue> issues)
        {
            if (choiceData == null)
                return;
			
            if (choiceData.choiceTextList == null || choiceData.choiceTextList.Count == 0)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "선택지 목록이 비어 있습니다.", choiceData, eventData);
                return;
            }
			
            for (int choiceIndex = 0; choiceIndex < choiceData.choiceTextList.Count; choiceIndex++)
            {
                ChoiceData choice = choiceData.choiceTextList[choiceIndex];
                string choiceName = string.IsNullOrWhiteSpace(choice.choiceText)
                    ? $"Element {choiceIndex}" : choice.choiceText;
				
                if (choice.isRandom && (choice.randomTalkList == null || choice.randomTalkList.Count == 0))
                {
                    AddIssue(issues, RunEventValidationSeverity.Error,
                        $"'{choiceName}'은 랜덤 대화를 사용하지만 목록이 비어 있습니다.", choiceData, eventData);
                }
				
                if (!string.IsNullOrWhiteSpace(choice.rewardKey) && !rewardKeys.Contains(choice.rewardKey))
                {
                    AddIssue(issues, RunEventValidationSeverity.Warning,
                        $"선택지 Reward Key '{choice.rewardKey}'와 일치하는 보상이 없습니다.",
                        choiceData, eventData);
                }
				
                if (choice.beforeEventActionTalkData != null && choice.eventAction == null)
                {
                    AddIssue(issues, RunEventValidationSeverity.Warning,
                        $"'{choiceName}'에 이벤트 실행 전 대화가 있지만 이벤트 행동이 없습니다.",
                        choiceData, eventData);
                }
				
                if (choice.eventAction != null)
                    ValidateAction(eventData, choice.eventAction, rewardKeys, issues);
				
                ValidateTalkGraph(eventData, choice.beforeEventActionTalkData,
                    rewardKeys, visitedTalks, activeTalks, issues);
				
                ValidateTalkGraph(eventData, choice.nextTalkData,
                    rewardKeys, visitedTalks, activeTalks, issues);
				
                if (choice.randomTalkList == null)
                    continue;
				
                for (int randomIndex = 0; randomIndex < choice.randomTalkList.Count; randomIndex++)
                {
                    ValidateTalkGraph(eventData, choice.randomTalkList[randomIndex],
                        rewardKeys, visitedTalks, activeTalks, issues);
                }
            }
        }
		
        //Action의 공통 비용과 종류별 선택, 성공, 실패 결과를 검사
        private static void ValidateAction(RunEventSO eventData, RunEventActionSO action,
            HashSet<string> rewardKeys, ICollection<RunEventValidationIssue> issues)
        {
            if (action.costs != null)
            {
                for (int i = 0; i < action.costs.Count; i++)
                    ValidateCost(eventData, action, action.costs[i], i, issues);
            }
			
            switch (action)
            {
                case RunEventTransactionSO transaction:
                    ValidateSelection(eventData, action, transaction.selection, issues);
                    ValidateOutcome(eventData, action, transaction.successOutcome, rewardKeys, issues);
                    break;
                case RunEventChanceSO chance:
                    ValidateOutcome(eventData, action, chance.successOutcome, rewardKeys, issues);
                    ValidateOutcome(eventData, action, chance.failureOutcome, rewardKeys, issues);
                    break;
                case RunEventDoubleOrNothingSO gamble:
                    if (string.IsNullOrWhiteSpace(gamble.StateKey))
                    {
                        AddIssue(issues, RunEventValidationSeverity.Error,
                            "Double Or Nothing의 State Key가 비어 있습니다.", action, eventData);
                    }
					
                    ValidateOutcome(eventData, action, gamble.successOutcome, rewardKeys, issues);
                    ValidateOutcome(eventData, action, gamble.failureOutcome, rewardKeys, issues);
                    break;
            }
        }
		
        //비용 요소와 비용 종류에 필요한 Item, Relic, Skill, Ally 참조를 검사
        private static void ValidateCost(RunEventSO eventData, RunEventActionSO action,
            RunEventCostData cost, int index, ICollection<RunEventValidationIssue> issues)
        {
            if (cost == null)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    $"Action Cost Element {index}가 비어 있습니다.", action, eventData);
                return;
            }
			
            UnityEngine.Object requiredReference = cost.costType switch
            {
                RunEventCostType.Item => cost.item,
                RunEventCostType.Relic => cost.relic,
                RunEventCostType.Skill => cost.skill,
                RunEventCostType.Ally => cost.ally,
                _ => action
            };
            //Currency와 체력 비용은 별도 SO가 필요 없어 action을 정상값으로 사용
			
            if (requiredReference == null)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    $"Action Cost Element {index}의 {cost.costType} 데이터가 없습니다.", action, eventData);
            }
        }
		
        //Transaction의 선택 대상과 선택 개수, 피해 범위 설정을 검사
        private static void ValidateSelection(RunEventSO eventData, RunEventActionSO action,
            RunEventSelectionSettings selection, ICollection<RunEventValidationIssue> issues)
        {
            if (selection == null)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "Transaction 선택 설정이 없습니다.", action, eventData);
                return;
            }
			
            if (selection.MinimumCount > 0 && selection.source == RunEventSelectionSource.None)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "선택 개수가 1 이상이지만 Selection Source가 None입니다.", action, eventData);
            }
			
            if (selection.maximumTotalDamage > 0f &&
                selection.minimumTotalDamage > selection.maximumTotalDamage)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "최소 총 피해가 최대 총 피해보다 큽니다.", action, eventData);
            }
        }
		
        //Action 결과 데이터와 결과에서 사용할 Reward Key를 검사
        private static void ValidateOutcome(RunEventSO eventData, RunEventActionSO action,
            RunEventOutcomeData outcome, HashSet<string> rewardKeys,
            ICollection<RunEventValidationIssue> issues)
        {
            if (outcome == null)
            {
                AddIssue(issues, RunEventValidationSeverity.Error,
                    "Action Outcome 데이터가 없습니다.", action, eventData);
                return;
            }
			
            if (!string.IsNullOrWhiteSpace(outcome.rewardKey) && !rewardKeys.Contains(outcome.rewardKey))
            {
                AddIssue(issues, RunEventValidationSeverity.Warning,
                    $"Action Reward Key '{outcome.rewardKey}'와 일치하는 보상이 없습니다.", action, eventData);
            }
        }
		
        //RunEvent 보상 목록에서 비어 있지 않은 Reward Key만 모음
        private static HashSet<string> CollectRewardKeys(RunEventSO eventData)
        {
            HashSet<string> rewardKeys = new HashSet<string>(StringComparer.Ordinal);
            if (eventData.rewardEntries == null)
                return rewardKeys;
			
            for (int i = 0; i < eventData.rewardEntries.Length; i++)
            {
                TalkRewardGiver.RewardEntry reward = eventData.rewardEntries[i];
                if (reward != null && !string.IsNullOrWhiteSpace(reward.rewardKey))
                    rewardKeys.Add(reward.rewardKey);
            }
			
            return rewardKeys;
        }
		
        //같은 Talk ID를 사용하는 RunEvent가 두 개 이상인지 검사
        private static void AddDuplicateTalkIdIssues(IReadOnlyList<RunEventSO> events,
            ICollection<RunEventValidationIssue> issues)
        {
            IEnumerable<IGrouping<string, RunEventSO>> duplicateGroups = events
                .Where(eventData => eventData != null && !string.IsNullOrWhiteSpace(eventData.TalkId))
                .GroupBy(eventData => eventData.TalkId)
                .Where(group => group.Count() > 1);
			
            foreach (IGrouping<string, RunEventSO> group in duplicateGroups)
            {
                foreach (RunEventSO eventData in group)
                {
                    AddIssue(issues, RunEventValidationSeverity.Error,
                        $"Talk ID '{group.Key}'를 사용하는 RunEvent가 여러 개입니다.", eventData, eventData);
                }
            }
        }
		
        //Action 폴더 전체에서 같은 Action ID를 사용하는 SO가 있는지 검사
        private void AddDuplicateActionIdIssues(ICollection<RunEventValidationIssue> issues)
        {
            IEnumerable<IGrouping<string, RunEventActionSO>> duplicateGroups = _repository.FindActions()
                .Where(action => action != null && !string.IsNullOrWhiteSpace(action.ActionId))
                .GroupBy(action => action.ActionId)
                .Where(group => group.Count() > 1);
			
            foreach (IGrouping<string, RunEventActionSO> group in duplicateGroups)
            {
                foreach (RunEventActionSO action in group)
                {
                    AddIssue(issues, RunEventValidationSeverity.Warning,
                        $"Action ID '{group.Key}'를 사용하는 Action SO가 여러 개입니다.", action, null);
                }
            }
        }
		
        //보상 종류에 필요한 Object 참조가 비어 있으면 Error를 추가
        private static void RequireReference(UnityEngine.Object value, string fieldName, int rewardIndex,
            RunEventSO eventData, ICollection<RunEventValidationIssue> issues)
        {
            if (value != null)
                return;
			
            AddIssue(issues, RunEventValidationSeverity.Error,
                $"Reward Element {rewardIndex}의 {fieldName}이 비어 있습니다.", eventData, eventData);
        }
		
        //검사 결과 정보를 만들어 목록에 추가하는 공통 메서드
        private static void AddIssue(ICollection<RunEventValidationIssue> issues,
            RunEventValidationSeverity severity, string message, UnityEngine.Object target, RunEventSO ownerEvent)
        {
            issues.Add(new RunEventValidationIssue(severity, message, target, ownerEvent));
        }
		
    }
}
#endif
