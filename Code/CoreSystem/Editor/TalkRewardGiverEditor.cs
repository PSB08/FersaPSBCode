#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.Editor
{
    [CustomEditor(typeof(TalkRewardGiver))]
    public class TalkRewardGiverEditor : UnityEditor.Editor
    {
        private ReorderableList _list;

        private void OnEnable()
        {
            _list = new ReorderableList(serializedObject, 
                serializedObject.FindProperty("rewardEntries"), true, true, true, true);

            _list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Talk Reward Entries");
            };

            _list.elementHeightCallback = index =>
            {
                var element = _list.serializedProperty.GetArrayElementAtIndex(index);
                var typeProp = element.FindPropertyRelative("rewardType");

                float lineHeight = EditorGUIUtility.singleLineHeight + 5f;
                int rowCount = 2;

                var rewardType = (TalkRewardGiver.RewardType)typeProp.enumValueIndex;

                switch (rewardType)
                {
                    case TalkRewardGiver.RewardType.DropTable:
                        rowCount += 2;
                        break;

                    case TalkRewardGiver.RewardType.GiveSkill:
                    case TalkRewardGiver.RewardType.GiveRandomSkill:
                        rowCount += 1;
                        break;

                    case TalkRewardGiver.RewardType.DropTableAndSkill:
                        rowCount += 3;
                        break;

                    case TalkRewardGiver.RewardType.GiveRelic:
                        rowCount += 1;
                        break;

                    case TalkRewardGiver.RewardType.GiveRandomRelic:
                        rowCount += 1;
                        break;

                    case TalkRewardGiver.RewardType.ActiveObject:
                        rowCount += 1;
                        break;

                    case TalkRewardGiver.RewardType.HealPlayer:
                        rowCount += 2;
                        break;
                }

                return lineHeight * rowCount + 5f;
            };

            _list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = _list.serializedProperty.GetArrayElementAtIndex(index);

                float line = EditorGUIUtility.singleLineHeight;
                float spacing = 5f;

                rect.y += 2f;

                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), 
                    element.FindPropertyRelative("rewardKey"));

                rect.y += line + spacing;

                var typeProp = element.FindPropertyRelative("rewardType");

                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), typeProp);

                rect.y += line + spacing;

                var rewardType = (TalkRewardGiver.RewardType)typeProp.enumValueIndex;

                switch (rewardType)
                {
                    case TalkRewardGiver.RewardType.DropTable:
                    {
                        DrawDropTableFields(element, ref rect, line, spacing);
                        break;
                    }

                    case TalkRewardGiver.RewardType.GiveSkill:
                    case TalkRewardGiver.RewardType.GiveRandomSkill:
                    {
                        DrawSkillDropTableFields(element, ref rect, line, spacing);
                        break;
                    }

                    case TalkRewardGiver.RewardType.DropTableAndSkill:
                    {
                        DrawDropTableFields(element, ref rect, line, spacing);
                        rect.y += spacing;
                        DrawSkillDropTableFields(element, ref rect, line, spacing);
                        break;
                    }

                    case TalkRewardGiver.RewardType.GiveRelic:
                    {
                        EditorGUI.PropertyField(
                            new Rect(rect.x, rect.y, rect.width, line),
                            element.FindPropertyRelative("rewardRelic")
                        );
                        break;
                    }

                    case TalkRewardGiver.RewardType.GiveRandomRelic:
                    {
                        EditorGUI.PropertyField(
                            new Rect(rect.x, rect.y, rect.width, line),
                            element.FindPropertyRelative("relicList")
                        );
                        break;
                    }

                    case TalkRewardGiver.RewardType.ActiveObject:
                    {
                        EditorGUI.PropertyField(
                            new Rect(rect.x, rect.y, rect.width, line),
                            element.FindPropertyRelative("rewardObject")
                        );
                        break;
                    }

                    case TalkRewardGiver.RewardType.HealPlayer:
                    {
                        EditorGUI.PropertyField(
                            new Rect(rect.x, rect.y, rect.width, line),
                            element.FindPropertyRelative("healValue")
                        );
                        rect.y += line + spacing;

                        EditorGUI.PropertyField(
                            new Rect(rect.x, rect.y, rect.width, line),
                            element.FindPropertyRelative("healMode")
                        );
                        break;
                    }
                }
            };
            
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inventory"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skillContainer"));
            EditorGUILayout.Space();

            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDropTableFields(SerializedProperty element, ref Rect rect, float line, float spacing)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("dropTable"));

            rect.y += line + spacing;

            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), 
                element.FindPropertyRelative("useItemDropper"));

            rect.y += line + spacing;
        }

        private void DrawSkillDropTableFields(SerializedProperty element, ref Rect rect, float line, float spacing)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line),
                element.FindPropertyRelative("skillDropTable"));
        }
        
    }
}
#endif
