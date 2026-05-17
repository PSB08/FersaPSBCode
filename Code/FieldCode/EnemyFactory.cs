using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB.Code.BattleCode.Enemies;
using UnityEngine;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode
{
    public class EnemyFactory : MonoBehaviour
    {
        [SerializeField] private EnemyPrefabDB prefabDB;
        [SerializeField] private Transform enemyParent;
        
        public BattleEnemy CreateEnemy(EnemySO enemyData, PoolManagerMono poolManager)
        {
            if (prefabDB.enemyPrefab == null)
            {
                Debug.LogError("[EnemyFactory] Enemy prefab is missing in EnemyPrefabDB!");
                return null;
            }

            if (enemyParent == null)
            {
                Debug.LogError("[EnemyFactory] Enemy parent is null in EnemyFactory");
                return null;
            }

            GameObject enemyObj = Instantiate
            (
                prefabDB.enemyPrefab,
                transform.position,
                Quaternion.identity,
                enemyParent
            );
            BattleEnemy battleEnemyComp = enemyObj.GetComponent<BattleEnemy>();
            battleEnemyComp.buffModule.SetPool(poolManager);
            
            if (battleEnemyComp == null)
            {
                Debug.LogError("[EnemyFactory] Enemy prefab does not contain Enemy component!");
                return null;
            }

            battleEnemyComp.Setup(enemyData);
            return battleEnemyComp;
        }
        
    }
}