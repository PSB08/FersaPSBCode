#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using PSW.Code.Talk;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Work.PSB.Code.RunSystem.Editor
{
    //선택한 RunEventSO의 전체 연결 구조를 가운데 화면에 표시
    public class RunEventFlowView : IDisposable
    {
        //대화 연결을 화면에 재귀적으로 표시할 최대 깊이
        private const int MaximumVisibleDepth = 12;
		
        private readonly RunEventAuthoringState _state; //현재 선택된 이벤트와 상세 SO 상태
        private readonly RunEventAssetRepository _repository; //에셋 폴더와 경로 관리
        private readonly RunEventAssetFactory _factory; //Talk, Choice, Action SO 생성과 연결
        
        private readonly VisualElement _emptyFlow; //이벤트를 선택하지 않았을 때 표시할 화면
        private readonly ScrollView _flowScroll; //이벤트 흐름 UI가 들어갈 스크롤 영역
        private readonly Action _onAssetsChanged; //에셋 변경 후 전체 화면을 갱신할 콜백
		
        //UXML 요소를 찾아 저장하고 이벤트 선택 변경 시 화면을 다시 그리도록 연결
        public RunEventFlowView(VisualElement root, RunEventAuthoringState state,
            RunEventAssetRepository repository, RunEventAssetFactory factory, Action onAssetsChanged)
        {
            _state = state;
            _repository = repository;
            _factory = factory;
            _onAssetsChanged = onAssetsChanged;
            _emptyFlow = root.Q<VisualElement>("empty-flow");
            _flowScroll = root.Q<ScrollView>("flow-scroll");
            _flowScroll.mode = ScrollViewMode.Vertical;
            _flowScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _state.EventSelectionChanged += Refresh;
            Refresh();
        }
		
        //View가 닫힐 때 State에 등록한 이벤트를 해제
        public void Dispose()
        {
            _state.EventSelectionChanged -= Refresh;
        }
		
        //선택된 RunEventSO의 전체 정보를 처음부터 다시 그림
        public void Refresh()
        {
            _flowScroll.Clear();
            RunEventSO eventData = _state.SelectedEvent;
            bool hasEvent = eventData != null;
            _emptyFlow.style.display = hasEvent ? DisplayStyle.None : DisplayStyle.Flex;
            _flowScroll.style.display = hasEvent ? DisplayStyle.Flex : DisplayStyle.None;
			
            if (!hasEvent)
                return;
			
            DrawHeader(eventData);
            DrawOverviewSection(eventData);
            DrawTalkStagesSection(eventData);
            DrawBattleSection(eventData);
            DrawRewardSection(eventData);
            DrawDialogueGraphSection(eventData);
        }
		
        //이벤트 이미지, 이름, Talk ID와 편집 버튼을 표시
        private void DrawHeader(RunEventSO eventData)
        {
            VisualElement header = new VisualElement();
            header.AddToClassList("flow-header");
			
            Image image = new Image
            {
                sprite = eventData.sprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            image.AddToClassList("flow-header-image");
            header.Add(image);
			
            VisualElement textRoot = new VisualElement();
            textRoot.AddToClassList("flow-header-text");
            Label nameLabel = new Label(eventData.name);
            nameLabel.AddToClassList("flow-header-name");
            nameLabel.tooltip = eventData.name;
            Label idLabel = new Label($"Talk ID: {eventData.TalkId}");
            idLabel.AddToClassList("flow-header-id");
            idLabel.tooltip = idLabel.text;
            textRoot.Add(nameLabel);
            textRoot.Add(idLabel);
            header.Add(textRoot);
			
            Button editButton = CreateCommandButton("편집", "RunEventSO를 오른쪽 Inspector에 표시합니다.",
                () => _state.SelectObject(eventData));
            header.Add(editButton);
            _flowScroll.Add(header);
        }
		
        //NPC, System Prefab과 이벤트 완료 후 처리 방식을 요약
        private void DrawOverviewSection(RunEventSO eventData)
        {
            VisualElement section = CreateSection("이벤트 개요");
            section.Add(CreateSummaryLabel($"NPC: {GetObjectName(eventData.entityPrefab)}"));
            section.Add(CreateSummaryLabel($"System Prefab: {GetObjectName(eventData.systemPrefab)}"));
            section.Add(CreateSummaryLabel(eventData.disableEntityAfterFinished
                ? "완료 후 NPC 오브젝트 비활성화" : "완료 후 NPC 오브젝트 유지"));
            _flowScroll.Add(section);
        }
		
        //시작, 전투 성공, 전투 실패 TalkData의 연결 상태를 표시
        private void DrawTalkStagesSection(RunEventSO eventData)
        {
            VisualElement section = CreateSection("대화 단계");
            DrawTalkConnection(section, "시작 대화", eventData, "talkStage.talkData",
                eventData.talkStage?.talkData, RunEventAssetRepository.MainTalkFolder, "_Talk");
            DrawTalkConnection(section, "성공 대화", eventData, "victoryTalkStage.talkData",
                eventData.victoryTalkStage?.talkData, RunEventAssetRepository.AfterBattleFolder, "_Victory");
            DrawTalkConnection(section, "실패 대화", eventData, "failureTalkStage.talkData",
                eventData.failureTalkStage?.talkData, RunEventAssetRepository.AfterBattleFolder, "_Failure");
            _flowScroll.Add(section);
        }
		
        //전투 유무와 Challenge 또는 Health End 조건을 요약
        private void DrawBattleSection(RunEventSO eventData)
        {
            VisualElement section = CreateSection("전투");
            section.Add(CreateSummaryLabel($"Encounter: {GetObjectName(eventData.battleEncounter)}"));
            section.Add(CreateSummaryLabel($"Presentation: {GetObjectName(eventData.battlePresentation)}"));
			
            if (!eventData.HasBattle)
            {
                Label noBattleLabel = CreateSummaryLabel("전투 없음");
                noBattleLabel.AddToClassList("warning-text");
                section.Add(noBattleLabel);
            }
            else if (eventData.HasBattleChallenge)
            {
                section.Add(CreateSummaryLabel(
                    $"Challenge: {eventData.battleChallengeTurnLimit}턴 안에 " +
                    $"{eventData.battleChallengeRequiredDamage:0.#} 피해"));
                section.Add(CreateSummaryLabel(
                    $"대상 적: Element {eventData.battleChallengeTargetEnemyIndex}"));
            }
            else if (eventData.HasBattleHealthEndCondition)
            {
                string condition = eventData.battleHealthEndMode == RunEventBattleHealthEndMode.HealthOne
                    ? "체력 1" : $"체력 {eventData.battleHealthTargetPercent * 100f:0.#}% 이하";
                section.Add(CreateSummaryLabel(
                    $"Health End: Element {eventData.battleHealthTargetEnemyIndex}, {condition}"));
            }
            else
            {
                section.Add(CreateSummaryLabel("일반 이벤트 전투"));
            }
			
            section.Add(CreateCommandButton("전투 설정 편집", "오른쪽 Inspector에서 전투 설정을 편집합니다.",
                () => _state.SelectObject(eventData)));
            _flowScroll.Add(section);
        }
		
        //보상 개수와 사용 중인 Reward Key를 중복 없이 표시
        private void DrawRewardSection(RunEventSO eventData)
        {
            VisualElement section = CreateSection("보상");
            int rewardCount = eventData.rewardEntries != null ? eventData.rewardEntries.Length : 0;
            section.Add(CreateSummaryLabel($"Reward Entry: {rewardCount}개"));
			
            if (eventData.rewardEntries != null)
            {
                IEnumerable<string> rewardKeys = eventData.rewardEntries
                    .Where(reward => reward != null && !string.IsNullOrWhiteSpace(reward.rewardKey))
                    .Select(reward => reward.rewardKey)
                    .Distinct();
                string keyText = string.Join(", ", rewardKeys);
                section.Add(CreateSummaryLabel(string.IsNullOrWhiteSpace(keyText)
                    ? "Reward Key 없음" : $"Reward Keys: {keyText}"));
            }
			
            section.Add(CreateCommandButton("보상 설정 편집", "오른쪽 Inspector에서 보상 목록을 편집합니다.",
                () => _state.SelectObject(eventData)));
            _flowScroll.Add(section);
        }
		
        //시작, 성공, 실패 대화를 출발점으로 전체 대화 연결을 그림
        private void DrawDialogueGraphSection(RunEventSO eventData)
        {
            VisualElement section = CreateSection("전체 대화 흐름");
            //activeTalks는 현재 탐색 경로의 순환 검사, renderedTalks는 중복 표시 방지에 사용
            HashSet<TalkDataListSO> activeTalks = new HashSet<TalkDataListSO>();
            HashSet<TalkDataListSO> renderedTalks = new HashSet<TalkDataListSO>();
			
            DrawTalkGraph(section, eventData.talkStage?.talkData,
                "시작", 0, activeTalks, renderedTalks);
            DrawTalkGraph(section, eventData.victoryTalkStage?.talkData,
                "전투 성공", 0, activeTalks, renderedTalks);
            DrawTalkGraph(section, eventData.failureTalkStage?.talkData,
                "전투 실패", 0, activeTalks, renderedTalks);
            _flowScroll.Add(section);
        }
		
        //TalkData에서 ChoiceData와 다음 TalkData를 따라가며 재귀적으로 화면에 표시
        private void DrawTalkGraph(VisualElement parent, TalkDataListSO talkData, string edgeLabel, int depth,
            HashSet<TalkDataListSO> activeTalks, HashSet<TalkDataListSO> renderedTalks)
        {
            if (talkData == null)
                return;
			
            if (depth > MaximumVisibleDepth)
            {
                //연결이 너무 깊으면 Editor가 계속 커지지 않도록 여기서 중단
                Label depthLabel = CreateSummaryLabel($"{edgeLabel} → 표시 깊이 제한 도달");
                depthLabel.AddToClassList("warning-text");
                parent.Add(depthLabel);
                return;
            }
			
            if (activeTalks.Contains(talkData))
            {
                //현재 경로에 같은 TalkData가 다시 나오면 순환 연결
                Label cycleLabel = CreateSummaryLabel($"{edgeLabel} → {talkData.name} (순환 연결)");
                cycleLabel.AddToClassList("warning-text");
                parent.Add(cycleLabel);
                return;
            }
			
            if (renderedTalks.Contains(talkData))
            {
                //이미 그린 공유 TalkData는 내용 대신 선택 버튼만 표시
                Button linkedButton = CreateCommandButton($"{edgeLabel} → {talkData.name} (위에서 표시됨)",
                    "연결된 TalkData를 선택합니다.", () => _state.SelectObject(talkData));
                parent.Add(linkedButton);
                return;
            }
			
            renderedTalks.Add(talkData);
            activeTalks.Add(talkData);
			
            VisualElement talkRoot = new VisualElement();
            talkRoot.AddToClassList("dialogue-row");
            talkRoot.AddToClassList($"dialogue-depth-{Mathf.Min(depth, 6)}");
			
            VisualElement titleRow = new VisualElement();
            titleRow.AddToClassList("flow-title-row");
            Label title = new Label($"{edgeLabel} → {talkData.name}");
            title.AddToClassList("choice-title");
            title.tooltip = title.text;
            titleRow.Add(title);
            titleRow.Add(CreateSmallButton("편집", "TalkData를 오른쪽 Inspector에 표시합니다.",
                () => _state.SelectObject(talkData)));
            titleRow.Add(CreateSmallButton("+ 대사", "TalkData에 새 대사를 추가합니다.", () =>
            {
                _factory.AddTalkLine(talkData);
                NotifyAssetsChanged(talkData);
            }));
            talkRoot.Add(titleRow);
			
            if (talkData.talkDataList == null || talkData.talkDataList.Count == 0)
            {
                Label emptyLabel = CreateSummaryLabel("대사가 없습니다.");
                emptyLabel.AddToClassList("warning-text");
                talkRoot.Add(emptyLabel);
            }
            else
            {
                for (int lineIndex = 0; lineIndex < talkData.talkDataList.Count; lineIndex++)
                    DrawTalkLine(talkRoot, talkData, lineIndex, depth, activeTalks, renderedTalks);
            }
			
            parent.Add(talkRoot);
            activeTalks.Remove(talkData);
        }
		
        //대사 한 줄의 방향, 내용 미리보기와 연결된 ChoiceData를 표시
        private void DrawTalkLine(VisualElement talkRoot, TalkDataListSO talkData, int lineIndex, int depth,
            HashSet<TalkDataListSO> activeTalks, HashSet<TalkDataListSO> renderedTalks)
        {
            TalkValueData talkLine = talkData.talkDataList[lineIndex];
            VisualElement lineRoot = new VisualElement();
            lineRoot.style.marginTop = 4f;
			
            VisualElement lineHeader = new VisualElement();
            lineHeader.AddToClassList("flow-title-row");
            Label textLabel = new Label(
                $"{lineIndex + 1}. [{talkLine.Type}] {GetDialoguePreview(talkLine.Text)}");
            textLabel.AddToClassList("dialogue-label");
            textLabel.tooltip = talkLine.Text;
            lineHeader.Add(textLabel);
			
            if (talkLine.choiceData == null)
            {
                lineHeader.Add(CreateSmallButton("+ 선택지", "이 대사에 ChoiceData를 생성하고 연결합니다.", () =>
                {
                    TalkChoiceDataSO createdChoice = _factory.CreateChoiceAndAssign(talkData, lineIndex);
                    NotifyAssetsChanged(createdChoice);
                }));
            }
            else
            {
                lineHeader.Add(CreateSmallButton("선택지 편집", "연결된 ChoiceData를 편집합니다.",
                    () => _state.SelectObject(talkLine.choiceData)));
            }
			
            lineRoot.Add(lineHeader);
			
            if (talkLine.choiceData != null)
                DrawChoiceData(lineRoot, talkLine.choiceData, depth + 1, activeTalks, renderedTalks);
			
            talkRoot.Add(lineRoot);
        }
		
        //ChoiceData의 선택지 목록과 각 선택지에서 이어지는 연결을 표시
        private void DrawChoiceData(VisualElement parent, TalkChoiceDataSO choiceData, int depth,
            HashSet<TalkDataListSO> activeTalks, HashSet<TalkDataListSO> renderedTalks)
        {
            VisualElement choiceRoot = new VisualElement();
            choiceRoot.AddToClassList("choice-row");
			
            VisualElement titleRow = new VisualElement();
            titleRow.AddToClassList("flow-title-row");
            Label title = new Label(choiceData.name);
            title.AddToClassList("choice-title");
            title.tooltip = choiceData.name;
            titleRow.Add(title);
            titleRow.Add(CreateSmallButton("편집", "ChoiceData를 오른쪽 Inspector에 표시합니다.",
                () => _state.SelectObject(choiceData)));
            titleRow.Add(CreateSmallButton("+ 항목", "새 Choice 항목을 추가합니다.", () =>
            {
                _factory.AddChoiceOption(choiceData);
                NotifyAssetsChanged(choiceData);
            }));
            choiceRoot.Add(titleRow);
			
            if (choiceData.choiceTextList == null || choiceData.choiceTextList.Count == 0)
            {
                Label emptyLabel = CreateSummaryLabel("선택지 항목이 없습니다.");
                emptyLabel.AddToClassList("warning-text");
                choiceRoot.Add(emptyLabel);
                parent.Add(choiceRoot);
                return;
            }
			
            for (int choiceIndex = 0; choiceIndex < choiceData.choiceTextList.Count; choiceIndex++)
            {
                ChoiceData choice = choiceData.choiceTextList[choiceIndex];
                VisualElement optionRoot = new VisualElement();
                optionRoot.style.marginBottom = 7f;
                optionRoot.Add(CreateSummaryLabel(
                    $"{choiceIndex + 1}. {GetDialoguePreview(choice.choiceText)}  [{choice.actionType}]"));
				
                if (choice.runEventBattleAction != RunEventBattleAction.Default)
                    optionRoot.Add(CreateSummaryLabel($"전투 행동: {choice.runEventBattleAction}"));
				
                if (!string.IsNullOrWhiteSpace(choice.rewardKey))
                    optionRoot.Add(CreateSummaryLabel($"Reward Key: {choice.rewardKey}"));
				
                DrawChoiceConnections(optionRoot, choiceData, choiceIndex, choice);
				
                DrawTalkGraph(optionRoot, choice.beforeEventActionTalkData,
                    "이벤트 실행 전 대화", depth + 1, activeTalks, renderedTalks);
                DrawActionOutcomeTalks(optionRoot, choice.eventAction,
                    depth + 1, activeTalks, renderedTalks);
				
                if (choice.isRandom && choice.randomTalkList != null)
                {
                    //랜덤 선택지는 randomTalkList의 모든 대화를 각각 표시
                    for (int randomIndex = 0; randomIndex < choice.randomTalkList.Count; randomIndex++)
                    {
                        DrawTalkGraph(optionRoot, choice.randomTalkList[randomIndex],
                            $"랜덤 기본 대화 {randomIndex + 1}", depth + 1, activeTalks, renderedTalks);
                    }
                }
                else
                {
                    DrawTalkGraph(optionRoot, choice.nextTalkData,
                        "기본 다음 대화", depth + 1, activeTalks, renderedTalks);
                }
                choiceRoot.Add(optionRoot);
            }
			
            parent.Add(choiceRoot);
        }
		
        //선택지에서 실행 전 대화, Event Action, 결과 대화와 기본 다음 대화를 순서대로 표시
        private void DrawChoiceConnections(VisualElement parent, TalkChoiceDataSO choiceData,
            int choiceIndex, ChoiceData choice)
        {
            string beforeActionTalkPath =
                $"choiceTextList.Array.data[{choiceIndex}].beforeEventActionTalkData";
            DrawTalkConnection(parent, "이벤트 전 대화", choiceData, beforeActionTalkPath,
                choice.beforeEventActionTalkData, RunEventAssetRepository.NextTalkFolder,
                $"_BeforeAction_{choiceIndex + 1}");
			
            string actionPath = $"choiceTextList.Array.data[{choiceIndex}].eventAction";
            DrawActionConnection(parent, "이벤트 행동", choiceData, actionPath,
                choice.eventAction, choiceIndex);
			
            DrawActionOutcomeConnections(parent, choice.eventAction);
			
            string nextTalkPath = $"choiceTextList.Array.data[{choiceIndex}].nextTalkData";
            DrawTalkConnection(parent, "기본 다음 대화", choiceData, nextTalkPath,
                choice.nextTalkData, RunEventAssetRepository.NextTalkFolder, $"_Next_{choiceIndex + 1}");
        }
		
        //Action 종류에 맞는 성공, 실패 TalkData 연결 필드를 표시
        private void DrawActionOutcomeConnections(VisualElement parent, RunEventActionSO action)
        {
            switch (action)
            {
                case RunEventTransactionSO transaction:
                    DrawTalkConnection(parent, "행동 성공", transaction, "successOutcome.nextTalkData",
                        transaction.successOutcome.nextTalkData, RunEventAssetRepository.NextTalkFolder, "_Success");
                    break;
                case RunEventChanceSO chance:
                    DrawTalkConnection(parent, "확률 성공", chance, "successOutcome.nextTalkData",
                        chance.successOutcome.nextTalkData, RunEventAssetRepository.NextTalkFolder, "_Success");
                    DrawTalkConnection(parent, "확률 실패", chance, "failureOutcome.nextTalkData",
                        chance.failureOutcome.nextTalkData, RunEventAssetRepository.NextTalkFolder, "_Failure");
                    break;
                case RunEventDoubleOrNothingSO gamble:
                    DrawTalkConnection(parent, "도박 성공", gamble, "successOutcome.nextTalkData",
                        gamble.successOutcome.nextTalkData, RunEventAssetRepository.NextTalkFolder, "_Success");
                    DrawTalkConnection(parent, "도박 실패", gamble, "failureOutcome.nextTalkData",
                        gamble.failureOutcome.nextTalkData, RunEventAssetRepository.NextTalkFolder, "_Failure");
                    break;
            }
        }
		
        //Action 결과에 연결된 TalkData를 실제 대화 흐름으로 이어서 표시
        private void DrawActionOutcomeTalks(VisualElement parent, RunEventActionSO action, int depth,
            HashSet<TalkDataListSO> activeTalks, HashSet<TalkDataListSO> renderedTalks)
        {
            switch (action)
            {
                case RunEventTransactionSO transaction:
                    DrawTalkGraph(parent, transaction.successOutcome.nextTalkData,
                        "행동 성공", depth, activeTalks, renderedTalks);
                    break;
                case RunEventChanceSO chance:
                    DrawTalkGraph(parent, chance.successOutcome.nextTalkData,
                        "확률 성공", depth, activeTalks, renderedTalks);
                    DrawTalkGraph(parent, chance.failureOutcome.nextTalkData,
                        "확률 실패", depth, activeTalks, renderedTalks);
                    break;
                case RunEventDoubleOrNothingSO gamble:
                    DrawTalkGraph(parent, gamble.successOutcome.nextTalkData,
                        "도박 성공", depth, activeTalks, renderedTalks);
                    DrawTalkGraph(parent, gamble.failureOutcome.nextTalkData,
                        "도박 실패", depth, activeTalks, renderedTalks);
                    break;
            }
        }
		
        //TalkData를 교체하거나 새로 생성하고 선택할 수 있는 연결 행을 생성
        private void DrawTalkConnection(VisualElement parent, string label, UnityEngine.Object owner,
            string propertyPath, TalkDataListSO currentValue, string folder, string suffix)
        {
            VisualElement row = CreateConnectionRow(label, out VisualElement controls);
            ObjectField field = CreateObjectField(typeof(TalkDataListSO), currentValue);
            field.RegisterValueChangedCallback(evt =>
            {
                _factory.AssignObjectReference(owner, propertyPath, evt.newValue, "Connect Talk Data");
                NotifyAssetsChanged(evt.newValue);
            });
            controls.Add(field);
			
            Button createButton = CreateSmallButton("+", "새 TalkData를 생성하고 즉시 연결합니다.", () =>
            {
                TalkDataListSO createdTalk = _factory.CreateTalkAndAssign(owner, propertyPath, folder, suffix);
                NotifyAssetsChanged(createdTalk);
            });
            controls.Add(createButton);
			
            Button selectButton = CreateSmallButton("선택", "연결된 TalkData를 편집합니다.", () =>
            {
                if (currentValue != null)
                    _state.SelectObject(currentValue);
            });
            selectButton.SetEnabled(currentValue != null);
            controls.Add(selectButton);
            parent.Add(row);
        }
		
        //Action을 교체하거나 종류를 골라 생성하고 선택할 수 있는 연결 행을 생성
        private void DrawActionConnection(VisualElement parent, string label, TalkChoiceDataSO owner,
            string propertyPath, RunEventActionSO currentValue, int choiceIndex)
        {
            VisualElement row = CreateConnectionRow(label, out VisualElement controls);
            ObjectField field = CreateObjectField(typeof(RunEventActionSO), currentValue);
            field.RegisterValueChangedCallback(evt =>
            {
                _factory.AssignObjectReference(owner, propertyPath, evt.newValue, "Connect Run Event Action");
                NotifyAssetsChanged(evt.newValue);
            });
            controls.Add(field);
			
            Button createButton = CreateSmallButton("+", "생성할 Action 종류를 선택합니다.", () =>
            {
                GenericMenu menu = new GenericMenu();
                AddActionCreationItem(menu, owner, choiceIndex, RunEventActionKind.Transaction);
                AddActionCreationItem(menu, owner, choiceIndex, RunEventActionKind.Chance);
                AddActionCreationItem(menu, owner, choiceIndex, RunEventActionKind.DoubleOrNothing);
                menu.ShowAsContext();
            });
            controls.Add(createButton);
			
            Button selectButton = CreateSmallButton("선택", "연결된 Action을 편집합니다.", () =>
            {
                if (currentValue != null)
                    _state.SelectObject(currentValue);
            });
            selectButton.SetEnabled(currentValue != null);
            controls.Add(selectButton);
            parent.Add(row);
        }
		
        //Action 생성 메뉴에 종류별 항목을 추가하고 선택 시 ChoiceData에 연결
        private void AddActionCreationItem(GenericMenu menu, TalkChoiceDataSO owner,
            int choiceIndex, RunEventActionKind actionKind)
        {
            menu.AddItem(new GUIContent(actionKind.ToString()), false, () =>
            {
                RunEventActionSO action = _factory.CreateActionAndAssign(owner, choiceIndex, actionKind);
                NotifyAssetsChanged(action);
            });
        }
		
        //변경 내용을 저장하고 View를 갱신한 뒤 새로 만든 SO를 Inspector에 표시
        private void NotifyAssetsChanged(UnityEngine.Object selectedObject)
        {
            AssetDatabase.SaveAssets();
            _onAssetsChanged?.Invoke();
			
            if (selectedObject != null)
                _state.SelectObject(selectedObject);
        }
		
        //제목이 포함된 Flow 섹션을 생성
        private static VisualElement CreateSection(string title)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("flow-section");
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("flow-section-title");
            section.Add(titleLabel);
            return section;
        }
		
        //왼쪽 Label과 오른쪽 컨트롤 영역으로 구성된 연결 행을 생성
        private static VisualElement CreateConnectionRow(string label, out VisualElement controls)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("connection-row");
            Label labelElement = new Label(label);
            labelElement.AddToClassList("connection-label");
            row.Add(labelElement);
			
            controls = new VisualElement();
            controls.AddToClassList("connection-controls");
            row.Add(controls);
            return row;
        }
		
        //프로젝트 에셋만 받을 수 있는 ObjectField를 생성
        private static ObjectField CreateObjectField(Type objectType, UnityEngine.Object value)
        {
            ObjectField field = new ObjectField
            {
                objectType = objectType,
                allowSceneObjects = false
            };
            field.SetValueWithoutNotify(value);
            field.AddToClassList("connection-field");
            return field;
        }
		
        //텍스트, 설명, 클릭 동작이 들어간 기본 명령 버튼을 생성
        private static Button CreateCommandButton(string text, string tooltip, Action clicked)
        {
            Button button = new Button(clicked)
            {
                text = text,
                tooltip = tooltip
            };
            button.AddToClassList("flow-command-button");
            return button;
        }
		
        //글자 길이에 맞는 크기 스타일을 추가한 작은 버튼을 생성
        private static Button CreateSmallButton(string text, string tooltip, Action clicked)
        {
            Button button = CreateCommandButton(text, tooltip, clicked);
            button.AddToClassList("connection-button");
			
            if (text == "+")
                button.AddToClassList("icon-connection-button");
            else if (text.Length <= 2)
                button.AddToClassList("short-connection-button");
            else
                button.AddToClassList("long-connection-button");
			
            return button;
        }
		
        //Flow에서 간단한 정보를 보여주는 Label을 생성
        private static Label CreateSummaryLabel(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("flow-summary-label");
            return label;
        }
		
        //에셋이 없으면 None, 있으면 에셋 이름을 반환
        private static string GetObjectName(UnityEngine.Object target)
        {
            return target != null ? target.name : "None";
        }
		
        //대사의 줄바꿈을 제거하고 80자가 넘으면 줄여서 반환
        private static string GetDialoguePreview(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "(내용 없음)";
			
            string singleLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return singleLine.Length <= 80 ? singleLine : singleLine.Substring(0, 77) + "...";
        }
		
    }
}
#endif
