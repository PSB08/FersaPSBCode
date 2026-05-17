using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies;
using UnityEngine;

namespace PSB.Code.BattleCode.Players
{
    public static class SkillTargetingUtil
    {
        public static List<Entity> GetTargetsByRange(IReadOnlyList<Entity> enemies, int centerIndex, int range)
        {
            var result = new List<Entity>();
            if (enemies == null || enemies.Count == 0) return result;

            if (range <= 0) range = 1;
            if (centerIndex < 0 || centerIndex >= enemies.Count) return result;

            int maxCount = enemies.Count;
            int count = range > maxCount ? maxCount : range;

            for (int k = 0; k < count; k++)
            {
                int idx = (centerIndex + k) % enemies.Count;
                var e = enemies[idx];
                if (e == null) continue;
                
                if (e.IsDead) continue;

                result.Add(e);
            }

            return result;
        }

        
    }
}