using System;
using PSB.Code.BattleCode.Enums;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies
{
    [CreateAssetMenu(fileName = "GradeNameTable", menuName = "SO/Enemy/GradeNameTable", order = 30)]
    public class EnemyGradeNameTableSO : ScriptableObject
    {
        [SerializeField] private EnemyGradeNameEntry[] entries;

        public string GetName(EnemyGrade grade)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].grade == grade && !string.IsNullOrEmpty(entries[i].displayName))
                        return entries[i].displayName;
                }
            }

            return grade.ToString();
        }
    }

    [Serializable]
    public struct EnemyGradeNameEntry
    {
        public EnemyGrade grade;
        public string displayName;
    }
    
}