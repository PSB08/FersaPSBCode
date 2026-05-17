using System;
using UnityEngine;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.Items
{
    [Serializable]
    public class DropEntry
    {
        public ItemDataSO item;
        public int minAmount = 1;
        public int maxAmount = 1;
        public float dropRate = 1f;
    }
    
    [CreateAssetMenu(fileName = "DropTable", menuName = "SO/Item/DropTable", order = 99)]
    public class DropTableSO : ScriptableObject
    {
        public DropEntry[] entries;
    }   
}