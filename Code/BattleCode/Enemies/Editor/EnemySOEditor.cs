using PSB.Code.BattleCode.Enemies;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Work.PSB.Code.BattleCode.Enemies.Editor
{
    [CustomEditor(typeof(EnemySO))]
    public class EnemySOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset inspectorUxml;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            inspectorUxml.CloneTree(root);

            var syncButton = root.Q<Button>("SyncButton");
            syncButton.clicked += () =>
            {
                serializedObject.ApplyModifiedProperties();

                EnemySO enemy = (EnemySO)target;
                enemy.SyncStatOverrides();
                EditorUtility.SetDirty(enemy);
                serializedObject.Update();
            };

            return root;
        }
    
    }
}