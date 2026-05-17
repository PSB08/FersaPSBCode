using UnityEditor;
using UnityEngine;
using PSB.Code.BattleCode.Enemies.Phases;

namespace Work.PSB.Code.BattleCode.Enemies.Editor
{
    [CustomPropertyDrawer(typeof(EnemyPhaseData))]
    public class EnemyPhaseDataDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight * 2 + 4; 

            SerializedProperty actionTypeProp = property.FindPropertyRelative("actionType");
            PhaseActionType type = (PhaseActionType)actionTypeProp.enumValueIndex;

            if (type == PhaseActionType.ChangeSkills)
            {
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("phaseSkills"), true) + 2;
            }
            else if (type == PhaseActionType.ApplyBuffs)
            {
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("phaseBuffs"), true) + 2;
            }
            else if (type == PhaseActionType.SpawnEnemies)
            {
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("phaseEnemies"), true) + 2;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            SerializedProperty hpProp = property.FindPropertyRelative("hpThresholdPercent");
            EditorGUI.PropertyField(rect, hpProp, new GUIContent("HP Threshold (%)"));
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            SerializedProperty actionTypeProp = property.FindPropertyRelative("actionType");
            EditorGUI.PropertyField(rect, actionTypeProp, new GUIContent("Action Type"));
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            PhaseActionType type = (PhaseActionType)actionTypeProp.enumValueIndex;

            if (type == PhaseActionType.ChangeSkills)
            {
                SerializedProperty skillsProp = property.FindPropertyRelative("phaseSkills");
                EditorGUI.PropertyField(rect, skillsProp, new GUIContent("Phase Skills"), true);
            }
            else if (type == PhaseActionType.ApplyBuffs)
            {
                SerializedProperty buffsProp = property.FindPropertyRelative("phaseBuffs");
                EditorGUI.PropertyField(rect, buffsProp, new GUIContent("Phase Buffs (Milestones)"), true);
            }
            else if (type == PhaseActionType.SpawnEnemies)
            {
                SerializedProperty enemiesProp = property.FindPropertyRelative("phaseEnemies");
                EditorGUI.PropertyField(rect, enemiesProp, new GUIContent("Phase Enemies To Spawn"), true);
            }

            EditorGUI.EndProperty();
        }
        
    }
}