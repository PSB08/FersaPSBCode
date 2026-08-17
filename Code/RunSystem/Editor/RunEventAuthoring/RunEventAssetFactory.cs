#if UNITY_EDITOR
using System.Collections.Generic;
using PSW.Code.Talk;
using UnityEditor;
using UnityEngine;

namespace Work.PSB.Code.RunSystem.Editor
{
    public enum RunEventActionKind
    {
        Transaction, //자원이나 대상을 지불, 선택하는 교환
        Chance, //성공 확률에 따라 결과 분기
        DoubleOrNothing //성공할 때 값을 누적하고 실패 또는 정산하는 행동
    } //생성할 이벤트 Action 종류
	
    public enum RunEventTemplateKind
    {
        Empty,
        Dialogue,
        Choice,
        Battle,
        DamageChallenge,
        HealthOneBattle,
        HealthPercentBattle,
        Transaction,
        Chance,
        DoubleOrNothing
    } //새 이벤트 메뉴에서 선택할 초기 구성
    //Empty, Dialogue, Choice, 일반 전투, Challenge 전투, 체력 종료 전투 두 종류, Action 이벤트 세 종류
	
    //RunEvent와 연결된 Talk, Choice, Action SO의 생성,연결,복제를 담당
    public class RunEventAssetFactory
    {
        private readonly RunEventAssetRepository _repository;
        //폴더 생성, 에셋 탐색, 경로 규칙을 담당하는 Repository
		
        //에셋 폴더와 경로를 관리할 Repository를 저장
        public RunEventAssetFactory(RunEventAssetRepository repository)
        {
            _repository = repository;
        }
		
        //저장 위치를 입력받아 빈 RunEventSO를 생성
        public RunEventSO CreateRunEvent()
        {
            _repository.EnsureDefaultFolders(); //기본 폴더를 확인하고 없으면 생성
            string path = EditorUtility.SaveFilePanelInProject("새 Run Event", "RunEvent", "asset",
                "생성할 RunEventSO의 이름과 위치를 선택하세요.", RunEventAssetRepository.RunEventFolder);
			//SaveFilePanelInProject()로 저장 위치와 이름을 받아오기
            
            if (string.IsNullOrWhiteSpace(path))
                return null; //취소하면 null을 반환
			
            RunEventSO eventData = ScriptableObject.CreateInstance<RunEventSO>(); //RunEventSO 인스턴스를 만들고
            CreateAsset(eventData, AssetDatabase.GenerateUniqueAssetPath(path), "Create Run Event"); 
            //중복되지 않는 경로로 .asset을 생성, Undo에 생성 작업을 등록
            return eventData;
        }
		
        //빈 RunEventSO를 생성한 뒤 선택한 템플릿에 필요한 SO와 연결을 구성
        public RunEventSO CreateRunEvent(RunEventTemplateKind templateKind)
        {
            RunEventSO eventData = CreateRunEvent();
            if (eventData == null || templateKind == RunEventTemplateKind.Empty)
                return eventData;
			
            Undo.RecordObject(eventData, "Configure Run Event Template");
            eventData.talkStage.actionMode = InteractActionMode.TalkOnly;
            //Empty가 아닌 템플릿은 공통으로 시작 TalkData를 먼저 생성
            TalkDataListSO startTalk = CreateTalkAndAssign(eventData, "talkStage.talkData",
                RunEventAssetRepository.MainTalkFolder, "_Talk");
            
            switch (templateKind)
            {
                case RunEventTemplateKind.Dialogue:
                    break; //시작 TalkData만 사용하는 대화 이벤트
                case RunEventTemplateKind.Choice:
                    ConfigureChoiceTemplate(startTalk, null);
                    break; //ChoiceData, 선택지 2개와 다음 대화 2개 생성
                case RunEventTemplateKind.Battle:
                    ConfigureBattleTemplate(eventData);
                    break; //Battle : 시작 대화와 전투 진입, 거절 선택지, 승리 대화 생성
                case RunEventTemplateKind.DamageChallenge:
                    eventData.battleChallengeMode = RunEventBattleChallengeMode.DamageWithinTurns;
                    ConfigureBattleTemplate(eventData, true);
                    break; //DamageChallenge : Battle 템플릿에 Challenge 모드와 실패 대화 추가
                case RunEventTemplateKind.HealthOneBattle:
                    eventData.battleHealthEndMode = RunEventBattleHealthEndMode.HealthOne;
                    ConfigureBattleTemplate(eventData);
                    break; //HealthOneBattle : 체력 1 종료 모드 지정
                case RunEventTemplateKind.HealthPercentBattle:
                    eventData.battleHealthEndMode = RunEventBattleHealthEndMode.HealthPercent;
                    ConfigureBattleTemplate(eventData);
                    break; //HealthPercentBattle : 체력 비율 종료 모드 지정
                case RunEventTemplateKind.Transaction:
                    ConfigureChoiceTemplate(startTalk, RunEventActionKind.Transaction);
                    break;
                case RunEventTemplateKind.Chance:
                    ConfigureChoiceTemplate(startTalk, RunEventActionKind.Chance);
                    break;
                case RunEventTemplateKind.DoubleOrNothing:
                    ConfigureChoiceTemplate(startTalk, RunEventActionKind.DoubleOrNothing);
                    break;
                //Transaction, Chance, DoubleOrNothing: 시작 TalkData와 ChoiceData를 만든 뒤 첫 선택지에 해당 Action SO 연결
            }
			
            EditorUtility.SetDirty(eventData); //템플릿 설정이 변경된 RunEvent를 저장 대상으로 표시
            AssetDatabase.SaveAssets();
            return eventData;
        }
		
        //TalkData를 새로 생성하고 지정된 owner 필드에 즉시 연결
        public TalkDataListSO CreateTalkAndAssign(Object owner, string propertyPath,
            string folder, string suffix)
        {
            if (owner == null)
                return null; //빈 owner면 종료
			
            _repository.EnsureDefaultFolders();
            string safeName = RunEventAssetRepository.SanitizeAssetName(owner.name + suffix, "TalkData");
            //owner 이름과 suffix로 파일명 생성
            TalkDataListSO talkData = ScriptableObject.CreateInstance<TalkDataListSO>();
            talkData.talkDataList = new List<TalkValueData> //첫 대사 한 줄을 기본 생성
            {
                new TalkValueData
                {
                    Type = DirType.Left, //기본 방향은 DirType.Left
                    Text = string.Empty, //대사 내용은 빈 문자열
                    choiceData = null //Choice 연결은 null
                }
            };
			
            string path = CreateUniquePath(folder, safeName);
            CreateAsset(talkData, path, "Create Talk Data");
            AssignObjectReference(owner, propertyPath, talkData, "Connect Talk Data");
            //생성 후 AssignObjectReference()로 owner의 지정된 property path에 연결
            return talkData;
        }
		
        //TalkData의 특정 대사 줄에 ChoiceData를 생성해 연결
        public TalkChoiceDataSO CreateChoiceAndAssign(TalkDataListSO talkData, int lineIndex)
        {
            if (talkData == null || talkData.talkDataList == null ||
                lineIndex < 0 || lineIndex >= talkData.talkDataList.Count)
            {
                return null; //TalkData와 줄 인덱스가 유효한지 확인
            }
			
            _repository.EnsureDefaultFolders();
            TalkChoiceDataSO choiceData = ScriptableObject.CreateInstance<TalkChoiceDataSO>();
            choiceData.type = talkData.talkDataList[lineIndex].Type; //Choice 방향을 원래 대사의 Type과 동일하게 설정
            choiceData.choiceTextList = new List<ChoiceData> //기본 선택지 한 개 생성
            {
                new ChoiceData
                {
                    choiceText = "새 선택지", 
                    actionType = ChoiceActionType.Continue, //기본 행동은 ChoiceActionType.Continue
                    itemCosts = new List<ChoiceItemCost>() //비용 목록은 빈 리스트로 생성
                }
            };
			
            string safeName = RunEventAssetRepository.SanitizeAssetName
                ($"{talkData.name}_Choice_{lineIndex + 1}", "TalkChoice");
            
            string path = CreateUniquePath(RunEventAssetRepository.ChoiceFolder, safeName);
            CreateAsset(choiceData, path, "Create Talk Choice");
            
            AssignObjectReference(talkData,
                $"talkDataList.Array.data[{lineIndex}].choiceData", choiceData, "Connect Talk Choice");
            //talkDataList.Array.data[index].choiceData 경로에 연결
            return choiceData;
        }
		
        //특정 선택지의 nextTalkData에 새 TalkData를 생성해 연결
        public TalkDataListSO CreateNextTalkAndAssign(TalkChoiceDataSO choiceData, int choiceIndex)
        {
            if (!IsValidChoiceIndex(choiceData, choiceIndex))
                return null; //선택지 인덱스 검사를 먼저 수행
			
            return CreateTalkAndAssign(choiceData,
                $"choiceTextList.Array.data[{choiceIndex}].nextTalkData",
                RunEventAssetRepository.NextTalkFolder, $"_Next_{choiceIndex + 1}");
        }
		
        //Action SO를 생성한 뒤 특정 선택지의 eventAction 필드에 연결
        public RunEventActionSO CreateActionAndAssign(TalkChoiceDataSO choiceData, int choiceIndex,
            RunEventActionKind actionKind)
        {
            if (!IsValidChoiceIndex(choiceData, choiceIndex))
                return null;
			
            RunEventActionSO action = CreateAction(actionKind,
                $"{choiceData.name}_{actionKind}_{choiceIndex + 1}");
			
            if (action != null)
            {
                AssignObjectReference(choiceData,
                    $"choiceTextList.Array.data[{choiceIndex}].eventAction", action, "Connect Run Event Action");
            }
			
            return action;
        }
		
        //RunEventActionKind에 따라 실제 SO 타입을 선택
        public RunEventActionSO CreateAction(RunEventActionKind actionKind, string assetName)
        {
            _repository.EnsureDefaultFolders();
            RunEventActionSO action = actionKind switch
            {
                RunEventActionKind.Transaction => ScriptableObject.CreateInstance<RunEventTransactionSO>(),
                //Transaction → RunEventTransactionSO
                RunEventActionKind.Chance => ScriptableObject.CreateInstance<RunEventChanceSO>(),
                //Chance → RunEventChanceSO
                RunEventActionKind.DoubleOrNothing => ScriptableObject.CreateInstance<RunEventDoubleOrNothingSO>(),
                //DoubleOrNothing → RunEventDoubleOrNothingSO
                _ => null
            };
			
            if (action == null)
                return null;
			
            string safeName = RunEventAssetRepository.SanitizeAssetName(assetName, actionKind.ToString());
            string path = CreateUniquePath(RunEventAssetRepository.ActionFolder, safeName);
            CreateAsset(action, path, "Create Run Event Action");
            //안전한 이름과 고유 경로를 만든 뒤 Action 폴더에 저장
            return action;
        }
		
        //기존 TalkData에 새 대사 한 줄을 추가
        public void AddTalkLine(TalkDataListSO talkData)
        {
            if (talkData == null)
                return;
			
            Undo.RecordObject(talkData, "Add Talk Line");  //Undo.RecordObject()를 먼저 호출하며
            talkData.talkDataList ??= new List<TalkValueData>();
            talkData.talkDataList.Add(new TalkValueData  //리스트가 null이면 새 리스트를 만든 뒤 기본 왼쪽 방향의 빈 대사를 추가
            {
                Type = DirType.Left,
                Text = string.Empty,
                choiceData = null
            });
            EditorUtility.SetDirty(talkData);
        }
		
        //ChoiceData에 기본 선택지 한 개를 추가
        public void AddChoiceOption(TalkChoiceDataSO choiceData)
        {
            if (choiceData == null)
                return;
			
            Undo.RecordObject(choiceData, "Add Choice Option");
            choiceData.choiceTextList ??= new List<ChoiceData>();
            choiceData.choiceTextList.Add(new ChoiceData
            {
                choiceText = "새 선택지",
                actionType = ChoiceActionType.Continue,
                itemCosts = new List<ChoiceItemCost>()
            });
            EditorUtility.SetDirty(choiceData);
        }
		
        //문자열로 받은 SerializedProperty 경로에 SO를 연결하는 공통 메서드
        public void AssignObjectReference(Object owner, string propertyPath,
            Object value, string undoName)
        {
            if (owner == null || string.IsNullOrWhiteSpace(propertyPath))
                return; //owner와 경로 검사
			
            SerializedObject serializedOwner = new SerializedObject(owner); //SerializedObject 생성
            SerializedProperty property = serializedOwner.FindProperty(propertyPath); //경로로 Property 탐색
			
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogError($"Run Event Editor : 연결할 필드를 찾을 수 없습니다. {owner.name}.{propertyPath}", owner);
                return; //ObjectReference 필드가 아니면 오류 출력
            }
			
            Undo.RecordObject(owner, undoName);
            property.objectReferenceValue = value; //objectReferenceValue 변경
            serializedOwner.ApplyModifiedProperties();
            EditorUtility.SetDirty(owner); //변경 적용 및 Dirty 표시
        }
        
        //RunEvent만 복제하거나 연결된 Talk, Choice, Action까지 함께 복제
        public RunEventSO DuplicateEvent(RunEventSO source, bool duplicateConnectedAssets)
        {
            if (source == null)
                return null;
			
            _repository.EnsureDefaultFolders();
            Dictionary<Object, Object> duplicatedAssets = new Dictionary<Object, Object>();
            //원본 SO -> 복제 SO를 저장하는 Dictionary
            //같은 TalkData가 여러 곳에서 참조되면 한 번만 복제하고 새 참조들도 같은 복제본을 가리키게 함,
            //순환 대화 그래프가 있어도 무한 재귀에 빠지지 않게 해주는 핵심 장치
            
            RunEventSO duplicate = CloneAsset(source, RunEventAssetRepository.RunEventFolder,
                source.name + "_Copy", duplicatedAssets);
            //복제 직후에는 기존 TalkData, ChoiceData, Action SO 참조를 원본과 공유
			
            if (!duplicateConnectedAssets)
                return duplicate;
			
            duplicate.talkStage.talkData = DuplicateTalk(source.talkStage.talkData,
                duplicatedAssets, RunEventAssetRepository.MainTalkFolder);
            duplicate.victoryTalkStage.talkData = DuplicateTalk(source.victoryTalkStage.talkData,
                duplicatedAssets, RunEventAssetRepository.AfterBattleFolder);
            duplicate.failureTalkStage.talkData = DuplicateTalk(source.failureTalkStage.talkData,
                duplicatedAssets, RunEventAssetRepository.AfterBattleFolder);
            //루트 이벤트를 복제한 뒤 다음 그래프도 재귀적으로 복제
            //시작 TalkData, 승리, 실패 TalkData, 각 대사의 ChoiceData,
            //선택지의 nextTalkData, 랜덤 TalkData 목록, Action SO와 Action 결과의 nextTalkData
            
            //Prefab, Encounter, Presentation, 보상 대상 같은 외부 데이터는 그대로 공유
            EditorUtility.SetDirty(duplicate);
            AssetDatabase.SaveAssets();
            return duplicate;
        }
		
        //에셋 경로를 찾고 파일명으로 사용할 수 없는 문자를 정리한 뒤 AssetDatabase.RenameAsset()으로 이름을 변경
        public bool RenameAsset(Object target, string newName)
        {
            if (target == null || string.IsNullOrWhiteSpace(newName))
                return false;
			
            string path = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrWhiteSpace(path))
                return false;
			
            string safeName = RunEventAssetRepository.SanitizeAssetName(newName, target.name);
            string error = AssetDatabase.RenameAsset(path, safeName);
			
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Run Event Editor : 이름 변경 실패 - {error}", target);
                return false;
            }
			
            AssetDatabase.SaveAssets();
            return true;
        }
		
        //TalkData를 복제하고 각 대사 줄의 choiceData를 복제본으로 교체
        //원래 저장된 TalkData 종류에 따라 Main, Next, AfterBattle 폴더를 유지
        private TalkDataListSO DuplicateTalk(TalkDataListSO source, Dictionary<Object, Object> duplicatedAssets, string preferredFolder)
        {
            if (source == null)
                return null;
			
            if (duplicatedAssets.TryGetValue(source, out Object existing))
                return existing as TalkDataListSO;
			
            string folder = ResolveTalkFolder(source, preferredFolder);
            TalkDataListSO duplicate = CloneAsset(source, folder, source.name + "_Copy", duplicatedAssets);
			
            if (source.talkDataList == null || duplicate.talkDataList == null)
                return duplicate;
			
            for (int i = 0; i < source.talkDataList.Count && i < duplicate.talkDataList.Count; i++)
            {
                TalkValueData value = duplicate.talkDataList[i];
                value.choiceData = DuplicateChoice(source.talkDataList[i].choiceData, duplicatedAssets);
                duplicate.talkDataList[i] = value;
            }
			
            EditorUtility.SetDirty(duplicate);
            return duplicate;
        }
		
        //ChoiceData를 복제한 뒤 각 선택지의 다음 연결을 다시 복제
        private TalkChoiceDataSO DuplicateChoice(TalkChoiceDataSO source, Dictionary<Object, Object> duplicatedAssets)
        {
            if (source == null)
                return null;
			
            if (duplicatedAssets.TryGetValue(source, out Object existing))
                return existing as TalkChoiceDataSO;
			
            TalkChoiceDataSO duplicate = CloneAsset(source, RunEventAssetRepository.ChoiceFolder,
                source.name + "_Copy", duplicatedAssets);
			
            if (source.choiceTextList == null || duplicate.choiceTextList == null)
                return duplicate;
			
            for (int i = 0; i < source.choiceTextList.Count && i < duplicate.choiceTextList.Count; i++)
            {
                ChoiceData sourceChoice = source.choiceTextList[i];
                ChoiceData choice = duplicate.choiceTextList[i];
                choice.beforeEventActionTalkData = DuplicateTalk(sourceChoice.beforeEventActionTalkData,
                    duplicatedAssets, RunEventAssetRepository.NextTalkFolder);
                choice.nextTalkData = DuplicateTalk(sourceChoice.nextTalkData,
                    duplicatedAssets, RunEventAssetRepository.NextTalkFolder);
                choice.eventAction = DuplicateAction(sourceChoice.eventAction, duplicatedAssets);
				
                if (sourceChoice.randomTalkList != null)
                {
                    choice.randomTalkList = new List<TalkDataListSO>();
                    for (int talkIndex = 0; talkIndex < sourceChoice.randomTalkList.Count; talkIndex++)
                    {
                        choice.randomTalkList.Add(DuplicateTalk(sourceChoice.randomTalkList[talkIndex],
                            duplicatedAssets, RunEventAssetRepository.NextTalkFolder));
                    }
                }
				
                duplicate.choiceTextList[i] = choice;
            }
			
            EditorUtility.SetDirty(duplicate);
            return duplicate;
        }
		
        //Action SO를 복제하고 결과에 연결된 TalkData를 새 복제본으로 교체
        private RunEventActionSO DuplicateAction(RunEventActionSO source, Dictionary<Object, Object> duplicatedAssets)
        {
            if (source == null)
                return null;
			
            if (duplicatedAssets.TryGetValue(source, out Object existing))
                return existing as RunEventActionSO;
			
            RunEventActionSO duplicate = CloneAsset(source, RunEventAssetRepository.ActionFolder,
                source.name + "_Copy", duplicatedAssets);
			
            switch (duplicate)
            {
                case RunEventTransactionSO transaction when source is RunEventTransactionSO sourceTransaction:
                    transaction.successOutcome.nextTalkData = DuplicateTalk(
                        sourceTransaction.successOutcome.nextTalkData, duplicatedAssets,
                        RunEventAssetRepository.NextTalkFolder);
                    break;
                case RunEventChanceSO chance when source is RunEventChanceSO sourceChance:
                    chance.successOutcome.nextTalkData = DuplicateTalk(sourceChance.successOutcome.nextTalkData,
                        duplicatedAssets, RunEventAssetRepository.NextTalkFolder);
                    chance.failureOutcome.nextTalkData = DuplicateTalk(sourceChance.failureOutcome.nextTalkData,
                        duplicatedAssets, RunEventAssetRepository.NextTalkFolder);
                    break;
                case RunEventDoubleOrNothingSO gamble when source is RunEventDoubleOrNothingSO sourceGamble:
                    gamble.successOutcome.nextTalkData = DuplicateTalk(sourceGamble.successOutcome.nextTalkData,
                        duplicatedAssets, RunEventAssetRepository.NextTalkFolder);
                    gamble.failureOutcome.nextTalkData = DuplicateTalk(sourceGamble.failureOutcome.nextTalkData,
                        duplicatedAssets, RunEventAssetRepository.NextTalkFolder);
                    break;
            }
			
            EditorUtility.SetDirty(duplicate);
            return duplicate;
        }
		
        //실제 공통 복제 함수
        private T CloneAsset<T>(T source, string folder, string assetName,
            Dictionary<Object, Object> duplicatedAssets) where T : ScriptableObject
        {
            T duplicate = Object.Instantiate(source);
            duplicate.name = assetName;
            
            string path = CreateUniquePath(folder,
                RunEventAssetRepository.SanitizeAssetName(assetName, typeof(T).Name));
            
            duplicatedAssets[source] = duplicate;
            CreateAsset(duplicate, path, "Duplicate Run Event Asset");
            return duplicate;
        }
		
        //기존 TalkData 경로를 보고 적절한 복제 폴더를 결정
        private static string ResolveTalkFolder(TalkDataListSO source, string fallbackFolder)
        {
            string path = AssetDatabase.GetAssetPath(source).Replace('\\', '/');
			
            if (path.Contains("/AfterBattleData/"))
                return RunEventAssetRepository.AfterBattleFolder;
			
            if (path.Contains("/NextTalkData/"))
                return RunEventAssetRepository.NextTalkFolder;
			
            if (path.Contains("/00_TalkData/"))
                return RunEventAssetRepository.MainTalkFolder;
			
            return fallbackFolder;
        }
		
        //ChoiceData와 인덱스 범위를 검사
        private static bool IsValidChoiceIndex(TalkChoiceDataSO choiceData, int choiceIndex)
        {
            return choiceData != null && choiceData.choiceTextList != null &&
                   choiceIndex >= 0 && choiceIndex < choiceData.choiceTextList.Count;
        }
		
        //같은 이름이 있으면 Unity가 번호가 붙은 고유 경로를 생성
        private static string CreateUniquePath(string folder, string assetName)
        {
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{assetName}.asset");
        }
		
        //생성, Undo 등록, Dirty, 저장, Import를 한 번에 처리
        private static void CreateAsset(ScriptableObject asset, string path, string undoName)
        {
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, undoName);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
        }
		
        //일반 Choice 또는 Action Choice의 기본 구조를 생성
        private void ConfigureChoiceTemplate(TalkDataListSO startTalk, RunEventActionKind? actionKind)
        {
            if (startTalk == null)
                return;
			
            TalkChoiceDataSO choiceData = CreateChoiceAndAssign(startTalk, 0);
            if (choiceData == null)
                return;
			
            if (actionKind.HasValue)
            {
                CreateActionAndAssign(choiceData, 0, actionKind.Value);
                return;
            }
			
            AddChoiceOption(choiceData);
            CreateNextTalkAndAssign(choiceData, 0);
            CreateNextTalkAndAssign(choiceData, 1);
        }
		
        //전투 진입, 거절 선택지와 승리 대화를 만들고 필요하면 실패 대화도 생성
        private void ConfigureBattleTemplate(RunEventSO eventData, bool includeFailureTalk = false)
        {
            TalkChoiceDataSO choiceData = CreateChoiceAndAssign(eventData.talkStage.talkData, 0);
            if (choiceData != null)
            {
                AddChoiceOption(choiceData);
                ChoiceData enterBattleChoice = choiceData.choiceTextList[0];
                enterBattleChoice.choiceText = "전투한다";
                enterBattleChoice.actionType = ChoiceActionType.Continue;
                enterBattleChoice.runEventBattleAction = RunEventBattleAction.EnterBattle;
                choiceData.choiceTextList[0] = enterBattleChoice;
				
                ChoiceData skipBattleChoice = choiceData.choiceTextList[1];
                skipBattleChoice.choiceText = "거절한다";
                skipBattleChoice.actionType = ChoiceActionType.Continue;
                skipBattleChoice.runEventBattleAction = RunEventBattleAction.SkipBattle;
                choiceData.choiceTextList[1] = skipBattleChoice;
                CreateNextTalkAndAssign(choiceData, 1);
                EditorUtility.SetDirty(choiceData);
            }
			
            eventData.victoryTalkStage.actionMode = InteractActionMode.TalkOnly;
            CreateTalkAndAssign(eventData, "victoryTalkStage.talkData",
                RunEventAssetRepository.AfterBattleFolder, "_Victory");
			
            if (!includeFailureTalk)
                return;
			
            eventData.failureTalkStage.actionMode = InteractActionMode.TalkOnly;
            CreateTalkAndAssign(eventData, "failureTalkStage.talkData",
                RunEventAssetRepository.AfterBattleFolder, "_Failure");
        }
		
    }
}
#endif
