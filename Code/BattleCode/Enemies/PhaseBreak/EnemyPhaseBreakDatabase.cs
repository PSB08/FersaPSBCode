using System;
using PSB.Code.BattleCode.Enums;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.PhaseBreak
{
    [CreateAssetMenu(fileName = "EnemyPhaseBreakDatabase", menuName = "SO/Enemy/Phase Break Database", order = 131)]
    public class EnemyPhaseBreakDatabase : ScriptableObject
    {
        [SerializeField] private EnemyPhaseBreakProfile defaultProfile;
        [SerializeField] private EnemyPhaseBreakEnemyEntry[] enemyProfiles;
        [SerializeField] private EnemyPhaseBreakGradeEntry[] gradeProfiles;

        public EnemyPhaseBreakProfile Resolve(EnemySO enemySO)
        {
            //적별 설정을 먼저 보고, 없으면 등급별 설정, 그것도 없으면 기본 Profile 사용
            if (enemySO == null)
                return defaultProfile;

            if (enemyProfiles != null)
            {
                //적 개별 설정이 등급 설정보다 우선
                foreach (EnemyPhaseBreakEnemyEntry entry in enemyProfiles)
                {
                    if (entry.Enemy == enemySO && entry.Profile != null)
                        return entry.Profile;
                }
            }

            if (gradeProfiles != null)
            {
                //개별 설정이 없으면 적 등급에 맞는 Profile 찾기
                foreach (EnemyPhaseBreakGradeEntry entry in gradeProfiles)
                {
                    if (entry.Grade == enemySO.grade && entry.Profile != null)
                        return entry.Profile;
                }
            }

            //개별, 등급 설정을 모두 못 찾으면 기본 Profile을 사용
            return defaultProfile;
        }
    }

    [Serializable]
    public struct EnemyPhaseBreakEnemyEntry
    {
        [SerializeField] private EnemySO enemy; //이 Profile을 적용할 특정 EnemySO
        [SerializeField] private EnemyPhaseBreakProfile profile; //해당 EnemySO가 사용할 PhaseBreak Profile
         
        public EnemySO Enemy => enemy; //DB Resolve에서 EnemySO 비교용으로 읽는 값
        public EnemyPhaseBreakProfile Profile => profile; //DB Resolve에서 반환할 Profile 값
    }

    [Serializable]
    public struct EnemyPhaseBreakGradeEntry
    {
        [SerializeField] private EnemyGrade grade; //이 Profile을 적용할 적 등급
        [SerializeField] private EnemyPhaseBreakProfile profile; //해당 등급이 사용할 PhaseBreak Profile
        
        public EnemyGrade Grade => grade; //DB Resolve에서 등급 비교용으로 읽는 값
        public EnemyPhaseBreakProfile Profile => profile; //DB Resolve에서 반환할 Profile 값
    }
    
}
