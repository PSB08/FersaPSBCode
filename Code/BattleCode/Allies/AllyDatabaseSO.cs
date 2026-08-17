using System;
using UnityEngine;

namespace PSB.Code.BattleCode.Allies
{
    [CreateAssetMenu(fileName = "AllyDatabase", menuName = "SO/Ally/AllyDatabase", order = 121)]
    public class AllyDatabaseSO : ScriptableObject
    {
        [SerializeField] private AllySO[] allies;

        public AllySO[] Allies => allies;

        public bool TryGetById(string allyId, out AllySO ally)
        {
            ally = null;

            if (string.IsNullOrWhiteSpace(allyId) || allies == null)
                return false;

            for (int i = 0; i < allies.Length; i++)
            {
                AllySO candidate = allies[i];
                if (candidate == null) continue;

                if (string.Equals(candidate.AllyId, allyId, StringComparison.Ordinal))
                {
                    ally = candidate;
                    return true;
                }
            }

            return false;
        }
        
    }
}
