using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PSB.Code.CoreSystem.SaveSystem
{
    [CreateAssetMenu(fileName = "save id", menuName = "SO/Save id", order = 0)]
    public class SaveId : ScriptableObject
    {
        [field: SerializeField] public int ID { get; private set; }
        [field: SerializeField, TextArea] public string Description { get; private set; } 
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateDuplicateID();
        }

        private void ValidateDuplicateID()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(SaveId)}");
            
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SaveId other = AssetDatabase.LoadAssetAtPath<SaveId>(path);

                if (other != null && other != this && other.ID == ID)
                {
                    Debug.LogError($"<color=red>[SaveSystem]</color> 중복된 ID " +
                                   $"현재 에셋: <b>{name}</b>, 중복된 에셋: <b>{other.name}</b> (ID: {ID})");
                }
            }
        }
#endif
        
    }
}