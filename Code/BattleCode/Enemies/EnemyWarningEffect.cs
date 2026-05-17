using PSB.Code.BattleCode.Enums;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies
{
    public class EnemyWarningEffect : MonoBehaviour, IModule
    {
        [SerializeField] private GameObject warningObject;

        private BattleEnemy _enemy;
        
        public void Initialize(ModuleOwner owner)
        {
            _enemy = owner as BattleEnemy;
        }
        
        private void Start()
        {
            bool isWarningTarget =
                _enemy.enemySO.grade == EnemyGrade.MiniBoss ||
                _enemy.enemySO.grade == EnemyGrade.Boss;

            warningObject.SetActive(isWarningTarget);
        }

        public void EffectOffCode()
        {
            warningObject.SetActive(false);
        }
        
        
    }
}