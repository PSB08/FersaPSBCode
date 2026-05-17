using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.PSB.Code.FieldCode
{
    [CreateAssetMenu(fileName = "EnemyPrefabDB", menuName = "SO/Data/EnemyPrefabDB", order = 20)]
    public class EnemyPrefabDB : ScriptableObject
    {
        public GameObject enemyPrefab;
        
    }
}