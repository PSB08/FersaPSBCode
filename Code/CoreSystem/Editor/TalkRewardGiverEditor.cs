#if UNITY_EDITOR
using UnityEditor;
#endif
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

            _list.elementHeightCallback = (int index) => {
                var element = _list.serializedProperty.GetArrayElementAtIndex(index);
                var typeProp = element.FindPropertyRelative("rewardType");
                
                float lineHeight = EditorGUIUtility.singleLineHeight + 2;
                int rowCount = 2;

                if (typeProp.enumValueIndex == (int)TalkRewardGiver.RewardType.DropTable)
                    rowCount += 3;
                else
                    rowCount += 2;

                return lineHeight * rowCount;
            };

            _list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = _list.serializedProperty.GetArrayElementAtIndex(index);
                
                float line = EditorGUIUtility.singleLineHeight;

                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), 
                    element.FindPropertyRelative("rewardKey"));
                rect.y += line + 5;

                var typeProp = element.FindPropertyRelative("rewardType");
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), typeProp);
                rect.y += line + 5;

                if (typeProp.enumValueIndex == (int)TalkRewardGiver.RewardType.DropTable)
                {
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), 
                        element.FindPropertyRelative("dropTable"));
                    rect.y += line + 5;
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), 
                        element.FindPropertyRelative("useItemDropper"));
                }
                else
                {
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), 
                        element.FindPropertyRelative("rewardObject"));
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inventory"));
            EditorGUILayout.Space();

            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    
    }
}