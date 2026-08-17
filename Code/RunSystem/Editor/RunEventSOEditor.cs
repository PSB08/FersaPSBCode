#if UNITY_EDITOR
using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Work.PSB.Code.CoreSystem;

namespace Work.PSB.Code.RunSystem.Editor
{
    //RunEventSO의 전투 조건과 보상 종류에 맞춰 필요한 필드만 표시
    [CustomEditor(typeof(RunEventSO))]
    public class RunEventSOEditor : UnityEditor.Editor
    {
        private ReorderableList _rewardList; //순서 변경과 추가, 삭제가 가능한 보상 목록
        
        //보상 목록의 제목, 항목 높이와 실제 그리기 메서드를 설정
        private void OnEnable()
        {
            SerializedProperty rewardEntries = serializedObject.FindProperty("rewardEntries");
            _rewardList = new ReorderableList(serializedObject, rewardEntries, true, true, true, true); 
            //true 4개 -> 드래그 가능, 헤더 표시, 추가 버튼 표시, 삭제 버튼 표시
            
            _rewardList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Run Event Reward Entries");
            };
            //보상 목록 위 헤더에 Run Event Reward Entries라는 제목을 추가
            
            //현재 보상 종류에 맞춰 한 항목이 사용할 세로 높이를 계산
            _rewardList.elementHeightCallback = index =>
            {
                SerializedProperty element = _rewardList.serializedProperty.GetArrayElementAtIndex(index);
                SerializedProperty typeProp = element.FindPropertyRelative("rewardType");
                
                float lineHeight = EditorGUIUtility.singleLineHeight + 5f;
                int rowCount = 2;
                
                var rewardType = (TalkRewardGiver.RewardType)typeProp.enumValueIndex;
                //기본 rowCount = 2: rewardKey, rewardType
                
                switch (rewardType)
                {
                    case TalkRewardGiver.RewardType.DropTable:
                        rowCount += 2;
                        break; //DropTable: dropTable, useItemDropper 2줄 추가
                    
                    case TalkRewardGiver.RewardType.GiveSkill:
                    case TalkRewardGiver.RewardType.GiveRandomSkill:
                        rowCount += 1;
                        break; //GiveSkill, GiveRandomSkill: skillDropTable 1줄 추가
                    
                    case TalkRewardGiver.RewardType.DropTableAndSkill:
                        rowCount += 3;
                        break; //DropTableAndSkill: 드롭 테이블 2줄과 스킬 테이블 1줄 추가
                    
                    case TalkRewardGiver.RewardType.GiveRelic:
                    case TalkRewardGiver.RewardType.GiveRandomRelic:
                    case TalkRewardGiver.RewardType.ActiveObject:
                        rowCount += 1;
                        break; //GiveRelic, GiveRandomRelic, ActiveObject: 대상 필드 1줄 추가
                    
                    case TalkRewardGiver.RewardType.HealPlayer:
                        rowCount += 2;
                        break; //HealPlayer: 회복 수치와 회복 방식 2줄 추가
                    
                    case TalkRewardGiver.RewardType.GiveAlly:
                    case TalkRewardGiver.RewardType.GiveRandomAlly:
                        rowCount += 3;
                        break; //GiveAlly, GiveRandomAlly: 대상, 자동 장착, 중복 회복 비율 3줄 추가
                }
                
                return lineHeight * rowCount + 5f; 
                //EditorGUIUtility.singleLineHeight + 5f를 한 줄 높이로 사용하고 마지막에 여백 5px 추가
            };
            
            _rewardList.drawElementCallback = DrawRewardEntry;
            //각 보상 항목의 실제 필드 렌더링은 DrawRewardEntry()가 담당하도록 연결
        }
        
        //RunEventSO Inspector 전체를 그리는 진입점
        public override void OnInspectorGUI()
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.38f, 105f, 160f);
			//현재 Inspector 너비의 38%를 Label 영역으로 사용하되 최소 105px, 최대 160px로 제한
            
            try
            {
                serializedObject.Update(); //실제 에셋의 최신 값을 SerializedObject에 읽어옵니다.
				
                DrawEntitySection();
                DrawTalkSection();
                DrawEventBattleSection();
                DrawRewardSection(); //섹션들 그리기
				
                serializedObject.ApplyModifiedProperties();
                //Inspector에서 바꾼 값을 실제 RunEventSO에 반영
            }
            finally
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
                //처리 결과와 상관없이 Inspector Label 너비를 원래대로 복구
            }
        }
        
        //이벤트 NPC와 화면 표시에 필요한 Entity 관련 필드를 표시
        private void DrawEntitySection()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("entityPrefab"), new GUIContent("Entity Prefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("systemPrefab"), new GUIContent("System Prefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorController"), new GUIContent("Animator"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disableEntityAfterFinished"),
                new GUIContent("Disable After Finish"));
            EditorGUILayout.Space();
        }
        
        //시작 TalkStage와 내부 필드를 모두 펼쳐 표시
        private void DrawTalkSection()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("talkStage"), 
                new GUIContent("Talk Stage"), true);
            //talkStage 전체를 자식 필드까지 펼쳐 표시합니다.
            //true는 TalkStage 내부의 actionMode, talkData, rewardKey 같은 하위 필드도 함께 그리라는 의미
        }
        
        //이벤트 전투 필드를 조건부로 표시
        private void DrawEventBattleSection()
        {
            SerializedProperty encounter = serializedObject.FindProperty("battleEncounter");
            EditorGUILayout.PropertyField(encounter, new GUIContent("Encounter"));
            //battleEncounter : 실제 적 구성과 전투 정보, Encounter가 없으면 나머지 전투 옵션은 숨겨짐
            
            if (encounter.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("battlePresentation"),
                    new GUIContent("Presentation"));
                //battlePresentation : 전투 연출 데이터
                EditorGUILayout.PropertyField(serializedObject.FindProperty("victoryTalkStage"),
                    new GUIContent("Victory Talk"), true);
                //victoryTalkStage : 승리 후 대화
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rollbackBattleLootOnVictory"),
                    new GUIContent("Rollback Battle Loot"));
                //rollbackBattleLootOnVictory : 승리 시 일반 전투 전리품을 되돌릴지 여부
                DrawBattleChallengeSection(encounter);
                
                RunEventBattleChallengeMode challengeMode = (RunEventBattleChallengeMode)
                    serializedObject.FindProperty("battleChallengeMode").enumValueIndex;
                //전투 Challenge 설정
                if (challengeMode == RunEventBattleChallengeMode.None)
                    DrawBattleHealthEndSection(encounter);
                //Challenge가 없을 때만 체력 종료 조건
            }
            
            EditorGUILayout.Space();
        }
        
        //Challenge 모드와 세부 조건을 그립니다.
        private void DrawBattleChallengeSection(SerializedProperty encounter)
        {
            SerializedProperty challengeMode = serializedObject.FindProperty("battleChallengeMode");
            //battleChallengeMode : Challenge 없음 또는 제한 턴 피해 Challenge
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(challengeMode, new GUIContent("Challenge Mode"));
            
            RunEventBattleChallengeMode mode = (RunEventBattleChallengeMode)challengeMode.enumValueIndex;
            if (mode == RunEventBattleChallengeMode.None)
                return; //None이면 즉시 종료하여 나머지 필드를 숨김
            
            BattleEncounterSO encounterData = encounter.objectReferenceValue as BattleEncounterSO;
            if (encounterData == null || encounterData.enemies == null || encounterData.enemies.Length == 0)
            {
                EditorGUILayout.HelpBox("Battle Encounter has no enemies.", MessageType.Warning);
                return;
            } //Encounter에 적이 없으면 경고 표시
            
            SerializedProperty targetEnemyIndex =
                serializedObject.FindProperty("battleChallengeTargetEnemyIndex");
            //battleChallengeTargetEnemyIndex : Challenge 대상 적
            string[] enemyOptions = CreateEnemyOptions(encounterData);
            
            targetEnemyIndex.intValue = Mathf.Clamp(targetEnemyIndex.intValue, 0, enemyOptions.Length - 1);
            targetEnemyIndex.intValue = EditorGUILayout.Popup("Challenge Target Enemy",
                targetEnemyIndex.intValue, enemyOptions);
            //대상 적 리스트를 생성, Clamp로 보정
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("battleChallengeTurnLimit"),
                new GUIContent("Turn Limit"));
            //battleChallengeTurnLimit : 제한 턴
            EditorGUILayout.PropertyField(serializedObject.FindProperty("battleChallengeRequiredDamage"),
                new GUIContent("Required Damage"));
            //battleChallengeRequiredDamage : 제한 안에 가해야 하는 피해
            EditorGUILayout.PropertyField(serializedObject.FindProperty("battleChallengeProtectTarget"),
                new GUIContent("Protect Target"));
            //battleChallengeProtectTarget : 대상 적을 보호해야 하는지
            EditorGUILayout.PropertyField(serializedObject.FindProperty("battleChallengeEndDelay"),
                new GUIContent("End Delay"));
            //battleChallengeEndDelay : 성공·실패 판정 후 종료 지연
            EditorGUILayout.PropertyField(serializedObject.FindProperty("failureTalkStage"),
                new GUIContent("Failure Talk"), true);
            //failureTalkStage : 실패 후 대화
            
            SerializedProperty healthMode = serializedObject.FindProperty("battleHealthEndMode");
            if (healthMode.enumValueIndex != (int)RunEventBattleHealthEndMode.None)
            {
                EditorGUILayout.HelpBox("Battle Challenge가 켜져 있으면 기존 Health End 설정은 사용하지 않습니다.",
                    MessageType.Info);
            }
            //Challenge가 켜져 있는데 battleHealthEndMode 값도 남아 있으면 기존 Health End 설정은 사용되지 않는다는 안내를 표시
        }
        
        //Challenge가 없을 때 체력 기반 전투 종료 조건을 표시합니다.
        private void DrawBattleHealthEndSection(SerializedProperty encounter)
        {
            SerializedProperty endMode = serializedObject.FindProperty("battleHealthEndMode");
            //battleHealthEndMode : 없음, 체력 1, 체력 비율
            SerializedProperty targetEnemyIndex = serializedObject.FindProperty("battleHealthTargetEnemyIndex");
            //battleHealthTargetEnemyIndex : 감시할 Encounter 적
            SerializedProperty targetPercent = serializedObject.FindProperty("battleHealthTargetPercent");
            //battleHealthTargetPercent : 체력 비율 종료 기준
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(endMode, new GUIContent("Health End Mode"));
            
            RunEventBattleHealthEndMode mode = (RunEventBattleHealthEndMode)endMode.enumValueIndex;
            if (mode == RunEventBattleHealthEndMode.None)
                return;
            
            BattleEncounterSO encounterData = encounter.objectReferenceValue as BattleEncounterSO;
            if (encounterData == null || encounterData.enemies == null || encounterData.enemies.Length == 0)
            {
                EditorGUILayout.HelpBox("Battle Encounter에 적이 없습니다.", MessageType.Warning);
                return;
            }
            
            string[] enemyOptions = CreateEnemyOptions(encounterData);
            
            targetEnemyIndex.intValue = Mathf.Clamp(targetEnemyIndex.intValue, 0, enemyOptions.Length - 1);
            targetEnemyIndex.intValue = EditorGUILayout.Popup("Target Enemy", targetEnemyIndex.intValue, enemyOptions);
            
            if (mode == RunEventBattleHealthEndMode.HealthPercent)
                EditorGUILayout.Slider(targetPercent, 0.01f, 1f, new GUIContent("Target Health Percent"));
            //HealthPercent일 때만 0.01~1 범위 Slider를 표시
            //HealthOne은 고정 조건이므로 비율 필드가 필요 없음
        }
        
        //Encounter의 enemies 배열을 Popup용 문자열 배열로 변환
        private static string[] CreateEnemyOptions(BattleEncounterSO encounterData)
        {
            string[] enemyOptions = new string[encounterData.enemies.Length];
            for (int i = 0; i < encounterData.enemies.Length; i++)
            {
                var enemy = encounterData.enemies[i];
                enemyOptions[i] = enemy != null ? $"Element {i} - {enemy.name}" : $"Element {i} - None";
            } //적 SO가 있으면 이름을 표시하고, 배열 요소가 비어 있으면 None으로 표시
            
            return enemyOptions;
        }
        
        //Talk ID와 순서 변경이 가능한 보상 목록을 표시
        private void DrawRewardSection()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("talkId"), new GUIContent("Talk ID"));
            //talkId : 이벤트와 대화 보상을 구분하는 ID
            EditorGUILayout.Space();
            _rewardList.DoLayoutList();
            //_rewardList.DoLayoutList() : OnEnable()에서 구성한 보상 목록을 실제로 그림
        }
        
        //보상 하나의 실제 필드를 그림
        private void DrawRewardEntry(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _rewardList.serializedProperty.GetArrayElementAtIndex(index);
            
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = 5f;
            
            rect.y += 2f;
            
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("rewardKey"));
            
            rect.y += line + spacing;
            
            SerializedProperty typeProp = element.FindPropertyRelative("rewardType");
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), typeProp);
            
            rect.y += line + spacing;
            
            var rewardType = (TalkRewardGiver.RewardType)typeProp.enumValueIndex;
            //rewardKey와 rewardType을 무조건 그리고, 타입에 따라 필요한 필드를 추가
            
            switch (rewardType)
            {
                case TalkRewardGiver.RewardType.DropTable:
                    DrawDropTableFields(element, ref rect, line, spacing);
                    break; //DropTable : dropTable, useItemDropper
                
                case TalkRewardGiver.RewardType.GiveSkill:
                case TalkRewardGiver.RewardType.GiveRandomSkill:
                    DrawSkillDropTableFields(element, ref rect, line);
                    break; //GiveSkill, GiveRandomSkill : skillDropTable
                
                case TalkRewardGiver.RewardType.DropTableAndSkill:
                    DrawDropTableFields(element, ref rect, line, spacing);
                    rect.y += spacing;
                    DrawSkillDropTableFields(element, ref rect, line);
                    break; //DropTableAndSkill : 드롭 테이블 관련 필드와 스킬 테이블
                
                case TalkRewardGiver.RewardType.GiveRelic:
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                        element.FindPropertyRelative("rewardRelic"));
                    break; //GiveRelic : rewardRelic
                
                case TalkRewardGiver.RewardType.GiveRandomRelic:
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                        element.FindPropertyRelative("relicList"));
                    break; //GiveRandomRelic : relicList
                
                case TalkRewardGiver.RewardType.ActiveObject:
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                        element.FindPropertyRelative("rewardObject"));
                    break; //ActiveObject : rewardObject
                
                case TalkRewardGiver.RewardType.HealPlayer:
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                        element.FindPropertyRelative("healValue"));
                    rect.y += line + spacing;
                    
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                        element.FindPropertyRelative("healMode"));
                    break; //HealPlayer : healValue, healMode
                
                case TalkRewardGiver.RewardType.GiveAlly:
                    DrawAllyFields(element, ref rect, line, spacing);
                    break; //GiveAlly : Ally 관련 필드
                
                case TalkRewardGiver.RewardType.GiveRandomAlly:
                    DrawRandomAllyFields(element, ref rect, line, spacing);
                    break; //GiveRandomAlly : 랜덤 Ally 관련 필드
            }
        }
        
        //dropTable과 useItemDropper를 표시
        private static void DrawDropTableFields(SerializedProperty element, ref Rect rect, float line, float spacing)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("dropTable"));
            
            rect.y += line + spacing;
            
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("useItemDropper"));
            
            rect.y += line + spacing;
        }
        
        //skillDropTable을 표시
        private static void DrawSkillDropTableFields(SerializedProperty element, ref Rect rect, float line)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("skillDropTable"));
        }
        
        //ally, autoEquipAlly, duplicateAllyHealPercent를 표시
        private static void DrawAllyFields(SerializedProperty element, ref Rect rect, float line, float spacing)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("ally"));
            
            rect.y += line + spacing;
            
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("autoEquipAlly"));
            
            rect.y += line + spacing;
            
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("duplicateAllyHealPercent"));
        }
        
        //allyDatabase, autoEquipAlly, duplicateAllyHealPercent를 표시
        private static void DrawRandomAllyFields(SerializedProperty element, ref Rect rect, float line, float spacing)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("allyDatabase"));
            
            rect.y += line + spacing;
            
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("autoEquipAlly"));
            
            rect.y += line + spacing;
            
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), 
                element.FindPropertyRelative("duplicateAllyHealPercent"));
        }
        
    }
}
#endif
